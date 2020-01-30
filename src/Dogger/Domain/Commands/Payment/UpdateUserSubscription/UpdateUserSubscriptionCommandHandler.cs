using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Domain.Queries.Plans.GetSupportedPullDogPlans;
using Dogger.Infrastructure.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Polly;
using Stripe;
using Plan = Dogger.Domain.Queries.Plans.GetSupportedPlans.Plan;

namespace Dogger.Domain.Commands.Payment.UpdateUserSubscription
{
    public class UpdateUserSubscriptionCommandHandler : IRequestHandler<UpdateUserSubscriptionCommand>
    {
        private readonly DataContext dataContext;
        private readonly SubscriptionService subscriptionService;

        private readonly ITimeProvider timeProvider;
        private readonly IMediator mediator;

        public UpdateUserSubscriptionCommandHandler(
            DataContext dataContext,
            SubscriptionService subscriptionService,
            ITimeProvider timeProvider,
            IMediator mediator)
        {
            this.dataContext = dataContext;
            this.subscriptionService = subscriptionService;
            this.timeProvider = timeProvider;
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(
            UpdateUserSubscriptionCommand request, 
            CancellationToken cancellationToken)
        {
            var user = await this.dataContext
                .Users
                .Include(x => x.PullDogSettings)
                .Include(x => x.Clusters)
                .ThenInclude(x => x.Instances)
                .ThenInclude(x => x.PullDogPullRequest)
                .ThenInclude(x => x!.PullDogRepository)
                .ThenInclude(x => x.PullDogSettings)
                .SingleOrDefaultAsync(
                    x => x.Id == request.UserId,
                    cancellationToken);

            var existingSubscriptionItems = await GetExistingSubscriptionItemsAsync(user);

            var subscriptionItems = user
                .Clusters
                .SelectMany(x => x.Instances)
                .Where(instance => instance.PullDogPullRequest == null)
                .GroupBy(x => x.PlanId)
                .Select(instancesByPlanId =>
                    GetDoggerSubscriptionItemFromGroupedInstances(
                        instancesByPlanId, 
                        existingSubscriptionItems))
                .ToList();

            var pullDogSubscriptionItem = await GetPullDogSubscriptionItemAsync(
                user, 
                existingSubscriptionItems, 
                cancellationToken);
            if (pullDogSubscriptionItem != null)
                subscriptionItems.Add(pullDogSubscriptionItem);

            AddRemainingExistingSubscriptionItemsAsDeletions(subscriptionItems, existingSubscriptionItems);

            if (CountAdditions(subscriptionItems) == 0 && user.StripeSubscriptionId == null)
                return Unit.Value;

            var createdSubscription = await UpdateSubscriptionAsync(user, subscriptionItems, cancellationToken);

            var intent = createdSubscription.LatestInvoice.PaymentIntent;
            switch (intent?.Status)
            {
                case "requires_payment_method":
                    throw new InvalidOperationException("Stripe reported no payment method present.");

                case "requires_action":
                    throw new NotImplementedException("Subscriptions requiring action are not yet supported.");
            }

            return Unit.Value;
        }

        private static void AddRemainingExistingSubscriptionItemsAsDeletions(List<SubscriptionItemOptions> subscriptionItems, SubscriptionItem[] existingSubscriptionItems)
        {
            foreach (var existingSubscriptionItem in existingSubscriptionItems)
            {
                var isPresentInSubscriptionItems = subscriptionItems.Any(item => item.Id == existingSubscriptionItem.Id);
                if (isPresentInSubscriptionItems)
                    continue;

                subscriptionItems.Add(new SubscriptionItemOptions()
                {
                    Id = existingSubscriptionItem.Id,
                    Quantity = existingSubscriptionItem.Quantity,
                    Deleted = true
                });
            }
        }

        private static SubscriptionItemOptions GetDoggerSubscriptionItemFromGroupedInstances(
            IGrouping<string, Instance> instancesByPlanId, 
            SubscriptionItem[] existingSubscriptionItems)
        {
            var planId = instancesByPlanId.Key;

            var existingItem = GetSubscriptionByPlanId(existingSubscriptionItems, planId);
            var count = instancesByPlanId.Count();

            return new SubscriptionItemOptions()
            {
                Id = existingItem?.Id,
                Plan = planId,
                Quantity = count
            };
        }

        private static SubscriptionItem? GetSubscriptionByPlanId(
            SubscriptionItem[] subscriptionItems, 
            string planId)
        {
            return subscriptionItems.SingleOrDefault(x => x.Plan.Id == planId);
        }

        private async Task<SubscriptionItem[]> GetExistingSubscriptionItemsAsync(User user)
        {
            if (user.StripeSubscriptionId == null)
                return Array.Empty<SubscriptionItem>();

            var subscription = await this.subscriptionService.GetAsync(user.StripeSubscriptionId);
            return subscription.Items.Data.ToArray();
        }

        private async Task<Subscription> UpdateSubscriptionAsync(
            User user, 
            List<SubscriptionItemOptions> subscriptionItems, 
            CancellationToken cancellationToken)
        {
            var monthsOffset = 0;

            var policy = Policy
                .Handle<StripeException>(exception =>
                    exception.Message == "Invalid timestamp: must be an integer Unix timestamp in the future.")
                .RetryAsync((ex, attempt) =>
                    monthsOffset++);

            var createdSubscription = await policy.ExecuteAsync(async () =>
            {
                Subscription subscription;
                if (user.StripeSubscriptionId == null)
                {
                    subscription = await this.subscriptionService.CreateAsync(
                        GetSubscriptionCreateOptions(
                            user,
                            subscriptionItems,
                            monthsOffset),
                        default,
                        cancellationToken);
                    user.StripeSubscriptionId = subscription.Id;
                }
                else
                {
                    if (CountAdditions(subscriptionItems) == 0)
                    {
                        subscription = await this.subscriptionService.CancelAsync(
                            user.StripeSubscriptionId,
                            GetSubscriptionCancelOptions(),
                            default,
                            cancellationToken);
                        user.StripeSubscriptionId = null;
                    }
                    else
                    {
                        subscription = await this.subscriptionService.UpdateAsync(
                            user.StripeSubscriptionId,
                            GetSubscriptionUpdateOptions(
                                subscriptionItems),
                            default,
                            cancellationToken);
                        user.StripeSubscriptionId = subscription.Id;
                    }
                }

                return subscription;
            });

            await this.dataContext.SaveChangesAsync(cancellationToken);
            return createdSubscription;
        }

        private static int CountAdditions(List<SubscriptionItemOptions> subscriptionItems)
        {
            return subscriptionItems.Count(x => x.Deleted != true);
        }

        private async Task<SubscriptionItemOptions?> GetPullDogSubscriptionItemAsync(
            User user,
            SubscriptionItem[] existingSubscriptionItems, 
            CancellationToken cancellationToken)
        {
            if (user.PullDogSettings == null || user.PullDogSettings.PoolSize == 0)
                return null;

            var pullDogPlan = await this.mediator.Send(
                new GetPullDogPlanFromSettingsQuery(
                    user.PullDogSettings.PlanId,
                    user.PullDogSettings.PoolSize),
                cancellationToken);
            if (pullDogPlan == null)
                throw new InvalidOperationException("Could not find a pull dog plan that matched.");

            var availablePullDogPlans = await this.mediator.Send(
                new GetSupportedPullDogPlansQuery(),
                cancellationToken);

            var existingSubscriptionItem = existingSubscriptionItems.SingleOrDefault(plan => 
                availablePullDogPlans.Any(p => p.Id == plan.Plan.Id));

            return new SubscriptionItemOptions()
            {
                Id = existingSubscriptionItem?.Id,
                Plan = pullDogPlan.Id,
                Quantity = 1
            };
        }

        private static SubscriptionCancelOptions GetSubscriptionCancelOptions()
        {
            var subscriptionCancelOptions = new SubscriptionCancelOptions()
            {
                InvoiceNow = true,
                Prorate = true
            };

            subscriptionCancelOptions.AddExpand("latest_invoice.payment_intent");
            return subscriptionCancelOptions;
        }

        private static SubscriptionUpdateOptions GetSubscriptionUpdateOptions(
            List<SubscriptionItemOptions> items)
        {
            var options = new SubscriptionUpdateOptions()
            {
                Prorate = true,
                Items = items
            };

            options.AddExpand("latest_invoice.payment_intent");
            return options;
        }

        private SubscriptionCreateOptions GetSubscriptionCreateOptions(
            User user,
            List<SubscriptionItemOptions> items,
            int monthsOffset = 0)
        {
            var options = new SubscriptionCreateOptions()
            {
                Customer = user.StripeCustomerId,
                BillingCycleAnchor = GetBillingCycleAnchor(monthsOffset),
                ProrationBehavior = "create_prorations",
                Items = items
            };

            options.AddExpand("latest_invoice.payment_intent");
            return options;
        }

        private DateTime GetBillingCycleAnchor(int monthsOffset)
        {
            var now = this.timeProvider.UtcNow;

            return new DateTime(now.Year, now.Month, 1)
                .AddMonths(1)
                .AddMonths(monthsOffset);
        }
    }
}

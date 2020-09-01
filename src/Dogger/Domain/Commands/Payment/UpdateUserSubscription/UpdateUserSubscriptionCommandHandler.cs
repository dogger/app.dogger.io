using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Payment.GetSubscriptionById;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings;
using Dogger.Domain.Queries.Plans.GetSupportedPullDogPlans;
using Dogger.Infrastructure.Ioc;
using Dogger.Infrastructure.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Polly;
using Stripe;

namespace Dogger.Domain.Commands.Payment.UpdateUserSubscription
{
    public class UpdateUserSubscriptionCommandHandler : IRequestHandler<UpdateUserSubscriptionCommand>
    {
        private readonly DataContext dataContext;
        private readonly SubscriptionService? subscriptionService;

        private readonly ITimeProvider timeProvider;
        private readonly IMediator mediator;

        public UpdateUserSubscriptionCommandHandler(
            DataContext dataContext,
            IOptionalService<SubscriptionService> subscriptionService,
            ITimeProvider timeProvider,
            IMediator mediator)
        {
            this.dataContext = dataContext;
            this.subscriptionService = subscriptionService.Value;
            this.timeProvider = timeProvider;
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(
            UpdateUserSubscriptionCommand request,
            CancellationToken cancellationToken)
        {
            if (this.subscriptionService == null)
                return Unit.Value;

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

            var createdSubscription = await UpdateSubscriptionAsync(user, cancellationToken);
            if (createdSubscription == null)
                return Unit.Value;

            var intent = createdSubscription.LatestInvoice.PaymentIntent;
            return (intent?.Status) switch
            {
                "requires_payment_method" =>
                    throw new InvalidOperationException("Stripe reported no payment method present."),
                "requires_action" =>
                    throw new NotImplementedException("Subscriptions requiring action are not yet supported."),
                _ => Unit.Value,
            };
        }

        private async Task<List<SubscriptionItemOptions>> GetCurrentSubscriptionItemOptionsAsync(
            User user, 
            Subscription? subscription,
            CancellationToken cancellationToken)
        {
            var existingSubscriptionItems = 
                subscription?.Items.Data.ToArray() ??
                Array.Empty<SubscriptionItem>();

            var subscriptionItemOptions = user
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
                subscriptionItemOptions.Add(pullDogSubscriptionItem);

            AddRemainingExistingSubscriptionItemsAsDeletions(
                subscriptionItemOptions,
                existingSubscriptionItems);

            return subscriptionItemOptions;
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

        private async Task<Subscription?> GetExistingSubscriptionAsync(User user)
        {
            if (user.StripeSubscriptionId == null)
                return null;

            return await this.mediator.Send(new GetSubscriptionByIdQuery(user.StripeSubscriptionId));
        }

        private async Task<Subscription?> UpdateSubscriptionAsync(
            User user,
            CancellationToken cancellationToken)
        {
            var subscription = await GetExistingSubscriptionAsync(user);

            var subscriptionItemOptions = await GetCurrentSubscriptionItemOptionsAsync(
                user, 
                subscription,
                cancellationToken);

            var monthsOffset = 0;

            var policy = Policy
                .Handle<StripeException>(exception =>
                    exception.Message == "Invalid timestamp: must be an integer Unix timestamp in the future.")
                .RetryAsync((ex, attempt) =>
                    monthsOffset++);

            var createdSubscription = await policy.ExecuteAsync(async () =>
            {
                if (this.subscriptionService == null)
                    throw new InvalidOperationException("Stripe subscription service was not configured.");
                
                var additions = CountAdditions(subscriptionItemOptions);
                if (subscription == null)
                {
                    if (additions == 0)
                        return null;

                    subscription = await this.subscriptionService.CreateAsync(
                        GetSubscriptionCreateOptions(
                            user,
                            subscriptionItemOptions,
                            monthsOffset),
                        default,
                        cancellationToken);
                    user.StripeSubscriptionId = subscription.Id;
                }
                else
                {
                    if (additions == 0)
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
                                subscriptionItemOptions),
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
                throw new InvalidOperationException("Could not find a Pull Dog plan that matched.");

            var availablePullDogPlans = await this.mediator.Send(
                new GetSupportedPullDogPlansQuery(),
                cancellationToken);

            var existingSubscriptionItem = existingSubscriptionItems.SingleOrDefault(plan =>
                availablePullDogPlans.Any(p => 
                    NormalizePlanName(p.Id) == NormalizePlanName(plan.Plan.Id)));

            return new SubscriptionItemOptions()
            {
                Id = existingSubscriptionItem?.Id,
                Plan = GetLatestPlanName(pullDogPlan.Id),
                Quantity = user.PullDogSettings.PoolSize
            };
        }

        public static string GetLatestPullDogPlanSuffix()
        {
            return "_v3";
        }

        private static string GetLatestPlanName(string name)
        {
            return $"{NormalizePlanName(name)}{GetLatestPullDogPlanSuffix()}";
        }

        private static string NormalizePlanName(string name)
        {
            name = TrimEnd(name, GetLatestPullDogPlanSuffix());
            name = TrimStart(name, "personal_");
            name = TrimStart(name, "business_");
            name = TrimStart(name, "pro_");

            return name;
        }

        private static string TrimStart(string input, string trim)
        {
            if (input.StartsWith(trim, StringComparison.InvariantCulture))
                input = input.Substring(trim.Length);

            return input;
        }

        private static string TrimEnd(string input, string trim)
        {
            if (input.EndsWith(trim, StringComparison.InvariantCulture))
                input = input.Substring(0, input.LastIndexOf(trim, StringComparison.InvariantCulture));

            return input;
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

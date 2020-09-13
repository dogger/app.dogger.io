using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Payment.UpdateUserSubscription;
using Dogger.Domain.Queries.Payment.GetSubscriptionById;
using Dogger.Domain.Queries.Plans.GetPlanById;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Domain.Queries.Plans.GetSupportedPullDogPlans;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.Extensions;
using Stripe;

namespace Dogger.Tests.Domain.Commands.Payment.UpdateUserSubscription
{
    [TestClass]
    public class UpdateUserSubscriptionCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_DoggerInstancesPresentWithDifferentPlanIds_DoggerInstancesAddedToDifferentSubscriptionItems()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var customer = await environment.Stripe.CustomerBuilder.BuildAsync();

            var oldPlan1 = await environment.Stripe.PlanBuilder.BuildAsync();
            var oldPlan2 = await environment.Stripe.PlanBuilder.BuildAsync();

            var newPlan1 = await environment.Stripe.PlanBuilder.BuildAsync();
            var newPlan2 = await environment.Stripe.PlanBuilder.BuildAsync();

            var subscription = await environment.Stripe.SubscriptionBuilder
                .WithCustomer(customer)
                .WithPlans(
                    oldPlan1, 
                    oldPlan2)
                .WithDefaultPaymentMethod(await environment.Stripe.PaymentMethodBuilder
                    .WithCustomer(customer)
                    .BuildAsync())
                .BuildAsync();

            var user = new TestUserBuilder()
                .WithStripeSubscriptionId(subscription.Id)
                .WithClusters(new TestClusterBuilder()
                    .WithInstances(
                        new TestInstanceBuilder()
                            .WithPlanId(newPlan1.Id),
                        new TestInstanceBuilder()
                            .WithPlanId(newPlan2.Id)))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            var refreshedSubscription = await environment.Stripe.SubscriptionService.GetAsync(subscription.Id);
            Assert.IsNull(refreshedSubscription.Quantity);
            Assert.AreEqual(2, refreshedSubscription.Items.Count());

            var refreshedNewPlan1 = refreshedSubscription.Items.Single(x => x.Plan.Id == newPlan1.Id);
            Assert.AreEqual(1, refreshedNewPlan1.Quantity);
            
            var refreshedNewPlan2 = refreshedSubscription.Items.Single(x => x.Plan.Id == newPlan2.Id);
            Assert.AreEqual(1, refreshedNewPlan2.Quantity);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_DoggerInstancesPresentWithSamePlanIds_DoggerInstancesAddedToSameSubscriptionItem()
        {
            //Arrange
            var fakeSubscriptionService = (SubscriptionService)null;
            fakeSubscriptionService
                .Configure()
                .UpdateAsync(
                    Arg.Any<string>(),
                    Arg.Any<SubscriptionUpdateOptions>(),
                    default,
                    default)
                .Returns(new Subscription()
                {
                    LatestInvoice = new Invoice()
                    {
                        PaymentIntent = new PaymentIntent()
                    }
                });

            fakeSubscriptionService
                .Configure()
                .GetAsync("some-subscription-id")
                .Returns(new Subscription()
                {
                    Items = new StripeList<SubscriptionItem>()
                    {
                        Data = new List<SubscriptionItem>()
                    }
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(
                new DoggerEnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeSubscriptionService)
                });

            var user = new TestUserBuilder()
                .WithStripeSubscriptionId("some-subscription-id")
                .WithClusters(new TestClusterBuilder()
                    .WithInstances(
                        new TestInstanceBuilder()
                            .WithPlanId("some-plan-id-1"),
                        new TestInstanceBuilder()
                            .WithPlanId("some-plan-id-1")))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            await fakeSubscriptionService
                .Received(1)
                .UpdateAsync(
                    "some-subscription-id",
                    Arg.Is<SubscriptionUpdateOptions>(args =>
                        args.Prorate == true &&
                        args.Items[0].Plan == "some-plan-id-1" &&
                        args.Items[0].Id == null &&
                        args.Items[0].Quantity == 2),
                    default,
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullDogPresentWithPaidPlan_PullDogAddedToSubscription()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetBundlesAsync(Arg.Any<GetBundlesRequest>())
                .Returns(new GetBundlesResponse()
                {
                    Bundles = new List<Bundle>()
                    {
                        new Bundle()
                        {
                            IsActive = true,
                            RamSizeInGb = 0.5f,
                            SupportedPlatforms = new List<string>()
                            {
                                "LINUX_UNIX"
                            }
                        }
                    }
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeAmazonLightsail)
            });
            
            var customer = await environment.Stripe.CustomerBuilder.BuildAsync();
            var plan = await environment.Stripe.PlanBuilder
                .WithId("512_v3")
                .BuildAsync();

            var subscription = await environment.Stripe.SubscriptionBuilder
                .WithPlans(plan)
                .WithCustomer(customer)
                .WithDefaultPaymentMethod(await environment.Stripe.PaymentMethodBuilder
                    .WithCustomer(customer)
                    .BuildAsync())
                .BuildAsync();

            var user = new TestUserBuilder()
                .WithStripeSubscriptionId(subscription.Id)
                .WithPullDogSettings(new TestPullDogSettingsBuilder()
                    .WithPoolSize(2))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            var refreshedSubscription = await environment.Stripe.SubscriptionService.GetAsync(subscription.Id);
            Assert.AreEqual(plan.Id, refreshedSubscription.Plan.Id);

            Assert.IsNull(refreshedSubscription.Items.Single().Id);
            Assert.AreEqual(2, refreshedSubscription.Items.Single().Quantity);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullDogPresentWithDemoPlan_NoPullDogAddedToSubscription()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args =>
                    args.DoggerPlanId == "some-pull-dog-plan" &&
                    args.PoolSize == 2))
                .Returns(new PullDogPlan(
                    "some-pull-dog-plan",
                    1337,
                    2));

            var fakeSubscriptionService = (SubscriptionService)null;
            fakeSubscriptionService
                .Configure()
                .UpdateAsync(
                    Arg.Any<string>(),
                    Arg.Any<SubscriptionUpdateOptions>(),
                    default,
                    default)
                .Returns(new Subscription()
                {
                    LatestInvoice = new Invoice()
                    {
                        PaymentIntent = new PaymentIntent()
                    }
                });

            fakeSubscriptionService
                .Configure()
                .GetAsync("some-subscription-id")
                .Returns(new Subscription()
                {
                    Items = new StripeList<SubscriptionItem>()
                    {
                        Data = new List<SubscriptionItem>()
                    }
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(
                new DoggerEnvironmentSetupOptions()
                {
                    IocConfiguration = services =>
                    {
                        services.AddSingleton(fakeSubscriptionService);
                        services.AddSingleton(fakeMediator);
                    }
                });

            var user = new TestUserBuilder()
                .WithStripeSubscriptionId(null)
                .WithPullDogSettings(new TestPullDogSettingsBuilder()
                    .WithPoolSize(0))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            await fakeSubscriptionService
                .DidNotReceiveWithAnyArgs()
                .UpdateAsync(
                    default,
                    default,
                    default,
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_DoggerInstanceAddedAndOtherInstanceRemoved_AddsBothAddAndRemoveToSubscription()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var customer = await environment.Stripe.CustomerBuilder.BuildAsync();

            var initialPlan = await environment.Stripe.PlanBuilder.BuildAsync();
            var newPlan = await environment.Stripe.PlanBuilder.BuildAsync();

            var subscription = await environment.Stripe.SubscriptionBuilder
                .WithCustomer(customer)
                .WithPlans(initialPlan)
                .WithDefaultPaymentMethod(await environment.Stripe.PaymentMethodBuilder
                    .WithCustomer(customer)
                    .BuildAsync())
                .BuildAsync();

            var user = new TestUserBuilder()
                .WithStripeSubscriptionId(subscription.Id)
                .WithStripeCustomerId(customer.Id)
                .WithClusters(new TestClusterBuilder()
                    .WithInstances(new TestInstanceBuilder()
                        .WithPlanId(newPlan.Id)))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            var refreshedSubscription = await environment.Stripe.SubscriptionService.GetAsync(subscription.Id);
            Assert.AreEqual(newPlan.Id, refreshedSubscription.Plan.Id);
            Assert.AreEqual(1, refreshedSubscription.Quantity);
            Assert.AreEqual(1, refreshedSubscription.Items.Count());
            Assert.AreEqual(newPlan.Id, refreshedSubscription.Items.Single().Plan.Id);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingPullDogSubscriptionAndSettingsChanged_ExistingPullDogPlanIsChanged()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args =>
                    args.DoggerPlanId == "some-pull-dog-plan" &&
                    args.PoolSize == 2))
                .Returns(new PullDogPlan(
                    "some-old-pull-dog-plan",
                    1337,
                    2));

            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args =>
                    args.DoggerPlanId == "some-pull-dog-plan" &&
                    args.PoolSize == 5))
                .Returns(new PullDogPlan(
                    "some-new-pull-dog-plan",
                    1337,
                    5));

            fakeMediator
                .Send(Arg.Any<GetSupportedPullDogPlansQuery>())
                .Returns(new[] {
                    new PullDogPlan(
                        "some-old-pull-dog-plan",
                        1337,
                        2),
                    new PullDogPlan(
                        "some-new-pull-dog-plan",
                        1337,
                        5)
                });

            //var fakeSubscriptionService = (SubscriptionService)null;
            //fakeSubscriptionService
            //    .Configure()
            //    .UpdateAsync(
            //        Arg.Any<string>(),
            //        Arg.Any<SubscriptionUpdateOptions>(),
            //        default,
            //        default)
            //    .Returns(new Subscription()
            //    {
            //        LatestInvoice = new Invoice()
            //        {
            //            PaymentIntent = new PaymentIntent()
            //        }
            //    });

            //fakeSubscriptionService
            //    .Configure()
            //    .GetAsync("some-subscription-id")
            //    .Returns(new Subscription()
            //    {
            //        Items = new StripeList<SubscriptionItem>()
            //        {
            //            Data = new List<SubscriptionItem>()
            //            {
            //                new SubscriptionItem()
            //                {
            //                    Id = "some-subscription-item-id",
            //                    Plan = new Stripe.Plan()
            //                    {
            //                        Id = "some-old-pull-dog-plan"
            //                    }
            //                }
            //            }
            //        }
            //    });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(
                new DoggerEnvironmentSetupOptions()
                {
                    IocConfiguration = services =>
                    {
                        services.AddSingleton(fakeMediator);
                    }
                });

            var user = new TestUserBuilder()
                .WithStripeSubscriptionId("some-subscription-id")
                .WithPullDogSettings(new TestPullDogSettingsBuilder()
                    .WithPoolSize(5)
                    .WithPlanId("some-pull-dog-plan"))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            //await fakeSubscriptionService
            //    .Received(1)
            //    .UpdateAsync(
            //        "some-subscription-id",
            //        Arg.Is<SubscriptionUpdateOptions>(args =>
            //            args.Prorate == true &&
            //            args.Items[0].Plan == "some-new-pull-dog-plan_v2" &&
            //            args.Items[0].Id == "some-subscription-item-id" &&
            //            args.Items[0].Quantity == 1),
            //        default,
            //        default);

            Assert.Fail();
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoStripeSubscriptionPresent_StripeSubscriptionCreated()
        {
            //Arrange
            var fakeSubscriptionService = (SubscriptionService)null;
            fakeSubscriptionService
                .Configure()
                .CreateAsync(
                    Arg.Any<SubscriptionCreateOptions>(),
                    default,
                    default)
                .Returns(new Subscription()
                {
                    LatestInvoice = new Invoice()
                    {
                        PaymentIntent = new PaymentIntent()
                    }
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(
                new DoggerEnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeSubscriptionService)
                });

            var user = new TestUserBuilder()
                .WithStripeCustomerId("some-customer-id")
                .WithStripeSubscriptionId(null)
                .WithClusters(new TestClusterBuilder()
                    .WithInstances(new TestInstanceBuilder()
                        .WithPlanId("some-plan-id")))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            await fakeSubscriptionService
                .Received(1)
                .CreateAsync(
                    Arg.Is<SubscriptionCreateOptions>(args =>
                        args.ProrationBehavior == "create_prorations" &&
                        args.Customer == "some-customer-id" &&
                        args.BillingCycleAnchor != null &&
                        args.Items[0].Plan == "some-plan-id" &&
                        args.Items[0].Id == null &&
                        args.Items[0].Quantity == 1),
                    default,
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoStripeSubscriptionPresent_UserStripeSubscriptionIdSavedInDatabase()
        {
            //Arrange
            var fakeSubscriptionService = (SubscriptionService)null;
            fakeSubscriptionService
                .Configure()
                .CreateAsync(
                    Arg.Any<SubscriptionCreateOptions>(),
                    default,
                    default)
                .Returns(new Subscription()
                {
                    Id = "created-subscription-id",
                    LatestInvoice = new Invoice()
                    {
                        PaymentIntent = new PaymentIntent()
                    }
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(
                new DoggerEnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeSubscriptionService)
                });

            var user = new TestUserBuilder()
                .WithStripeSubscriptionId(null)
                .WithClusters(new TestClusterBuilder()
                    .WithInstances(new TestInstanceBuilder()
                        .WithPlanId("some-plan-id")))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedUser = await dataContext
                    .Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync();
                Assert.IsNotNull(refreshedUser);

                Assert.AreEqual("created-subscription-id", refreshedUser.StripeSubscriptionId);
            });
        }
    }
}

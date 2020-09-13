﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.UpdateUserSubscription;
using Dogger.Domain.Queries.Payment.GetSubscriptionById;
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
            //        }
            //    });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var customer = await environment.Stripe.CustomerBuilder.BuildAsync();

            var plan1 = await environment.Stripe.PlanBuilder.BuildAsync();
            var plan2 = await environment.Stripe.PlanBuilder.BuildAsync();

            var subscription = await environment.Stripe.SubscriptionBuilder
                .WithCustomer(customer)
                .WithPlan(plan1)
                .WithDefaultPaymentMethod(await environment.Stripe.PaymentMethodBuilder
                    .WithCustomer(customer)
                    .BuildAsync())
                .BuildAsync();

            var user = new TestUserBuilder()
                .WithStripeSubscriptionId(subscription.Id)
                .WithClusters(new TestClusterBuilder()
                    .WithInstances(
                        new TestInstanceBuilder()
                            .WithPlanId(plan1.Id),
                        new TestInstanceBuilder()
                            .WithPlanId(plan2.Id)))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            var refreshedSubscription = await environment.Stripe.SubscriptionService.GetAsync(subscription.Id);
            refreshedSubscription.Items //TODO: also check items on other passing tests!
            Assert.AreEqual(newPlan.Id, refreshedSubscription.Plan.Id);
            Assert.AreEqual(1, refreshedSubscription.Quantity);

            //await fakeSubscriptionService
            //    .Received(1)
            //    .UpdateAsync(
            //        "some-subscription-id",
            //        Arg.Is<SubscriptionUpdateOptions>(args =>
            //            args.Prorate == true &&
            //            args.Items.Any(i => 
            //                i.Plan == "some-plan-id-1" &&
            //                i.Id == null &&
            //                i.Quantity == 1) &&
            //            args.Items.Any(i => 
            //                i.Plan == "some-plan-id-2" &&
            //                i.Id == null &&
            //                i.Quantity == 1)),
            //        default,
            //        default);
            Assert.Fail();
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
        public async Task Handle_DoggerInstanceWithPullRequestPresent_NothingAddedToSubscription()
        {
            //Arrange
            var doggerPlanId = Guid.NewGuid().ToString();
            var fakePullDogPlan = new PullDogPlan(
                doggerPlanId,
                1337,
                1);

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetSupportedPullDogPlansQuery>())
                .Returns(new[]
                {
                    fakePullDogPlan
                });

            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args =>
                    args.DoggerPlanId == doggerPlanId &&
                    args.PoolSize == 1))
                .Returns(fakePullDogPlan);

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(
                new DoggerEnvironmentSetupOptions()
                {
                    IocConfiguration = services =>
                    {
                        services.AddSingleton(fakeMediator);
                    }
                });
            
            var customer = await environment.Stripe.CustomerBuilder.BuildAsync();
            var plan = await environment.Stripe.PlanBuilder
                .WithId($"{doggerPlanId}_v3")
                .BuildAsync();

            var subscription = await environment.Stripe.SubscriptionBuilder
                .WithPlan(plan)
                .WithCustomer(customer)
                .WithDefaultPaymentMethod(await environment.Stripe.PaymentMethodBuilder
                    .WithCustomer(customer)
                    .BuildAsync())
                .BuildAsync();

            fakeMediator
                .Send(Arg.Is<GetSubscriptionByIdQuery>(args =>
                    args.Id == subscription.Id))
                .Returns(subscription);

            var user = new TestUserBuilder()
                .WithStripeSubscriptionId(subscription.Id)
                .WithStripeCustomerId(customer.Id)
                .Build();
            user.Clusters.Add(new TestClusterBuilder()
                .WithUser(user)
                .WithInstances(new TestInstanceBuilder()
                    .WithPlanId("some-plan-id")
                    .WithPullDogPullRequest(new TestPullDogPullRequestBuilder()
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithPullDogSettings(new TestPullDogSettingsBuilder()
                                .WithPlanId(doggerPlanId)
                                .WithPoolSize(1)
                                .WithUser(user))))));

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            var refreshedSubscription = await environment.Stripe.SubscriptionService.GetAsync(subscription.Id);
            Assert.AreEqual(plan.Id, refreshedSubscription.Plan.Id);

            Assert.AreEqual(subscription.Items.Single().Id, refreshedSubscription.Items.Single().Id);
            Assert.AreEqual(1, refreshedSubscription.Items.Single().Quantity);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullDogPresentWithPaidPlan_PullDogAddedToSubscription()
        {
            //Arrange
            var planId = Guid.NewGuid().ToString();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();
            
            var customer = await environment.Stripe.CustomerBuilder.BuildAsync();
            var plan = await environment.Stripe.PlanBuilder
                .WithId($"{planId}_v3")
                .BuildAsync();

            var subscription = await environment.Stripe.SubscriptionBuilder
                .WithPlan(plan)
                .WithCustomer(customer)
                .WithDefaultPaymentMethod(await environment.Stripe.PaymentMethodBuilder
                    .WithCustomer(customer)
                    .BuildAsync())
                .BuildAsync();

            var user = new TestUserBuilder()
                .WithStripeSubscriptionId(subscription.Id)
                .WithPullDogSettings(new TestPullDogSettingsBuilder()
                    .WithPoolSize(2)
                    .WithPlanId(planId))
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
                .WithPlan(initialPlan)
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

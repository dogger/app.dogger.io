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
using Dogger.Domain.Queries.Users.GetUserById;
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

            var customer = await environment.Stripe.CustomerBuilder
                .WithDefaultPaymentMethod(environment.Stripe.PaymentMethodBuilder)
                .BuildAsync();

            var newPlan1 = await environment.Stripe.PlanBuilder.BuildAsync();
            var newPlan2 = await environment.Stripe.PlanBuilder.BuildAsync();

            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
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
            var refreshedCustomer = await environment.Stripe.CustomerService.GetAsync(customer.Id);

            var refreshedSubscription = await environment.Stripe.SubscriptionService.GetAsync(refreshedCustomer
                .Subscriptions
                .Single()
                .Id);
            Assert.AreEqual("active", refreshedSubscription.Status);

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
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var customer = await environment.Stripe.CustomerBuilder
                .WithDefaultPaymentMethod(environment.Stripe.PaymentMethodBuilder)
                .BuildAsync();

            var newPlan = await environment.Stripe.PlanBuilder.BuildAsync();

            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
                .WithClusters(new TestClusterBuilder()
                    .WithInstances(
                        new TestInstanceBuilder()
                            .WithPlanId(newPlan.Id),
                        new TestInstanceBuilder()
                            .WithPlanId(newPlan.Id)))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            var refreshedCustomer = await environment.Stripe.CustomerService.GetAsync(customer.Id);

            var refreshedSubscription = await environment.Stripe.SubscriptionService.GetAsync(refreshedCustomer
                .Subscriptions
                .Single()
                .Id);
            Assert.AreEqual("active", refreshedSubscription.Status);

            Assert.AreEqual(1, refreshedSubscription.Items.Count());

            var refreshedNewPlan = refreshedSubscription.Items.Single();
            Assert.AreEqual(2, refreshedNewPlan.Quantity);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullDogPresentWithPaidPlan_PullDogAddedToSubscription()
        {
            //Arrange
            var planId = Guid.NewGuid().ToString();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetBundlesAsync(Arg.Any<GetBundlesRequest>())
                .Returns(new GetBundlesResponse()
                {
                    Bundles = new List<Bundle>()
                    {
                        new Bundle()
                        {
                            BundleId = planId,
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
                .WithStripeCustomerId(customer.Id)
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
            Assert.AreEqual("active", refreshedSubscription.Status);
            Assert.AreEqual(plan.Id, refreshedSubscription.Plan.Id);
            Assert.AreEqual(2, refreshedSubscription.Quantity);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullDogPresentWithPaidPlanAndDowngradedToDemoPlan_SubscriptionIsCancelled()
        {
            //Arrange
            var planId = Guid.NewGuid().ToString();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetBundlesAsync(Arg.Any<GetBundlesRequest>())
                .Returns(new GetBundlesResponse()
                {
                    Bundles = new List<Bundle>()
                    {
                        new Bundle()
                        {
                            BundleId = planId,
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
                .WithStripeCustomerId(customer.Id)
                .WithStripeSubscriptionId(subscription.Id)
                .WithPullDogSettings(new TestPullDogSettingsBuilder()
                    .WithPoolSize(0)
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
            Assert.AreEqual("canceled", refreshedSubscription.Status);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullDogPresentWithDemoPlan_NoPullDogAddedToSubscription()
        {
            //Arrange
            var planId = Guid.NewGuid().ToString();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetBundlesAsync(Arg.Any<GetBundlesRequest>())
                .Returns(new GetBundlesResponse()
                {
                    Bundles = new List<Bundle>()
                    {
                        new Bundle()
                        {
                            BundleId = planId,
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

            await environment.Stripe.PlanBuilder
                .WithId("512_v3")
                .BuildAsync();
            
            var customer = await environment.Stripe.CustomerBuilder.BuildAsync();

            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
                .WithPullDogSettings(new TestPullDogSettingsBuilder()
                    .WithPoolSize(0)
                    .WithPlanId(planId))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            var refreshedCustomer = await environment.Stripe.CustomerService.GetAsync(customer.Id);
            Assert.AreEqual(0, refreshedCustomer.Subscriptions.Count());
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
            Assert.AreEqual("active", refreshedSubscription.Status);
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
            var oldPlanId = Guid.NewGuid().ToString();
            var newPlanId = Guid.NewGuid().ToString();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetBundlesAsync(Arg.Any<GetBundlesRequest>())
                .Returns(new GetBundlesResponse()
                {
                    Bundles = new List<Bundle>()
                    {
                        new Bundle()
                        {
                            BundleId = oldPlanId,
                            IsActive = true,
                            RamSizeInGb = 0.5f,
                            SupportedPlatforms = new List<string>()
                            {
                                "LINUX_UNIX"
                            }
                        },
                        new Bundle()
                        {
                            BundleId = newPlanId,
                            IsActive = true,
                            RamSizeInGb = 1f,
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

            var oldPlan = await environment.Stripe.PlanBuilder
                .WithId("512_v3")
                .BuildAsync();

            var newPlan = await environment.Stripe.PlanBuilder
                .WithId("1024_v3")
                .BuildAsync();

            var subscription = await environment.Stripe.SubscriptionBuilder
                .WithPlans(oldPlan)
                .WithCustomer(customer)
                .WithDefaultPaymentMethod(await environment.Stripe.PaymentMethodBuilder
                    .WithCustomer(customer)
                    .BuildAsync())
                .BuildAsync();

            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
                .WithStripeSubscriptionId(subscription.Id)
                .WithPullDogSettings(new TestPullDogSettingsBuilder()
                    .WithPoolSize(1)
                    .WithPlanId(newPlanId))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            var refreshedSubscription = await environment.Stripe.SubscriptionService.GetAsync(subscription.Id);
            Assert.AreEqual("active", refreshedSubscription.Status);
            Assert.AreEqual(newPlan.Id, refreshedSubscription.Plan.Id);
            Assert.AreEqual(1, refreshedSubscription.Quantity);
            Assert.AreEqual(1, refreshedSubscription.Items.Count());
            Assert.AreEqual(newPlan.Id, refreshedSubscription.Items.Single().Plan.Id);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoStripeSubscriptionPresentAlreadyAndClusterWithPaidInstance_StripeSubscriptionCreated()
        {
            //Arrange
            var planId = Guid.NewGuid().ToString();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetBundlesAsync(Arg.Any<GetBundlesRequest>())
                .Returns(new GetBundlesResponse()
                {
                    Bundles = new List<Bundle>()
                    {
                        new Bundle()
                        {
                            BundleId = planId,
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

            await environment.Stripe.PlanBuilder
                .WithId("512_v3")
                .BuildAsync();

            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
                .WithStripeSubscriptionId(null)
                .WithClusters(new TestClusterBuilder()
                    .WithInstances(new TestInstanceBuilder()
                        .WithPlanId(planId)))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(new UpdateUserSubscriptionCommand(user.Id));

            //Assert
            var refreshedUser = await environment.Mediator.Send(new GetUserByIdQuery(user.Id));
            var createdSubscription = await environment.Stripe.SubscriptionService.GetAsync(refreshedUser.StripeSubscriptionId);
            Assert.IsNotNull(createdSubscription);
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

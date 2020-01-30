using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.UpdateUserSubscription;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Domain.Queries.Plans.GetSupportedPullDogPlans;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.Extensions;
using Stripe;

namespace Dogger.Tests.Domain.Commands.Payment
{
    [TestClass]
    public class UpdateUserSubscriptionCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_DoggerInstancesPresentWithDifferentPlanIds_DoggerInstancesAddedToDifferentSubscriptionItems()
        {
            //Arrange
            var fakeSubscriptionService = Substitute.ForPartsOf<SubscriptionService>();
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

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeSubscriptionService)
                });

            var user = new User()
            {
                StripeSubscriptionId = "some-subscription-id",
                StripeCustomerId = "dummy",
                Clusters = new List<Cluster>()
                {
                    new Cluster()
                    {
                        Instances = new List<Instance>()
                        {
                            new Instance()
                            {
                                PlanId = "some-plan-id-1",
                                Name = "dummy-1"
                            },
                            new Instance()
                            {
                                PlanId = "some-plan-id-2",
                                Name = "dummy-2"
                            }
                        }
                    }
                }
            };
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
                        args.Items[0].Quantity == 1 &&
                        args.Items[1].Plan == "some-plan-id-2" &&
                        args.Items[1].Id == null &&
                        args.Items[1].Quantity == 1),
                    default,
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_DoggerInstancesPresentWithSamePlanIds_DoggerInstancesAddedToSameSubscriptionItem()
        {
            //Arrange
            var fakeSubscriptionService = Substitute.ForPartsOf<SubscriptionService>();
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

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeSubscriptionService)
                });

            var user = new User()
            {
                StripeSubscriptionId = "some-subscription-id",
                StripeCustomerId = "dummy",
                Clusters = new List<Cluster>()
                {
                    new Cluster()
                    {
                        Instances = new List<Instance>()
                        {
                            new Instance()
                            {
                                PlanId = "some-plan-id-1",
                                Name = "dummy-1"
                            },
                            new Instance()
                            {
                                PlanId = "some-plan-id-1",
                                Name = "dummy-2"
                            }
                        }
                    }
                }
            };
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
            var fakePullDogPlan = new PullDogPlan(
                "some-pull-dog-plan",
                1337,
                1);

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetSupportedPullDogPlansQuery>())
                .Returns(new []
                {
                    fakePullDogPlan
                });

            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args =>
                    args.DoggerPlanId == "some-plan-id" &&
                    args.PoolSize == 1))
                .Returns(fakePullDogPlan);

            var fakeSubscriptionService = Substitute.ForPartsOf<SubscriptionService>();
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
                        {
                            new SubscriptionItem()
                            {
                                Id = "some-subscription-item",
                                Plan = new Stripe.Plan()
                                {
                                    Id = "some-pull-dog-plan"
                                }
                            }
                        }
                    }
                });

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services =>
                    {
                        services.AddSingleton(fakeSubscriptionService);
                        services.AddSingleton(fakeMediator);
                    }
                });

            var user = new User()
            {
                StripeSubscriptionId = "some-subscription-id",
                StripeCustomerId = "dummy"
            };
            user.Clusters.Add(new Cluster() {
                User = user,
                Instances = new List<Instance>()
                {
                    new Instance()
                    {
                        PlanId = "some-plan-id",
                        Name = "dummy",
                        PullDogPullRequest = new PullDogPullRequest()
                        {
                            Handle = "dummy",
                            PullDogRepository = new PullDogRepository()
                            {
                                Handle = "dummy",
                                PullDogSettings = new PullDogSettings()
                                {
                                    EncryptedApiKey = Array.Empty<byte>(),
                                    PlanId = "some-plan-id",
                                    PoolSize = 1,
                                    User = user
                                }
                            }
                        }
                    }
                }
            });

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
                        args.Items[0].Plan == "some-pull-dog-plan" &&
                        args.Items[0].Id == "some-subscription-item" &&
                        args.Items[0].Quantity == 1),
                    default,
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullDogPresentWithPaidPlan_PullDogAddedToSubscription()
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

            var fakeSubscriptionService = Substitute.ForPartsOf<SubscriptionService>();
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

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services =>
                    {
                        services.AddSingleton(fakeSubscriptionService);
                        services.AddSingleton(fakeMediator);
                    }
                });

            var user = new User()
            {
                StripeSubscriptionId = "some-subscription-id",
                StripeCustomerId = "dummy",
                PullDogSettings = new PullDogSettings()
                {
                    PoolSize = 2,
                    PlanId = "some-pull-dog-plan",
                    EncryptedApiKey = Array.Empty<byte>()
                }
            };
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
                        args.Items[0].Plan == "some-pull-dog-plan" &&
                        args.Items[0].Id == null &&
                        args.Items[0].Quantity == 1),
                    default,
                    default);
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

            var fakeSubscriptionService = Substitute.ForPartsOf<SubscriptionService>();
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

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services =>
                    {
                        services.AddSingleton(fakeSubscriptionService);
                        services.AddSingleton(fakeMediator);
                    }
                });

            var user = new User()
            {
                StripeSubscriptionId = null,
                StripeCustomerId = "dummy",
                PullDogSettings = new PullDogSettings()
                {
                    PoolSize = 0,
                    PlanId = "dummy",
                    EncryptedApiKey = Array.Empty<byte>()
                }
            };
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
            var fakeSubscriptionService = Substitute.ForPartsOf<SubscriptionService>();
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
                        {
                            new SubscriptionItem()
                            {
                                Id = "some-existing-subscription-item-id",
                                Plan = new Stripe.Plan()
                                {
                                    Id = "some-existing-plan-id"
                                }
                            }
                        }
                    }
                });

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeSubscriptionService)
                });

            var user = new User()
            {
                StripeSubscriptionId = "some-subscription-id",
                StripeCustomerId = "dummy",
                Clusters = new List<Cluster>()
                {
                    new Cluster()
                    {
                        Instances = new List<Instance>()
                        {
                            new Instance()
                            {
                                PlanId = "some-new-plan-id",
                                Name = "dummy"
                            }
                        }
                    }
                }
            };
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
                        args.Items[0].Plan == "some-new-plan-id" &&
                        args.Items[0].Id == null &&
                        args.Items[0].Quantity == 1 &&
                        args.Items[1].Id == "some-existing-subscription-item-id" &&
                        args.Items[1].Deleted == true),
                    default,
                    default);
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
                .Returns(new [] {
                    new PullDogPlan(
                        "some-old-pull-dog-plan",
                        1337,
                        2),
                    new PullDogPlan(
                        "some-new-pull-dog-plan",
                        1337,
                        5)
                });

            var fakeSubscriptionService = Substitute.ForPartsOf<SubscriptionService>();
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
                        {
                            new SubscriptionItem()
                            {
                                Id = "some-subscription-item-id",
                                Plan = new Stripe.Plan()
                                {
                                    Id = "some-old-pull-dog-plan"
                                }
                            }
                        }
                    }
                });

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services =>
                    {
                        services.AddSingleton(fakeSubscriptionService);
                        services.AddSingleton(fakeMediator);
                    }
                });

            var user = new User()
            {
                StripeSubscriptionId = "some-subscription-id",
                StripeCustomerId = "dummy",
                PullDogSettings = new PullDogSettings()
                {
                    PoolSize = 5,
                    PlanId = "some-pull-dog-plan",
                    EncryptedApiKey = Array.Empty<byte>()
                }
            };
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
                        args.Items[0].Plan == "some-new-pull-dog-plan" &&
                        args.Items[0].Id == "some-subscription-item-id" &&
                        args.Items[0].Quantity == 1),
                    default,
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoStripeSubscriptionPresent_StripeSubscriptionCreated()
        {
            //Arrange
            var fakeSubscriptionService = Substitute.ForPartsOf<SubscriptionService>();
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

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeSubscriptionService)
                });

            var user = new User()
            {
                StripeCustomerId = "some-user-id",
                StripeSubscriptionId = null,
                Clusters = new List<Cluster>()
                {
                    new Cluster()
                    {
                        Instances = new List<Instance>()
                        {
                            new Instance()
                            {
                                PlanId = "some-plan-id",
                                Name = "dummy"
                            }
                        }
                    }
                }
            };
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
                        args.Customer == "some-user-id" &&
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
            var fakeSubscriptionService = Substitute.ForPartsOf<SubscriptionService>();
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

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeSubscriptionService)
                });

            var user = new User()
            {
                StripeCustomerId = "some-user-id",
                StripeSubscriptionId = null,
                Clusters = new List<Cluster>()
                {
                    new Cluster()
                    {
                        Instances = new List<Instance>()
                        {
                            new Instance()
                            {
                                PlanId = "some-plan-id",
                                Name = "dummy"
                            }
                        }
                    }
                }
            };
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

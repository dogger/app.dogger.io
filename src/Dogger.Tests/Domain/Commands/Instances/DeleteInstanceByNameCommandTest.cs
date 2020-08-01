using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.GitHub;
using Dogger.Infrastructure.GitHub.Octokit;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Octokit;
using Stripe;
using Instance = Dogger.Domain.Models.Instance;
using NotFoundException = Amazon.Lightsail.Model.NotFoundException;
using User = Dogger.Domain.Models.User;

namespace Dogger.Tests.Domain.Commands.Instances
{
    [TestClass]
    public class DeleteInstanceByNameCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProperArgumentsGiven_LightsailDeletionOperationIsAwaited()
        {
            //Arrange
            var fakeOperations = new List<Operation>
            {
                new Operation() {Id = "some-operation-id-1"},
                new Operation() {Id = "some-operation-id-2"}
            };

            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .DeleteInstanceAsync(
                    Arg.Any<DeleteInstanceRequest>(),
                    default)
                .Returns(new DeleteInstanceResponse()
                {
                    Operations = fakeOperations
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonLightsailClient);
                    services.AddSingleton(fakeLightsailOperationService);
                }
            });

            //Act
            await environment.Mediator.Send(
                new DeleteInstanceByNameCommand("some-name", InitiatorType.System),
                default);

            //Assert
            await fakeLightsailOperationService
                .Received(1)
                .WaitForOperationsAsync(fakeOperations);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_DatabaseInstancePresentAndAmazonExceptionThrown_NothingIsRemovedFromDatabase()
        {
            //Arrange
            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .DeleteInstanceAsync(
                    Arg.Any<DeleteInstanceRequest>(),
                    default)
                .Throws<TestException>();

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonLightsailClient);
                    services.AddSingleton(fakeLightsailOperationService);
                }
            });

            var clusterId = Guid.NewGuid();
            var cluster = new Cluster()
            {
                Id = clusterId,
                Instances = new List<Instance>()
                {
                    new Instance()
                    {
                        Name = "not-matching",
                        PlanId = "dummy"
                    },
                    new Instance()
                    {
                        Name = "some-instance-name",
                        PlanId = "dummy"
                    }
                }
            };

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(cluster);
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () =>
                await environment.Mediator.Send(
                    new DeleteInstanceByNameCommand("some-instance-name", InitiatorType.System),
                    default));

            //Assert
            Assert.IsNotNull(exception);

            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedCluster = await dataContext
                    .Clusters
                    .Include(x => x.Instances)
                    .FirstOrDefaultAsync(x => x.Id == clusterId);
                var refreshedInstance = await dataContext.Instances.FirstOrDefaultAsync(x => x.Name == "some-instance-name");

                Assert.IsNotNull(refreshedInstance);
                Assert.IsNotNull(refreshedCluster);

                Assert.AreEqual(2, refreshedCluster.Instances.Count);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_DatabaseInstancesPresentWithPullRequestsAndSystemInitiated_InstanceAndPullRequestIsRemovedFromClusterAndDatabase()
        {
            //Arrange
            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .DeleteInstanceAsync(
                    Arg.Any<DeleteInstanceRequest>(),
                    default)
                .Returns(new DeleteInstanceResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            var fakeGitHubClient = Substitute.For<IGitHubClient>();
            fakeGitHubClient
                .GitHubApps
                .CreateInstallationToken(Arg.Any<long>())
                .Returns(new AccessToken("dummy", DateTimeOffset.MaxValue));

            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubInstallationClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubInstallationClient
                .PullRequest
                .Get(
                    1338,
                    1339)
                .Returns(new PullRequestBuilder()
                    .WithHead(
                        CreateGitReferenceDto(
                            CreateRepositoryDto(
                                1338)))
                    .WithBase(
                        CreateGitReferenceDto(
                            CreateRepositoryDto(
                                1338)))
                    .Build());

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonLightsailClient);
                    services.AddSingleton(fakeLightsailOperationService);
                    services.AddSingleton(fakeGitHubClient);
                    services.AddSingleton(fakeGitHubClientFactory);
                    services.AddSingleton(fakePullDogFileCollectorFactory);
                }
            });

            var clusterId = Guid.NewGuid();
            var cluster = new Cluster()
            {
                Id = clusterId,
                Instances = new List<Instance>()
                {
                    new Instance()
                    {
                        Name = "not-matching",
                        PlanId = "dummy",
                        PullDogPullRequest = new PullDogPullRequest()
                        {
                            Handle = "dummy",
                            PullDogRepository = new PullDogRepository()
                            {
                                Handle = "dummy",
                                PullDogSettings = new PullDogSettings()
                                {
                                    PlanId = "dummy",
                                    EncryptedApiKey = Array.Empty<byte>(),
                                    User = new User()
                                    {
                                        StripeCustomerId = "dummy"
                                    }
                                }
                            }
                        }
                    },
                    new Instance()
                    {
                        Name = "some-instance-name",
                        PlanId = "dummy",
                        PullDogPullRequest = new PullDogPullRequest()
                        {
                            Handle = "1339",
                            PullDogRepository = new PullDogRepository()
                            {
                                Handle = "1338",
                                GitHubInstallationId = 1337,
                                PullDogSettings = new PullDogSettings()
                                {
                                    PlanId = "dummy",
                                    EncryptedApiKey = Array.Empty<byte>(),
                                    User = new User()
                                    {
                                        StripeCustomerId = "dummy"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(cluster);
            });

            //Act
            await environment.Mediator.Send(
                new DeleteInstanceByNameCommand("some-instance-name", InitiatorType.System),
                default);

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedCluster = await dataContext
                    .Clusters
                    .Include(x => x.Instances)
                    .FirstOrDefaultAsync(x => x.Id == clusterId);
                var refreshedInstance = await dataContext.Instances.FirstOrDefaultAsync(x => x.Name == "some-instance-name");

                Assert.IsNull(refreshedInstance);
                Assert.IsNotNull(refreshedCluster);

                Assert.AreEqual(1, refreshedCluster.Instances.Count);
                Assert.AreNotEqual("some-instance-name", refreshedCluster.Instances.Single().Name);

                Assert.AreEqual(1, await dataContext.PullDogPullRequests.CountAsync());
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_DatabaseInstancesPresentWithPullRequestsAndUserInitiated_InstanceIsRemovedFromClusterAndDatabase()
        {
            //Arrange
            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .DeleteInstanceAsync(
                    Arg.Any<DeleteInstanceRequest>(),
                    default)
                .Returns(new DeleteInstanceResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            var fakeGitHubClient = Substitute.For<IGitHubClient>();
            fakeGitHubClient
                .GitHubApps
                .CreateInstallationToken(Arg.Any<long>())
                .Returns(new AccessToken("dummy", DateTimeOffset.MaxValue));

            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubInstallationClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubInstallationClient
                .PullRequest
                .Get(
                    1338,
                    1339)
                .Returns(new PullRequestBuilder()
                    .WithHead(
                        CreateGitReferenceDto(
                            CreateRepositoryDto(
                                1338)))
                    .WithBase(
                        CreateGitReferenceDto(
                            CreateRepositoryDto(
                                1338)))
                    .Build());

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonLightsailClient);
                    services.AddSingleton(fakeLightsailOperationService);
                    services.AddSingleton(fakeGitHubClient);
                    services.AddSingleton(fakeGitHubClientFactory);
                    services.AddSingleton(fakePullDogFileCollectorFactory);
                }
            });

            var clusterId = Guid.NewGuid();
            var cluster = new Cluster()
            {
                Id = clusterId,
                Instances = new List<Instance>()
                {
                    new Instance()
                    {
                        Name = "not-matching",
                        PlanId = "dummy",
                        PullDogPullRequest = new PullDogPullRequest()
                        {
                            Handle = "dummy",
                            PullDogRepository = new PullDogRepository()
                            {
                                Handle = "dummy",
                                PullDogSettings = new PullDogSettings()
                                {
                                    PlanId = "dummy",
                                    EncryptedApiKey = Array.Empty<byte>(),
                                    User = new User()
                                    {
                                        StripeCustomerId = "dummy"
                                    }
                                }
                            }
                        }
                    },
                    new Instance()
                    {
                        Name = "some-instance-name",
                        PlanId = "dummy",
                        PullDogPullRequest = new PullDogPullRequest()
                        {
                            Handle = "1339",
                            PullDogRepository = new PullDogRepository()
                            {
                                Handle = "1338",
                                GitHubInstallationId = 1337,
                                PullDogSettings = new PullDogSettings()
                                {
                                    PlanId = "dummy",
                                    EncryptedApiKey = Array.Empty<byte>(),
                                    User = new User()
                                    {
                                        StripeCustomerId = "dummy"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(cluster);
            });

            //Act
            await environment.Mediator.Send(
                new DeleteInstanceByNameCommand("some-instance-name", InitiatorType.User),
                default);

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedCluster = await dataContext
                    .Clusters
                    .Include(x => x.Instances)
                    .FirstOrDefaultAsync(x => x.Id == clusterId);
                var refreshedInstance = await dataContext.Instances.FirstOrDefaultAsync(x => x.Name == "some-instance-name");

                Assert.IsNull(refreshedInstance);
                Assert.IsNotNull(refreshedCluster);

                Assert.AreEqual(1, refreshedCluster.Instances.Count);
                Assert.AreNotEqual("some-instance-name", refreshedCluster.Instances.Single().Name);

                Assert.AreEqual(2, await dataContext.PullDogPullRequests.CountAsync());
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_DatabaseInstancePresent_InstanceIsRemovedFromClusterAndDatabase()
        {
            //Arrange
            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .DeleteInstanceAsync(
                    Arg.Any<DeleteInstanceRequest>(),
                    default)
                .Returns(new DeleteInstanceResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonLightsailClient);
                    services.AddSingleton(fakeLightsailOperationService);
                }
            });

            var clusterId = Guid.NewGuid();
            var cluster = new Cluster()
            {
                Id = clusterId,
                Instances = new List<Instance>()
                {
                    new Instance()
                    {
                        Name = "not-matching",
                        PlanId = "dummy"
                    },
                    new Instance()
                    {
                        Name = "some-instance-name",
                        PlanId = "dummy"
                    }
                }
            };

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(cluster);
            });

            //Act
            await environment.Mediator.Send(
                new DeleteInstanceByNameCommand("some-instance-name", InitiatorType.System),
                default);

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedCluster = await dataContext
                    .Clusters
                    .Include(x => x.Instances)
                    .FirstOrDefaultAsync(x => x.Id == clusterId);
                var refreshedInstance = await dataContext.Instances.FirstOrDefaultAsync(x => x.Name == "some-instance-name");

                Assert.IsNull(refreshedInstance);
                Assert.IsNotNull(refreshedCluster);

                Assert.AreEqual(1, refreshedCluster.Instances.Count);
                Assert.AreNotEqual("some-instance-name", refreshedCluster.Instances.Single().Name);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NonProvisionedNonFreeInstancePresent_SubscriptionIsRemoved()
        {
            //Arrange
            var fakeInstanceId = Guid.NewGuid();

            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .DeleteInstanceAsync(
                    Arg.Any<DeleteInstanceRequest>(),
                    default)
                .Returns(new DeleteInstanceResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonLightsailClient);
                    services.AddSingleton(fakeLightsailOperationService);
                }
            });

            var stripeCustomerService = environment.ServiceProvider.GetRequiredService<CustomerService>();
            var customer = await stripeCustomerService.CreateAsync(new CustomerCreateOptions()
            {
                Email = "dummy@example.com"
            });

            var stripePaymentMethodService = environment.ServiceProvider.GetRequiredService<PaymentMethodService>();
            var paymentMethod = await stripePaymentMethodService.AttachAsync("pm_card_visa", new PaymentMethodAttachOptions()
            {
                Customer = customer.Id
            });

            var stripeSubscriptionService = environment.ServiceProvider.GetRequiredService<SubscriptionService>();
            var subscription = await stripeSubscriptionService.CreateAsync(new SubscriptionCreateOptions()
            {
                Customer = customer.Id,
                DefaultPaymentMethod = paymentMethod.Id,
                Items = new List<SubscriptionItemOptions>()
                {
                    new SubscriptionItemOptions()
                    {
                        Plan = "nano_2_0"
                    }
                },
                Metadata = new Dictionary<string, string>()
                {
                    {
                        "InstanceId", fakeInstanceId.ToString()
                    }
                }
            });

            var clusterId = Guid.NewGuid();
            var user = new User()
            {
                StripeCustomerId = customer.Id
            };

            var cluster = new Cluster()
            {
                Id = clusterId,
                User = user,
                UserId = user.Id
            };
            user.Clusters.Add(cluster);

            var instance = new Instance()
            {
                Id = fakeInstanceId,
                Name = "some-instance-name",
                PlanId = "dummy",
                Cluster = cluster,
                ClusterId = cluster.Id,
                IsProvisioned = false
            };
            cluster.Instances.Add(instance);

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(cluster);
                await dataContext.Instances.AddAsync(instance);
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(
                new DeleteInstanceByNameCommand("some-instance-name", InitiatorType.System),
                default);

            //Assert
            var refreshedSubscription = await stripeSubscriptionService.GetAsync(subscription.Id);
            Assert.AreEqual(refreshedSubscription.Status, subscription.Status);
            Assert.AreNotEqual("canceled", refreshedSubscription.Status);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProvisionedFreeInstancePresent_SubscriptionIsUpdated()
        {
            //Arrange
            var fakeInstanceId = Guid.NewGuid();

            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .DeleteInstanceAsync(
                    Arg.Any<DeleteInstanceRequest>(),
                    default)
                .Returns(new DeleteInstanceResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonLightsailClient);
                    services.AddSingleton(fakeLightsailOperationService);
                }
            });

            var stripeCustomerService = environment.ServiceProvider.GetRequiredService<CustomerService>();
            var customer = await stripeCustomerService.CreateAsync(new CustomerCreateOptions()
            {
                Email = "dummy@example.com"
            });

            var stripePaymentMethodService = environment.ServiceProvider.GetRequiredService<PaymentMethodService>();
            var paymentMethod = await stripePaymentMethodService.AttachAsync("pm_card_visa", new PaymentMethodAttachOptions()
            {
                Customer = customer.Id
            });

            var stripeSubscriptionService = environment.ServiceProvider.GetRequiredService<SubscriptionService>();
            var subscription = await stripeSubscriptionService.CreateAsync(new SubscriptionCreateOptions()
            {
                Customer = customer.Id,
                DefaultPaymentMethod = paymentMethod.Id,
                Items = new List<SubscriptionItemOptions>()
                {
                    new SubscriptionItemOptions()
                    {
                        Plan = "nano_2_0"
                    }
                },
                Metadata = new Dictionary<string, string>()
                {
                    {
                        "InstanceId", fakeInstanceId.ToString()
                    }
                }
            });

            var clusterId = Guid.NewGuid();
            var user = new User()
            {
                StripeCustomerId = customer.Id
            };

            var cluster = new Cluster()
            {
                Id = clusterId,
                User = user,
                UserId = user.Id
            };
            user.Clusters.Add(cluster);

            var instance = new Instance()
            {
                Id = fakeInstanceId,
                Name = "some-instance-name",
                PlanId = "dummy",
                Cluster = cluster,
                ClusterId = cluster.Id,
                IsProvisioned = true
            };
            cluster.Instances.Add(instance);

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(cluster);
                await dataContext.Instances.AddAsync(instance);
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(
                new DeleteInstanceByNameCommand("some-instance-name", InitiatorType.System),
                default);

            //Assert
            var refreshedSubscription = await stripeSubscriptionService.GetAsync(subscription.Id);
            Assert.AreEqual(refreshedSubscription.Status, subscription.Status);
            Assert.AreNotEqual("canceled", refreshedSubscription.Status);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProvisionedNonFreeInstancePresent_SubscriptionIsUpdated()
        {
            //Arrange
            var fakeInstanceId = Guid.NewGuid();

            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .DeleteInstanceAsync(
                    Arg.Any<DeleteInstanceRequest>(),
                    default)
                .Returns(new DeleteInstanceResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonLightsailClient);
                    services.AddSingleton(fakeLightsailOperationService);
                }
            });

            var stripeCustomerService = environment.ServiceProvider.GetRequiredService<CustomerService>();
            var customer = await stripeCustomerService.CreateAsync(new CustomerCreateOptions()
            {
                Email = "dummy@example.com"
            });

            var stripePaymentMethodService = environment.ServiceProvider.GetRequiredService<PaymentMethodService>();
            var paymentMethod = await stripePaymentMethodService.AttachAsync("pm_card_visa", new PaymentMethodAttachOptions()
            {
                Customer = customer.Id
            });

            var stripeSubscriptionService = environment.ServiceProvider.GetRequiredService<SubscriptionService>();
            var subscription = await stripeSubscriptionService.CreateAsync(new SubscriptionCreateOptions()
            {
                Customer = customer.Id,
                DefaultPaymentMethod = paymentMethod.Id,
                Items = new List<SubscriptionItemOptions>()
                {
                    new SubscriptionItemOptions()
                    {
                        Plan = "nano_2_0"
                    }
                },
                Metadata = new Dictionary<string, string>()
                {
                    {
                        "InstanceId", fakeInstanceId.ToString()
                    }
                }
            });

            var clusterId = Guid.NewGuid();
            var user = new User()
            {
                StripeCustomerId = customer.Id,
                StripeSubscriptionId = subscription.Id
            };

            var cluster = new Cluster()
            {
                Id = clusterId,
                User = user,
                UserId = user.Id
            };
            user.Clusters.Add(cluster);

            var instance = new Instance()
            {
                Id = fakeInstanceId,
                Name = "some-instance-name",
                PlanId = "dummy",
                Cluster = cluster,
                ClusterId = cluster.Id,
                IsProvisioned = true
            };
            cluster.Instances.Add(instance);

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(cluster);
                await dataContext.Instances.AddAsync(instance);
                await dataContext.Users.AddAsync(user);
            });

            //Act
            await environment.Mediator.Send(
                new DeleteInstanceByNameCommand("some-instance-name", InitiatorType.System),
                default);

            //Assert
            var refreshedSubscription = await stripeSubscriptionService.GetAsync(subscription.Id);
            Assert.AreNotEqual(refreshedSubscription.Status, subscription.Status);
            Assert.AreEqual("canceled", refreshedSubscription.Status);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_LightsailInstanceNotFound_NoExceptionThrown()
        {
            //Arrange
            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .DeleteInstanceAsync(
                    Arg.Any<DeleteInstanceRequest>(),
                    default)
                .Throws(new NotFoundException("Not found"));

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonLightsailClient);
                    services.AddSingleton(fakeLightsailOperationService);
                }
            });

            //Act
            await environment.Mediator.Send(
                new DeleteInstanceByNameCommand("some-name", InitiatorType.System),
                default);

            //Assert
            await fakeLightsailOperationService
                .DidNotReceiveWithAnyArgs()
                .WaitForOperationsAsync(default);
        }

        private static Repository CreateRepositoryDto(
            long id)
        {
            return new RepositoryBuilder()
                .WithId(id)
                .Build();
        }

        private static GitReference CreateGitReferenceDto(
            Repository repository)
        {
            return new GitReference(
                default,
                default,
                default,
                default,
                default,
                default,
                repository);
        }
    }
}

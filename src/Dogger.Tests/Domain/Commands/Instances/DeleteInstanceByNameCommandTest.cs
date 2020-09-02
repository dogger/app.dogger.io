using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.GitHub;
using Dogger.Infrastructure.GitHub.Octokit;
using Dogger.Infrastructure.Ioc;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Octokit;
using Stripe;
using NotFoundException = Amazon.Lightsail.Model.NotFoundException;

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
            var cluster = new TestClusterBuilder()
                .WithId(clusterId)
                .WithInstances(
                    new TestInstanceBuilder()
                        .WithName("not-matching"),
                    new TestInstanceBuilder()
                        .WithName("some-instance-name"))
                .Build();

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
                var refreshedInstance = await dataContext
                    .Instances
                    .AsQueryable()
                    .FirstOrDefaultAsync(x => x.Name == "some-instance-name");

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
                                1338))));

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
            var cluster = new TestClusterBuilder()
                .WithId(clusterId)
                .WithInstances(
                    new TestInstanceBuilder()
                        .WithPullDogPullRequest()
                        .WithName("not-matching"),
                    new TestInstanceBuilder()
                        .WithName("some-instance-name")
                        .WithPullDogPullRequest(new TestPullDogPullRequestBuilder()
                            .WithHandle("1339")
                            .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                                .WithHandle("1338")
                                .WithGitHubInstallationId(1337))))
                .Build();

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
                var refreshedInstance = await dataContext
                    .Instances
                    .AsQueryable()
                    .FirstOrDefaultAsync(x => x.Name == "some-instance-name");

                Assert.IsNull(refreshedInstance);
                Assert.IsNotNull(refreshedCluster);

                Assert.AreEqual(1, refreshedCluster.Instances.Count);
                Assert.AreNotEqual("some-instance-name", refreshedCluster.Instances.Single().Name);

                Assert.AreEqual(1, await dataContext
                    .PullDogPullRequests
                    .AsQueryable()
                    .CountAsync());
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
                                1338))));

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
            var cluster = new TestClusterBuilder()
                .WithId(clusterId)
                .WithInstances(
                    new TestInstanceBuilder()
                        .WithName("not-matching")
                        .WithPullDogPullRequest(new TestPullDogPullRequestBuilder()
                            .WithPullDogRepository()),
                    new TestInstanceBuilder()
                        .WithName("some-instance-name")
                        .WithPullDogPullRequest(new TestPullDogPullRequestBuilder()
                            .WithHandle("1339")
                            .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                                .WithHandle("1338")
                                .WithGitHubInstallationId(1337))))
                .Build();

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
                var refreshedInstance = await dataContext
                    .Instances
                    .AsQueryable()
                    .FirstOrDefaultAsync(x => x.Name == "some-instance-name");

                Assert.IsNull(refreshedInstance);
                Assert.IsNotNull(refreshedCluster);

                Assert.AreEqual(1, refreshedCluster.Instances.Count);
                Assert.AreNotEqual("some-instance-name", refreshedCluster.Instances.Single().Name);

                Assert.AreEqual(2, await dataContext
                    .PullDogPullRequests
                    .AsQueryable()
                    .CountAsync());
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
            var cluster = new TestClusterBuilder()
                .WithId(clusterId)
                .WithInstances(
                    new TestInstanceBuilder()
                        .WithName("not-matching"),
                    new TestInstanceBuilder()
                        .WithName("some-instance-name"))
                .Build();

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
                var refreshedInstance = await dataContext
                    .Instances
                    .AsQueryable()
                    .FirstOrDefaultAsync(x => x.Name == "some-instance-name");

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

            var stripeCustomerService = environment.ServiceProvider.GetRequiredService<IOptionalService<CustomerService>>();
            var customer = await stripeCustomerService.Value.CreateAsync(new CustomerCreateOptions()
            {
                Email = "dummy@example.com"
            });

            var stripePaymentMethodService = environment.ServiceProvider.GetRequiredService<IOptionalService<PaymentMethodService>>();
            var paymentMethod = await stripePaymentMethodService.Value.AttachAsync("pm_card_visa", new PaymentMethodAttachOptions()
            {
                Customer = customer.Id
            });

            var stripeSubscriptionService = environment.ServiceProvider.GetRequiredService<IOptionalService<SubscriptionService>>().Value;
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
            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
                .Build();

            var cluster = new TestClusterBuilder()
                .WithId(clusterId)
                .WithUser(user)
                .Build();
            user.Clusters.Add(cluster);

            var instance = new TestInstanceBuilder()
                .WithId(fakeInstanceId)
                .WithName("some-instance-name")
                .WithCluster(cluster)
                .WithProvisionedStatus(false)
                .Build();
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

            var stripeCustomerService = environment.ServiceProvider.GetRequiredService<IOptionalService<CustomerService>>();
            var customer = await stripeCustomerService.Value.CreateAsync(new CustomerCreateOptions()
            {
                Email = "dummy@example.com"
            });

            var stripePaymentMethodService = environment.ServiceProvider.GetRequiredService<IOptionalService<PaymentMethodService>>();
            var paymentMethod = await stripePaymentMethodService.Value.AttachAsync("pm_card_visa", new PaymentMethodAttachOptions()
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
            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
                .Build();

            var cluster = new TestClusterBuilder()
                .WithId(clusterId)
                .WithUser(user)
                .Build();
            user.Clusters.Add(cluster);

            var instance = new TestInstanceBuilder()
                .WithId(fakeInstanceId)
                .WithName("some-instance-name")
                .WithCluster(cluster)
                .WithProvisionedStatus(true)
                .Build();
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

            var stripeCustomerService = environment.ServiceProvider.GetRequiredService<IOptionalService<CustomerService>>();
            var customer = await stripeCustomerService.Value.CreateAsync(new CustomerCreateOptions()
            {
                Email = "dummy@example.com"
            });

            var stripePaymentMethodService = environment.ServiceProvider.GetRequiredService<IOptionalService<PaymentMethodService>>();
            var paymentMethod = await stripePaymentMethodService.Value.AttachAsync("pm_card_visa", new PaymentMethodAttachOptions()
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
            var user = new TestUserBuilder()
                .WithStripeCustomerId(customer.Id)
                .WithStripeSubscriptionId(subscription.Id)
                .Build();

            var cluster = new TestClusterBuilder()
                .WithId(clusterId)
                .WithUser(user)
                .Build();
            user.Clusters.Add(cluster);

            var instance = new TestInstanceBuilder()
                .WithId(fakeInstanceId)
                .WithName("some-instance-name")
                .WithCluster(cluster)
                .WithProvisionedStatus(true)
                .Build();
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

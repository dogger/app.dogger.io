using System;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Queries.Instances.GetProvisionedClustersWithInstancesForUser;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Instances
{
    [TestClass]
    public class GetProvisionedClustersWithInstancesForUserQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoInstancesPresent_NoLightsailInstancesReturned()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            //Act
            var instances = await environment.Mediator.Send(
                new GetProvisionedClustersWithInstancesForUserQuery(
                    Guid.NewGuid()));

            //Assert
            Assert.IsNotNull(instances);
            Assert.AreEqual(0, instances.Count);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_MultipleProvisionedInstancesButNoProvisionedPresent_NoLightsailInstancesReturned()
        {
            //Arrange
            var fakeGetInstanceByNameQueryHandler = Substitute.For<IRequestHandler<GetLightsailInstanceByNameQuery, global::Amazon.Lightsail.Model.Instance>>();
            fakeGetInstanceByNameQueryHandler
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-1"),
                    default)
                .Returns(new global::Amazon.Lightsail.Model.Instance()
                {
                    Name = "some-instance-1"
                });

            fakeGetInstanceByNameQueryHandler
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-2"),
                    default)
                .Returns(new global::Amazon.Lightsail.Model.Instance()
                {
                    Name = "some-instance-2"
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeGetInstanceByNameQueryHandler);
                }
            });

            var userId = Guid.NewGuid();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(new TestUserBuilder()
                    .WithId(userId)
                    .WithClusters(new TestClusterBuilder()
                        .WithInstances(
                            new TestInstanceBuilder()
                                .WithName("some-instance-1")
                                .WithProvisionedStatus(false),
                            new TestInstanceBuilder()
                                .WithName("some-instance-2")
                                .WithProvisionedStatus(false))));
            });

            //Act
            var clusters = await environment.Mediator.Send(
                new GetProvisionedClustersWithInstancesForUserQuery(userId));

            //Assert
            Assert.IsNotNull(clusters);
            Assert.AreEqual(0, clusters.Count);

            await fakeGetInstanceByNameQueryHandler
                .DidNotReceive()
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-1"),
                    default);

            await fakeGetInstanceByNameQueryHandler
                .DidNotReceive()
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-2"),
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_MultipleProvisionedInstancesButOnlyOnceProvisionedPresent_MultipleLightsailInstancesReturned()
        {
            //Arrange
            var fakeGetInstanceByNameQueryHandler = Substitute.For<IRequestHandler<GetLightsailInstanceByNameQuery, global::Amazon.Lightsail.Model.Instance>>();
            fakeGetInstanceByNameQueryHandler
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-1"),
                    default)
                .Returns(new global::Amazon.Lightsail.Model.Instance()
                {
                    Name = "some-instance-1"
                });

            fakeGetInstanceByNameQueryHandler
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-2"),
                    default)
                .Returns(new global::Amazon.Lightsail.Model.Instance()
                {
                    Name = "some-instance-2"
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeGetInstanceByNameQueryHandler);
                }
            });

            var userId = Guid.NewGuid();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(new TestUserBuilder()
                    .WithId(userId)
                    .WithClusters(new TestClusterBuilder()
                        .WithInstances(
                            new TestInstanceBuilder()
                                .WithName("some-instance-1")
                                .WithProvisionedStatus(true),
                            new TestInstanceBuilder()
                                .WithName("some-instance-2")
                                .WithProvisionedStatus(true))));
            });

            //Act
            var clusters = await environment.Mediator.Send(
                new GetProvisionedClustersWithInstancesForUserQuery(userId));

            //Assert
            Assert.IsNotNull(clusters);
            Assert.AreEqual(1, clusters.Count);

            var cluster = clusters.Single();
            var instances = cluster.Instances.ToArray();

            Assert.IsNotNull(instances.First().DatabaseModel);
            Assert.IsNotNull(instances.First().AmazonModel);

            Assert.IsNotNull(instances.Last().DatabaseModel);
            Assert.IsNotNull(instances.Last().AmazonModel);

            await fakeGetInstanceByNameQueryHandler
                .Received(1)
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-1"),
                    default);

            await fakeGetInstanceByNameQueryHandler
                .Received(1)
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-2"),
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_MultipleProvisionedInstancesPresent_MultipleLightsailInstancesReturned()
        {
            //Arrange
            var fakeGetInstanceByNameQueryHandler = Substitute.For<IRequestHandler<GetLightsailInstanceByNameQuery, global::Amazon.Lightsail.Model.Instance>>();
            fakeGetInstanceByNameQueryHandler
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-1"),
                    default)
                .Returns(new global::Amazon.Lightsail.Model.Instance()
                {
                    Name = "some-instance-1"
                });

            fakeGetInstanceByNameQueryHandler
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-2"),
                    default)
                .Returns(new global::Amazon.Lightsail.Model.Instance()
                {
                    Name = "some-instance-2"
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeGetInstanceByNameQueryHandler);
                }
            });

            var userId = Guid.NewGuid();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(new TestUserBuilder()
                    .WithId(userId)
                    .WithClusters(new TestClusterBuilder()
                        .WithInstances(
                            new TestInstanceBuilder()
                                .WithName("some-instance-1")
                                .WithProvisionedStatus(true),
                            new TestInstanceBuilder()
                                .WithName("some-instance-2")
                                .WithProvisionedStatus(true))));
            });

            //Act
            var clusters = await environment.Mediator.Send(
                new GetProvisionedClustersWithInstancesForUserQuery(userId));

            //Assert
            Assert.IsNotNull(clusters);
            Assert.AreEqual(1, clusters.Count);

            var cluster = clusters.Single();
            var instances = cluster.Instances.ToArray();

            Assert.IsNotNull(instances.First().DatabaseModel);
            Assert.IsNotNull(instances.First().AmazonModel);

            Assert.IsNotNull(instances.Last().DatabaseModel);
            Assert.IsNotNull(instances.Last().AmazonModel);

            await fakeGetInstanceByNameQueryHandler
                .Received(1)
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-1"),
                    default);

            await fakeGetInstanceByNameQueryHandler
                .Received(1)
                .Handle(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-2"),
                    default);
        }
    }
}

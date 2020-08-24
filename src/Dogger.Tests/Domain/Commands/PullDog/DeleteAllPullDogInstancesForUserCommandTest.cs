using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.PullDog.DeleteAllPullDogInstancesForUser;
using Dogger.Domain.Models;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class DeleteAllPullDogInstancesForUserCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_InstancesWithoutPullRequestPresent_NothingIsDeleted()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(
                new DoggerEnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var user = new TestUserBuilder().Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);

                await dataContext.Instances.AddAsync(new TestInstanceBuilder()
                    .WithCluster(new TestClusterBuilder()
                        .WithUser(user))
                    .WithPullDogPullRequest(null));
            });

            //Act
            await environment.Mediator.Send(new DeleteAllPullDogInstancesForUserCommand(
                user.Id,
                InitiatorType.System));

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<DeleteInstanceByNameCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_InstancesWithPullRequestsForDifferentUser_NothingIsDeleted()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(
                new DoggerEnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var matchedUser = new TestUserBuilder().Build();

            var otherUser = new TestUserBuilder().Build();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(matchedUser);
                await dataContext.Users.AddAsync(otherUser);

                await dataContext.Instances.AddAsync(new TestInstanceBuilder()
                    .WithCluster(new TestClusterBuilder()
                        .WithUser(otherUser))
                    .WithPullDogPullRequest(new TestPullDogPullRequestBuilder()
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithPullDogSettings(new TestPullDogSettingsBuilder()
                                .WithUser(otherUser)))));
            });

            //Act
            await environment.Mediator.Send(new DeleteAllPullDogInstancesForUserCommand(
                matchedUser.Id,
                InitiatorType.System));

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<DeleteInstanceByNameCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_InstancesPresentWithPullRequestForUser_DeleteCommandsAreFiredForRelevantInstances()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(
                new DoggerEnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var user = new TestUserBuilder().Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);

                await dataContext.Instances.AddAsync(new TestInstanceBuilder()
                    .WithCluster(new TestClusterBuilder()
                        .WithUser(user))
                    .WithName("some-name")
                    .WithPullDogPullRequest(new TestPullDogPullRequestBuilder()
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithPullDogSettings(new TestPullDogSettingsBuilder()
                                .WithUser(user)))));
            });

            //Act
            await environment.Mediator.Send(new DeleteAllPullDogInstancesForUserCommand(
                user.Id,
                InitiatorType.System));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeleteInstanceByNameCommand>(args =>
                    args.Name == "some-name"));
        }
    }
}

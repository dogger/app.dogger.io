using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Models;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class DeleteInstanceByPullRequestCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoMatchingInstanceFound_DoesNothing()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeMediator)
            });

            //Act
            await environment.Mediator.Send(new DeleteInstanceByPullRequestCommand(
                "dummy",
                "dummy",
                InitiatorType.System));

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<DeleteInstanceByNameCommand>(), default);

            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<UpsertPullRequestCommentCommand>(), default);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullRequestWithInstancePresent_DeletesInstanceByName()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeMediator)
            });

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Instances.AddAsync(new TestInstanceBuilder()
                    .WithCluster()
                    .WithName("some-instance-name")
                    .WithPullDogPullRequest(new TestPullDogPullRequestBuilder()
                        .WithHandle("some-pull-request-handle")
                        .WithPullDogRepository(new PullDogRepository()
                        {
                            Handle = "some-repository-handle",
                            PullDogSettings = new PullDogSettings()
                            {
                                PlanId = "dummy",
                                User = new TestUserBuilder().Build(),
                                EncryptedApiKey = Array.Empty<byte>()
                            }
                        })
                        .Build())
                    .Build());
            });

            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(1, await dataContext.Instances.CountAsync());
                Assert.AreEqual(1, await dataContext.PullDogPullRequests.CountAsync());
            });

            //Act
            await environment.Mediator.Send(new DeleteInstanceByPullRequestCommand(
                "some-repository-handle",
                "some-pull-request-handle",
                InitiatorType.System));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(
                    Arg.Is<DeleteInstanceByNameCommand>(args =>
                        args.Name == "some-instance-name"),
                    default);
        }
    }
}

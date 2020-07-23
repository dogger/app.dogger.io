using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Models;
using Dogger.Tests.TestHelpers;
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

            await using var environment = await IntegrationTestEnvironment.CreateAsync(new EnvironmentSetupOptions()
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
        public async Task Handle_PullRequestWithInstancePresent_DeletesInstanceByNameAndPullRequest()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await IntegrationTestEnvironment.CreateAsync(new EnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeMediator)
            });

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Instances.AddAsync(new Instance()
                {
                    Name = "some-instance-name",
                    PlanId = "dummy",
                    Cluster = new Cluster(),
                    PullDogPullRequest = new PullDogPullRequest()
                    {
                        Handle = "some-pull-request-handle",
                        PullDogRepository = new PullDogRepository()
                        {
                            Handle = "some-repository-handle",
                            PullDogSettings = new PullDogSettings()
                            {
                                PlanId = "dummy",
                                User = new User()
                                {
                                    StripeCustomerId = "dummy"
                                },
                                EncryptedApiKey = Array.Empty<byte>()
                            }
                        }
                    }
                });
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

            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(0, await dataContext.PullDogPullRequests.CountAsync());
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullRequestWithoutInstancePresent_DeletesPullRequest()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await IntegrationTestEnvironment.CreateAsync(new EnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeMediator)
            });

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogPullRequests.AddAsync(new PullDogPullRequest()
                {
                    Handle = "some-pull-request-handle",
                    PullDogRepository = new PullDogRepository()
                    {
                        Handle = "some-repository-handle",
                        PullDogSettings = new PullDogSettings()
                        {
                            PlanId = "dummy",
                            User = new User()
                            {
                                StripeCustomerId = "dummy"
                            },
                            EncryptedApiKey = Array.Empty<byte>()
                        }
                    }
                });
            });

            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(1, await dataContext.PullDogPullRequests.CountAsync());
            });

            //Act
            await environment.Mediator.Send(new DeleteInstanceByPullRequestCommand(
                "some-repository-handle",
                "some-pull-request-handle",
                InitiatorType.System));

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(
                    Arg.Any<DeleteInstanceByNameCommand>(),
                    default);

            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(0, await dataContext.PullDogPullRequests.CountAsync());
            });
        }
    }
}

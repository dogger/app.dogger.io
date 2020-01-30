using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.PullDog.DeleteAllPullDogInstancesForUser;
using Dogger.Domain.Models;
using Dogger.Tests.TestHelpers;
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
        public async Task Handle_InstancesWithoutPullRequestPresent_NothingIsDeleted()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var user = new User()
            {
                StripeCustomerId = "dummy"
            };
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);

                await dataContext.Instances.AddAsync(new Instance()
                {
                    Cluster = new Cluster()
                    {
                        User = user
                    },
                    Name = "dummy",
                    PlanId = "dummy",
                    PullDogPullRequest = null
                });
            });

            //Act
            await environment.Mediator.Send(new DeleteAllPullDogInstancesForUserCommand(user.Id));

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<DeleteInstanceByNameCommand>());
        }

        [TestMethod]
        public async Task Handle_InstancesWithPullRequestsForDifferentUser_NothingIsDeleted()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var matchedUser = new User()
            {
                StripeCustomerId = "dummy"
            };

            var otherUser = new User()
            {
                StripeCustomerId = "dummy"
            };

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(matchedUser);
                await dataContext.Users.AddAsync(otherUser);

                await dataContext.Instances.AddAsync(new Instance()
                {
                    Cluster = new Cluster()
                    {
                        User = otherUser
                    },
                    Name = "dummy",
                    PlanId = "dummy",
                    PullDogPullRequest = new PullDogPullRequest()
                    {
                        Handle = "dummy",
                        PullDogRepository = new PullDogRepository()
                        {
                            Handle = "dummy",
                            PullDogSettings = new PullDogSettings()
                            {
                                User = otherUser,
                                PlanId = "dummy",
                                EncryptedApiKey = Array.Empty<byte>()
                            }
                        }
                    }
                });
            });

            //Act
            await environment.Mediator.Send(new DeleteAllPullDogInstancesForUserCommand(matchedUser.Id));

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<DeleteInstanceByNameCommand>());
        }

        [TestMethod]
        public async Task Handle_InstancesPresentWithPullRequestForUser_DeleteCommandsAreFiredForRelevantInstances()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var user = new User()
            {
                StripeCustomerId = "dummy"
            };
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);

                await dataContext.Instances.AddAsync(new Instance()
                {
                    Cluster = new Cluster()
                    {
                        User = user
                    },
                    Name = "some-name",
                    PlanId = "dummy",
                    PullDogPullRequest = new PullDogPullRequest()
                    {
                        Handle = "dummy",
                        PullDogRepository = new PullDogRepository()
                        {
                            Handle = "dummy",
                            PullDogSettings = new PullDogSettings()
                            {
                                User = user,
                                PlanId = "dummy",
                                EncryptedApiKey = Array.Empty<byte>()
                            }
                        }
                    }
                });
            });

            //Act
            await environment.Mediator.Send(new DeleteAllPullDogInstancesForUserCommand(user.Id));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeleteInstanceByNameCommand>(args => 
                    args.Name == "some-name"));
        }
    }
}

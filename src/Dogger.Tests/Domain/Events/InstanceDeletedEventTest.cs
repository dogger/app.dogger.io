using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Events.InstanceDeleted;
using Dogger.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Events
{
    [TestClass]
    public class InstanceDeletedEventTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ValidConditions_UpsertsPullRequestComment()
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

            var createdInstance = await environment
                .DataContext
                .Instances
                .Include(x => x.PullDogPullRequest)
                .ThenInclude(x => x.PullDogRepository)
                .ThenInclude(x => x.PullDogSettings)
                .SingleOrDefaultAsync();

            //Act
            await environment.Mediator.Send(new InstanceDeletedEvent(createdInstance));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(
                    Arg.Is<UpsertPullRequestCommentCommand>(args =>
                        args.PullRequest.Handle == "some-pull-request-handle" &&
                        args.PullRequest.PullDogRepository.Handle == "some-repository-handle"),
                    default);
        }
    }
}

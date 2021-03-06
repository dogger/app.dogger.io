﻿using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.RemoveLabelFromGitHubPullRequest;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Events.InstanceDeleted;
using Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest;
using Dogger.Domain.Services.PullDog;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using Dogger.Tests.TestHelpers.Environments.Dogger;
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
        public async Task Handle_PullRequestPresentOnInstance_UpsertsPullRequestComment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile());

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
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithHandle("some-repository-handle"))));
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

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullRequestPresentWithLabelOnConfiguration_UpsertsPullRequestComment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile()
                {
                    Label = "some-label"
                });

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
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithHandle("some-repository-handle"))));
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
                    Arg.Is<RemoveLabelFromGitHubPullRequestCommand>(args =>
                        args.Label == "some-label"),
                    default);
        }
    }
}

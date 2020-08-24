using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.EnsurePullDogDatabaseInstance;
using Dogger.Domain.Commands.PullDog.EnsurePullDogPullRequest;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest;
using Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using YamlDotNet.Core;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class ProvisionPullDogEnvironmentCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ValidProvisionConditionsGiven_SchedulesJobWithPullDogDatabaseInstance()
        {
            //Arrange
            var databaseInstance = new TestInstanceBuilder()
                .WithName("some-instance-name")
                .Build();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Returns(databaseInstance);

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new TestPullDogPullRequestBuilder().Build());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile(new List<string>()));

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();
            var fakePullDogRepositoryClientFactory = Substitute.For<IPullDogRepositoryClientFactory>();

            var fakePullDogFileCollector = fakePullDogFileCollectorFactory.Create(Arg.Any<IPullDogRepositoryClient>());
            fakePullDogFileCollector
                .GetRepositoryFilesFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(Array.Empty<RepositoryFile>());

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakePullDogFileCollectorFactory,
                fakePullDogRepositoryClientFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)),
                default);

            //Assert
            await fakeProvisioningService
                .Received(1)
                .ScheduleJobAsync(
                    Arg.Is<AggregateProvisioningStateFlow>(args =>
                        args.GetFlowOfType<ProvisionInstanceStateFlow>(0).DatabaseInstance == databaseInstance &&
                        args.GetFlowOfType<DeployToClusterStateFlow>(1).InstanceName == "some-instance-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ValidProvisionConditionsGiven_SchedulesJobWithDockerComposeContents()
        {
            //Arrange
            var databaseInstance = new TestInstanceBuilder()
                .WithName("some-instance-name")
                .Build();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Returns(databaseInstance);

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new TestPullDogPullRequestBuilder().Build());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile(new List<string> { "some-docker-compose-path" }));

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();
            var fakePullDogRepositoryClientFactory = Substitute.For<IPullDogRepositoryClientFactory>();

            var fakePullDogFileCollector = fakePullDogFileCollectorFactory.Create(Arg.Any<IPullDogRepositoryClient>());
            fakePullDogFileCollector
                .GetRepositoryFilesFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(Array.Empty<RepositoryFile>());

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakePullDogFileCollectorFactory,
                fakePullDogRepositoryClientFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)),
                default);

            //Assert
            await fakeProvisioningService
                .Received(1)
                .ScheduleJobAsync(
                    Arg.Is<AggregateProvisioningStateFlow>(args =>
                        args.GetFlowOfType<DeployToClusterStateFlow>(1).DockerComposeYmlFilePaths.Single() == "some-docker-compose-path"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InstallationIdNotFound_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();
            var fakePullDogRepositoryClientFactory = Substitute.For<IPullDogRepositoryClientFactory>();

            var fakePullDogFileCollector = fakePullDogFileCollectorFactory.Create(Arg.Any<IPullDogRepositoryClient>());
            fakePullDogFileCollector
                .GetRepositoryFilesFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(Array.Empty<RepositoryFile>());

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakePullDogFileCollectorFactory,
                fakePullDogRepositoryClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new ProvisionPullDogEnvironmentCommand(
                        "some-pull-request-handle",
                        new TestPullDogRepositoryBuilder()
                            .WithGitHubInstallationId(null)),
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_DockerComposeYmlContentsNotFound_ExplainsWhyInComment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new TestPullDogPullRequestBuilder().Build());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile(new List<string>()));

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();
            var fakePullDogRepositoryClientFactory = Substitute.For<IPullDogRepositoryClientFactory>();

            var fakePullDogFileCollector = fakePullDogFileCollectorFactory.Create(Arg.Any<IPullDogRepositoryClient>());
            fakePullDogFileCollector
                .GetConfigurationFileAsync()
                .Returns(new ConfigurationFile(new List<string>()));

            fakePullDogFileCollector
                .GetRepositoryFilesFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns((RepositoryFile[])null);

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakePullDogFileCollectorFactory,
                fakePullDogRepositoryClientFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)),
                default);

            //Assert
            await fakePullDogFileCollector
                .Received(1)
                .GetRepositoryFilesFromConfiguration(Arg.Any<ConfigurationFile>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_PoolSizeExceededWithNoTestEnvironmentListUrl_UpdatesPullRequestCommentWithMessageExplainingWhy()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Throws(new PullDogPoolSizeExceededException(Array.Empty<PullRequestDetails>()));

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new TestPullDogPullRequestBuilder().Build());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile(new List<string>()));

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();
            var fakePullDogRepositoryClientFactory = Substitute.For<IPullDogRepositoryClientFactory>();

            var fakePullDogFileCollector = fakePullDogFileCollectorFactory.Create(Arg.Any<IPullDogRepositoryClient>());
            fakePullDogFileCollector
                .GetRepositoryFilesFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(Array.Empty<RepositoryFile>());

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakePullDogFileCollectorFactory,
                fakePullDogRepositoryClientFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)),
                default);

            //Assert
            await fakeProvisioningService
                .DidNotReceive()
                .ScheduleJobAsync(
                    Arg.Any<AggregateProvisioningStateFlow>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<UpsertPullRequestCommentCommand>(args =>
                    args.Content.Contains("You can [upgrade your plan]")));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_PoolSizeExceededWithTestEnvironmentListUrl_UpdatesPullRequestCommentWithMessageExplainingWhy()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Throws(new PullDogPoolSizeExceededException(Array.Empty<PullRequestDetails>()));

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new TestPullDogPullRequestBuilder().Build());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile(new List<string>()));

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();
            var fakePullDogRepositoryClientFactory = Substitute.For<IPullDogRepositoryClientFactory>();

            var fakePullDogRepositoryClient = await fakePullDogRepositoryClientFactory.CreateAsync(Arg.Any<PullDogPullRequest>());
            fakePullDogRepositoryClient
                .GetTestEnvironmentListUrl(Arg.Any<ConfigurationFile>())
                .Returns(new Uri("https://some-test-environment-list-url.example.com"));

            var fakePullDogFileCollector = fakePullDogFileCollectorFactory.Create(fakePullDogRepositoryClient);
            fakePullDogFileCollector
                .GetRepositoryFilesFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(Array.Empty<RepositoryFile>());

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakePullDogFileCollectorFactory,
                fakePullDogRepositoryClientFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)),
                default);

            //Assert
            await fakeProvisioningService
                .DidNotReceive()
                .ScheduleJobAsync(
                    Arg.Any<AggregateProvisioningStateFlow>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<UpsertPullRequestCommentCommand>(args =>
                    args.Content.Contains("You can [upgrade your plan]") &&
                    args.Content.Contains("https://some-test-environment-list-url.example.com")));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoConfigurationFileFound_DoesNothing()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new TestPullDogPullRequestBuilder().Build());

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();
            var fakePullDogRepositoryClientFactory = Substitute.For<IPullDogRepositoryClientFactory>();

            var fakePullDogFileCollector = fakePullDogFileCollectorFactory.Create(Arg.Any<IPullDogRepositoryClient>());
            fakePullDogFileCollector
                .GetConfigurationFileAsync()
                .Returns((ConfigurationFile)null);

            fakePullDogFileCollector
                .GetRepositoryFilesFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(Array.Empty<RepositoryFile>());

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakePullDogFileCollectorFactory,
                fakePullDogRepositoryClientFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)),
                default);

            //Assert
            await fakeProvisioningService
                .DidNotReceive()
                .ScheduleJobAsync(
                    Arg.Any<AggregateProvisioningStateFlow>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_DockerComposeSyntaxError_UpdatesPullRequestCommentWithMessageExplainingIt()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Throws(new DockerComposeSyntaxErrorException(new SyntaxErrorException("dummy")));

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new TestPullDogPullRequestBuilder().Build());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile(new List<string>()));

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();
            var fakePullDogRepositoryClientFactory = Substitute.For<IPullDogRepositoryClientFactory>();

            var fakePullDogFileCollector = fakePullDogFileCollectorFactory.Create(Arg.Any<IPullDogRepositoryClient>());
            fakePullDogFileCollector
                .GetRepositoryFilesFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(Array.Empty<RepositoryFile>());

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakePullDogFileCollectorFactory,
                fakePullDogRepositoryClientFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)),
                default);

            //Assert
            await fakeProvisioningService
                .DidNotReceive()
                .ScheduleJobAsync(
                    Arg.Any<AggregateProvisioningStateFlow>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_DemoInstanceAlreadyProvisioned_UpdatesPullRequestCommentWithMessageExplainingWhy()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Throws(new PullDogDemoInstanceAlreadyProvisionedException(new PullRequestDetails("some-direct-pull-request-link")));

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new TestPullDogPullRequestBuilder().Build());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile(new List<string>()));

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();
            var fakePullDogRepositoryClientFactory = Substitute.For<IPullDogRepositoryClientFactory>();

            var fakePullDogFileCollector = fakePullDogFileCollectorFactory.Create(Arg.Any<IPullDogRepositoryClient>());
            fakePullDogFileCollector
                .GetRepositoryFilesFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(Array.Empty<RepositoryFile>());

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakePullDogFileCollectorFactory,
                fakePullDogRepositoryClientFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)),
                default);

            //Assert
            await fakeProvisioningService
                .DidNotReceive()
                .ScheduleJobAsync(
                    Arg.Any<AggregateProvisioningStateFlow>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_LazyModeEngaged_UpdatesPullRequestCommentWithMessageTellingServerIsSleeping()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new TestPullDogPullRequestBuilder().Build());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile(new List<string>())
                {
                    IsLazy = true
                });

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();
            var fakePullDogRepositoryClientFactory = Substitute.For<IPullDogRepositoryClientFactory>();

            var fakePullDogFileCollector = fakePullDogFileCollectorFactory.Create(Arg.Any<IPullDogRepositoryClient>());
            fakePullDogFileCollector
                .GetRepositoryFilesFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(Array.Empty<RepositoryFile>());

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakePullDogFileCollectorFactory,
                fakePullDogRepositoryClientFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)),
                default);

            //Assert
            await fakeProvisioningService
                .DidNotReceive()
                .ScheduleJobAsync(
                    Arg.Any<AggregateProvisioningStateFlow>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());
        }
    }
}

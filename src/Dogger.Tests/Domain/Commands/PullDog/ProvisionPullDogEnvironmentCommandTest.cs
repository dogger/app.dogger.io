using System;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.EnsurePullDogDatabaseInstance;
using Dogger.Domain.Commands.PullDog.EnsurePullDogPullRequest;
using Dogger.Domain.Commands.PullDog.OverrideConfigurationForPullRequest;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest;
using Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Slack.Webhooks;
using YamlDotNet.Core;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class ProvisionPullDogEnvironmentCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ConfigurationOverridePresentInRequest_UpdatesPullRequestConfigurationOverride()
        {
            //Arrange
            var databaseInstance = new Instance()
            {
                Name = "some-instance-name"
            };

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Returns(databaseInstance);

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new PullDogPullRequest());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile()
                {
                    DockerComposeYmlFilePaths = new[] { "dummy" }
                });

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            var fakeSlackClient = Substitute.For<ISlackClient>();
            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogRepositoryClient = await fakePullDogFileCollectorFactory.CreateAsync(Arg.Any<PullDogPullRequest>());
            fakePullDogRepositoryClient
                .GetRepositoryFileContextFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(new RepositoryPullDogFileContext(
                    new[]
                    {
                        "dummy"
                    },
                    Array.Empty<RepositoryFile>()));

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakeSlackClient,
                fakePullDogFileCollectorFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new PullDogRepository()
                    {
                        GitHubInstallationId = 1337,
                        PullDogSettings = new PullDogSettings()
                    })
                {
                    ConfigurationOverride = new ConfigurationFileOverride()
                },
                default);

            //Assert
            await fakeProvisioningService
                .Received(1)
                .ScheduleJobAsync(
                    Arg.Any<AggregateProvisioningStateFlow>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Any<OverrideConfigurationForPullRequestCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ValidProvisionConditionsGiven_SchedulesJobWithPullDogDatabaseInstance()
        {
            //Arrange
            var databaseInstance = new Instance()
            {
                Name = "some-instance-name"
            };

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Returns(databaseInstance);

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new PullDogPullRequest());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile()
                {
                    DockerComposeYmlFilePaths = new[] { "dummy" }
                });

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            var fakeSlackClient = Substitute.For<ISlackClient>();
            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogRepositoryClient = await fakePullDogFileCollectorFactory.CreateAsync(Arg.Any<PullDogPullRequest>());
            fakePullDogRepositoryClient
                .GetRepositoryFileContextFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(new RepositoryPullDogFileContext(
                    new[]
                    {
                        "dummy"
                    },
                    Array.Empty<RepositoryFile>()));

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator, 
                fakeProvisioningService, 
                fakeSlackClient, 
                fakePullDogFileCollectorFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new PullDogRepository()
                    {
                        GitHubInstallationId = 1337,
                        PullDogSettings = new PullDogSettings()
                    }),
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
            var databaseInstance = new Instance()
            {
                Name = "some-instance-name"
            };

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Returns(databaseInstance);

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new PullDogPullRequest());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile()
                {
                    DockerComposeYmlFilePaths = new[] { "dummy" }
                });

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            var fakeSlackClient = Substitute.For<ISlackClient>();
            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogRepositoryClient = await fakePullDogFileCollectorFactory.CreateAsync(Arg.Any<PullDogPullRequest>());
            fakePullDogRepositoryClient
                .GetRepositoryFileContextFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(new RepositoryPullDogFileContext(
                    new[]
                    {
                        "some-docker-compose-contents"
                    },
                    Array.Empty<RepositoryFile>()));

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakeSlackClient,
                fakePullDogFileCollectorFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new PullDogRepository()
                    {
                        GitHubInstallationId = 1337,
                        PullDogSettings = new PullDogSettings()
                    }),
                default);

            //Assert
            await fakeProvisioningService
                .Received(1)
                .ScheduleJobAsync(
                    Arg.Is<AggregateProvisioningStateFlow>(args =>
                        args.GetFlowOfType<DeployToClusterStateFlow>(1).DockerComposeYmlContents.Single() == "some-docker-compose-contents"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InstallationIdNotFound_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            var fakeSlackClient = Substitute.For<ISlackClient>();
            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogRepositoryClient = await fakePullDogFileCollectorFactory.CreateAsync(Arg.Any<PullDogPullRequest>());
            fakePullDogRepositoryClient
                .GetRepositoryFileContextFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(new RepositoryPullDogFileContext(
                    new[]
                    {
                        "some-docker-compose-contents"
                    },
                    Array.Empty<RepositoryFile>()));

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakeSlackClient,
                fakePullDogFileCollectorFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => 
                await handler.Handle(
                    new ProvisionPullDogEnvironmentCommand(
                        "some-pull-request-handle",
                        new PullDogRepository()
                        {
                            GitHubInstallationId = null,
                            PullDogSettings = new PullDogSettings()
                        }),
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
                .Returns(new PullDogPullRequest());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile()
                {
                    DockerComposeYmlFilePaths = new[] { "dummy" }
                });

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            var fakeSlackClient = Substitute.For<ISlackClient>();
            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogRepositoryClient = await fakePullDogFileCollectorFactory.CreateAsync(Arg.Any<PullDogPullRequest>());
            fakePullDogRepositoryClient
                .GetConfigurationFileAsync()
                .Returns(new ConfigurationFile()
                {
                    DockerComposeYmlFilePaths = new[] { "dummy" }
                });

            fakePullDogRepositoryClient
                .GetRepositoryFileContextFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns((RepositoryPullDogFileContext)null);

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakeSlackClient,
                fakePullDogFileCollectorFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new PullDogRepository()
                    {
                        GitHubInstallationId = 1337,
                        PullDogSettings = new PullDogSettings()
                    }),
                default);

            //Assert
            await fakePullDogRepositoryClient
                .Received(1)
                .GetRepositoryFileContextFromConfiguration(Arg.Any<ConfigurationFile>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_PoolSizeExceeded_UpdatesPullRequestCommentWithMessageExplainingWhy()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Throws(new PullDogPoolSizeExceededException(Array.Empty<PullRequestDetails>()));

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new PullDogPullRequest());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile()
                {
                    DockerComposeYmlFilePaths = new[] { "dummy" }
                });

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            var fakeSlackClient = Substitute.For<ISlackClient>();
            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogRepositoryClient = await fakePullDogFileCollectorFactory.CreateAsync(Arg.Any<PullDogPullRequest>());
            fakePullDogRepositoryClient
                .GetRepositoryFileContextFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(new RepositoryPullDogFileContext(
                    new[]
                    {
                        "dummy"
                    },
                    Array.Empty<RepositoryFile>()));

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakeSlackClient,
                fakePullDogFileCollectorFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new PullDogRepository()
                    {
                        GitHubInstallationId = 1337,
                        PullDogSettings = new PullDogSettings()
                    }),
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
        public async Task Handle_NoConfigurationFileFound_DoesNothing()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Throws(new PullDogDemoInstanceAlreadyProvisionedException(Array.Empty<PullRequestDetails>()));

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new PullDogPullRequest());

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            var fakeSlackClient = Substitute.For<ISlackClient>();
            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogRepositoryClient = await fakePullDogFileCollectorFactory.CreateAsync(Arg.Any<PullDogPullRequest>());
            fakePullDogRepositoryClient
                .GetConfigurationFileAsync()
                .Returns((ConfigurationFile)null);

            fakePullDogRepositoryClient
                .GetRepositoryFileContextFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(new RepositoryPullDogFileContext(
                    new[]
                    {
                        "dummy"
                    },
                    Array.Empty<RepositoryFile>()));

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakeSlackClient,
                fakePullDogFileCollectorFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new PullDogRepository()
                    {
                        GitHubInstallationId = 1337,
                        PullDogSettings = new PullDogSettings()
                    }),
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
                .Returns(new PullDogPullRequest());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile());

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            var fakeSlackClient = Substitute.For<ISlackClient>();
            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogRepositoryClient = await fakePullDogFileCollectorFactory.CreateAsync(Arg.Any<PullDogPullRequest>());
            fakePullDogRepositoryClient
                .GetRepositoryFileContextFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(new RepositoryPullDogFileContext(
                    new[]
                    {
                        "dummy"
                    },
                    Array.Empty<RepositoryFile>()));

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakeSlackClient,
                fakePullDogFileCollectorFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new PullDogRepository()
                    {
                        GitHubInstallationId = 1337,
                        PullDogSettings = new PullDogSettings()
                    }),
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
                .Throws(new PullDogDemoInstanceAlreadyProvisionedException(new []
                {
                    new PullRequestDetails(
                        "some-repository-name",
                        "some-pull-request-link")
                }));

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new PullDogPullRequest());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile());

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            var fakeSlackClient = Substitute.For<ISlackClient>();
            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogRepositoryClient = await fakePullDogFileCollectorFactory.CreateAsync(Arg.Any<PullDogPullRequest>());
            fakePullDogRepositoryClient
                .GetRepositoryFileContextFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(new RepositoryPullDogFileContext(
                    new[]
                    {
                        "dummy"
                    },
                    Array.Empty<RepositoryFile>()));

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakeSlackClient,
                fakePullDogFileCollectorFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new PullDogRepository()
                    {
                        GitHubInstallationId = 1337,
                        PullDogSettings = new PullDogSettings()
                    }),
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
                .Send(Arg.Any<EnsurePullDogDatabaseInstanceCommand>())
                .Throws(new PullDogDemoInstanceAlreadyProvisionedException(Array.Empty<PullRequestDetails>()));

            fakeMediator
                .Send(Arg.Any<EnsurePullDogPullRequestCommand>())
                .Returns(new PullDogPullRequest());

            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile()
                {
                    DockerComposeYmlFilePaths = new[] { "dummy" },
                    IsLazy = true
                });

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            var fakeSlackClient = Substitute.For<ISlackClient>();
            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogRepositoryClient = await fakePullDogFileCollectorFactory.CreateAsync(Arg.Any<PullDogPullRequest>());
            fakePullDogRepositoryClient
                .GetRepositoryFileContextFromConfiguration(Arg.Any<ConfigurationFile>())
                .Returns(new RepositoryPullDogFileContext(
                    new[]
                    {
                        "dummy"
                    },
                    Array.Empty<RepositoryFile>()));

            var handler = new ProvisionPullDogEnvironmentCommandHandler(
                fakeMediator,
                fakeProvisioningService,
                fakeSlackClient,
                fakePullDogFileCollectorFactory);

            //Act
            await handler.Handle(
                new ProvisionPullDogEnvironmentCommand(
                    "some-pull-request-handle",
                    new PullDogRepository()
                    {
                        GitHubInstallationId = 1337,
                        PullDogSettings = new PullDogSettings()
                    }),
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

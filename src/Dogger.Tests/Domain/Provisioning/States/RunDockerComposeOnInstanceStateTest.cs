using Dogger.Infrastructure.Ssh;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Amazon.Lightsail.OpenFirewallPorts;
using Dogger.Domain.Services.Provisioning.Arguments;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Domain.Services.Provisioning.States.CompleteInstanceSetup;
using Dogger.Domain.Services.Provisioning.States.RunDockerComposeOnInstance;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Tests.TestHelpers;
using MediatR;
using NSubstitute.ExceptionExtensions;

namespace Dogger.Tests.Domain.Provisioning.States
{
    [TestClass]
    public class RunDockerComposeOnInstanceStateTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task UpdateAsync_NoIpAddressSet_ThrowsException()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();

            var state = serviceProvider.GetRequiredService<RunDockerComposeOnInstanceState>();
            state.IpAddress = null;

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await state.UpdateAsync());

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task UpdateAsync_IpAddressProvided_CreatesProperSshClient()
        {
            //Arrange
            var fakeDockerComposeParser = Substitute.For<IDockerComposeParser>();

            var fakeDockerComposeParserFactory = Substitute.For<IDockerComposeParserFactory>();
            fakeDockerComposeParserFactory
                .Create(Arg.Any<string>())
                .Returns(fakeDockerComposeParser);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();

            var fakeSshClient = Substitute.For<ISshClient>();
            fakeSshClientFactory
                .CreateForLightsailInstanceAsync("ip-address")
                .Returns(fakeSshClient);

            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStateFactory>();
            fakeProvisioningStateFactory
                .Create<CompleteInstanceSetupState>()
                .Returns(new CompleteInstanceSetupState(
                    fakeSshClientFactory,
                    Substitute.For<IMediator>()));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeDockerComposeParserFactory);
                services.AddSingleton(fakeProvisioningStateFactory);
                services.AddSingleton(fakeSshClientFactory);
                services.AddSingleton(fakeMediator);
            });

            var state = serviceProvider.GetRequiredService<RunDockerComposeOnInstanceState>();
            state.InstanceName = "some-instance-name";
            state.IpAddress = "ip-address";
            state.DockerComposeYmlFilePaths = new[]
            {
                "some-docker-compose-yml-contents"
            };

            await state.InitializeAsync();

            //Act
            await state.UpdateAsync();

            //Assert
            await fakeSshClient
                .Received()
                .ExecuteCommandAsync(
                    Arg.Any<SshRetryPolicy>(),
                    Arg.Any<SshResponseSensitivity>(),
                    Arg.Any<string>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task UpdateAsync_DockerComposeUpThrowsException_JobValidationProblemResultSet()
        {
            //Arrange
            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();

            var fakeDockerComposeParser = Substitute.For<IDockerComposeParser>();
            fakeDockerComposeParser
                .GetExposedHostPorts()
                .Returns(new[]
                {
                    new ExposedPort()
                    {
                        Protocol = SocketProtocol.Tcp,
                        Port = 1337
                    }
                });

            var fakeDockerComposeParserFactory = Substitute.For<IDockerComposeParserFactory>();
            fakeDockerComposeParserFactory
                .Create(Arg.Any<string>())
                .Returns(fakeDockerComposeParser);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeSshClient = Substitute.For<ISshClient>();
            fakeSshClient
                .ExecuteCommandAsync(
                    SshRetryPolicy.ProhibitRetries,
                    SshResponseSensitivity.ContainsNoSensitiveData,
                    Arg.Is<string>(arg => arg.Contains("docker-compose -f some-docker-compose-yml-path --compatibility up")),
                    Arg.Any<Dictionary<string, string>>())
                .Throws(new SshCommandExecutionException("dummy", new SshCommandResult()
                {
                    Text = "some-error-text",
                    ExitCode = 1
                }));

            fakeSshClientFactory
                .CreateForLightsailInstanceAsync("ip-address")
                .Returns(fakeSshClient);

            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStateFactory>();
            fakeProvisioningStateFactory
                .Create<CompleteInstanceSetupState>()
                .Returns(new CompleteInstanceSetupState(
                    fakeSshClientFactory,
                    Substitute.For<IMediator>()));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeDockerComposeParserFactory);
                services.AddSingleton(fakeProvisioningStateFactory);
                services.AddSingleton(fakeSshClientFactory);
                services.AddSingleton(fakeMediator);
            });

            var state = serviceProvider.GetRequiredService<RunDockerComposeOnInstanceState>();
            state.IpAddress = "ip-address";
            state.InstanceName = "some-instance-name";
            state.DockerComposeYmlFilePaths = new[]
            {
                "some-docker-compose-yml-path"
            };

            await state.InitializeAsync();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<StateUpdateException>(async () => 
                await state.UpdateAsync());

            //Assert
            Assert.IsNotNull(exception);
            Assert.IsNotNull(exception.StatusResult);

            var validationProblem = exception.StatusResult.GetValidationProblemDetails();
            Assert.AreEqual("DOCKER_COMPOSE_UP_FAIL", validationProblem.Type);
            Assert.AreEqual("some-error-text", validationProblem.Title);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task UpdateAsync_IpAddressAndDockerComposeYmlWithPortsProvided_OpensNecessaryPortsAndDockerComposePorts()
        {
            //Arrange
            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();

            var fakeDockerComposeParser = Substitute.For<IDockerComposeParser>();
            fakeDockerComposeParser
                .GetExposedHostPorts()
                .Returns(new[]
                {
                    new ExposedPort()
                    {
                        Protocol = SocketProtocol.Tcp,
                        Port = 1337
                    }
                });

            var fakeDockerComposeParserFactory = Substitute.For<IDockerComposeParserFactory>();
            fakeDockerComposeParserFactory
                .Create(Arg.Any<string>())
                .Returns(fakeDockerComposeParser);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeSshClient = Substitute.For<ISshClient>();
            fakeSshClientFactory
                .CreateForLightsailInstanceAsync("ip-address")
                .Returns(fakeSshClient);

            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStateFactory>();
            fakeProvisioningStateFactory
                .Create<CompleteInstanceSetupState>()
                .Returns(new CompleteInstanceSetupState(
                    fakeSshClientFactory,
                    Substitute.For<IMediator>()));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeDockerComposeParserFactory);
                services.AddSingleton(fakeProvisioningStateFactory);
                services.AddSingleton(fakeSshClientFactory);
                services.AddSingleton(fakeMediator);
            });

            var state = serviceProvider.GetRequiredService<RunDockerComposeOnInstanceState>();
            state.IpAddress = "ip-address";
            state.InstanceName = "some-instance-name";
            state.Authentication = new [] {
                new DockerAuthenticationArguments(
                    "some-username",
                    "some-password")
                {
                    RegistryHostName = "some-registry"
                }
            };
            state.DockerComposeYmlFilePaths = new[]
            {
                "some-docker-compose-yml-contents"
            };

            await state.InitializeAsync();

            //Act
            await state.UpdateAsync();

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<OpenFirewallPortsCommand>(arg =>
                    arg.Ports.Any(p => p.ToPort == 1337 && p.FromPort == 1337 && p.Protocol == SocketProtocol.Tcp)));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task UpdateAsync_IpAddressProvided_ReturnsSucceeded()
        {
            //Arrange
            var fakeDockerComposeParser = Substitute.For<IDockerComposeParser>();

            var fakeDockerComposeParserFactory = Substitute.For<IDockerComposeParserFactory>();
            fakeDockerComposeParserFactory
                .Create(Arg.Any<string>())
                .Returns(fakeDockerComposeParser);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();

            var fakeSshClient = Substitute.For<ISshClient>();
            fakeSshClientFactory
                .CreateForLightsailInstanceAsync("ip-address")
                .Returns(fakeSshClient);

            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStateFactory>();
            fakeProvisioningStateFactory
                .Create<CompleteInstanceSetupState>()
                .Returns(new CompleteInstanceSetupState(
                    fakeSshClientFactory,
                    Substitute.For<IMediator>()));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeDockerComposeParserFactory);
                services.AddSingleton(fakeProvisioningStateFactory);
                services.AddSingleton(fakeSshClientFactory);
                services.AddSingleton(fakeMediator);
            });

            var state = serviceProvider.GetRequiredService<RunDockerComposeOnInstanceState>();
            state.IpAddress = "ip-address";
            state.InstanceName = "some-instance-name";
            state.DockerComposeYmlFilePaths = new[]
            {
                "some-docker-compose-yml-contents"
            };

            await state.InitializeAsync();

            //Act
            var newState = await state.UpdateAsync();

            //Assert
            Assert.AreEqual(ProvisioningStateUpdateResult.Succeeded, newState);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task UpdateAsync_IpAddressAndEnvironmentFilesProvided_SetsUpEnvironmentFilesOnInstance()
        {
            //Arrange
            var fakeDockerComposeParser = Substitute.For<IDockerComposeParser>();

            var fakeDockerComposeParserFactory = Substitute.For<IDockerComposeParserFactory>();
            fakeDockerComposeParserFactory
                .Create(Arg.Any<string>())
                .Returns(fakeDockerComposeParser);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();

            var fakeSshClient = Substitute.For<ISshClient>();
            fakeSshClientFactory
                .CreateForLightsailInstanceAsync("ip-address")
                .Returns(fakeSshClient);

            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStateFactory>();
            fakeProvisioningStateFactory
                .Create<CompleteInstanceSetupState>()
                .Returns(new CompleteInstanceSetupState(
                    fakeSshClientFactory,
                    Substitute.For<IMediator>()));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeDockerComposeParserFactory);
                services.AddSingleton(fakeProvisioningStateFactory);
                services.AddSingleton(fakeSshClientFactory);
                services.AddSingleton(fakeMediator);
            });

            var state = serviceProvider.GetRequiredService<RunDockerComposeOnInstanceState>();
            state.IpAddress = "ip-address";
            state.InstanceName = "some-instance-name";
            state.Files = new[]
            {
                new InstanceDockerFile(
                    "some-file-name", 
                    Encoding.UTF8.GetBytes("some-file-contents"))
            };
            state.DockerComposeYmlFilePaths = new[]
            {
                "some-docker-compose-yml-contents"
            };

            await state.InitializeAsync();

            //Act
            await state.UpdateAsync();

            //Assert
            await fakeSshClient
                .Received()
                .ExecuteCommandAsync(
                    Arg.Any<SshRetryPolicy>(),
                    Arg.Any<SshResponseSensitivity>(),
                    Arg.Is<string>(arg => arg.Contains("some-file-name")),
                    Arg.Any<Dictionary<string, string>>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task UpdateAsync_IpAddressAndAuthenticationParametersProvided_SignsInToDockerOnInstance()
        {
            //Arrange
            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();

            var fakeDockerComposeParser = Substitute.For<IDockerComposeParser>();

            var fakeDockerComposeParserFactory = Substitute.For<IDockerComposeParserFactory>();
            fakeDockerComposeParserFactory
                .Create(Arg.Any<string>())
                .Returns(fakeDockerComposeParser);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeSshClient = Substitute.For<ISshClient>();
            fakeSshClientFactory
                .CreateForLightsailInstanceAsync("ip-address")
                .Returns(fakeSshClient);

            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStateFactory>();
            fakeProvisioningStateFactory
                .Create<CompleteInstanceSetupState>()
                .Returns(new CompleteInstanceSetupState(
                    fakeSshClientFactory,
                    Substitute.For<IMediator>()));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeDockerComposeParserFactory);
                services.AddSingleton(fakeProvisioningStateFactory);
                services.AddSingleton(fakeSshClientFactory);
                services.AddSingleton(fakeMediator);
            });

            var state = serviceProvider.GetRequiredService<RunDockerComposeOnInstanceState>();
            state.IpAddress = "ip-address";
            state.InstanceName = "some-instance-name";
            state.Authentication = new [] {
                new DockerAuthenticationArguments(
                    "some-username",
                    "some-password")
                {
                    RegistryHostName = "some-registry"
                }
            };
            state.DockerComposeYmlFilePaths = new[]
            {
                "some-docker-compose-yml-contents"
            };

            await state.InitializeAsync();

            //Act
            await state.UpdateAsync();

            //Assert
            await fakeSshClient
                .Received()
                .ExecuteCommandAsync(
                    Arg.Any<SshRetryPolicy>(),
                    Arg.Any<SshResponseSensitivity>(),
                    Arg.Is<string>(arg =>
                        arg.Contains("@username") &&
                        arg.Contains("@password")),
                    Arg.Any<Dictionary<string, string>>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task UpdateAsync_IpAddressAndNoAuthenticationParametersProvided_DoesNotSignInToDockerOnInstance()
        {
            //Arrange
            var fakeDockerComposeParser = Substitute.For<IDockerComposeParser>();

            var fakeDockerComposeParserFactory = Substitute.For<IDockerComposeParserFactory>();
            fakeDockerComposeParserFactory
                .Create(Arg.Any<string>())
                .Returns(fakeDockerComposeParser);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();

            var fakeSshClient = Substitute.For<ISshClient>();
            fakeSshClientFactory
                .CreateForLightsailInstanceAsync("ip-address")
                .Returns(fakeSshClient);

            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStateFactory>();
            fakeProvisioningStateFactory
                .Create<CompleteInstanceSetupState>()
                .Returns(new CompleteInstanceSetupState(
                    fakeSshClientFactory,
                    Substitute.For<IMediator>()));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeDockerComposeParserFactory);
                services.AddSingleton(fakeProvisioningStateFactory);
                services.AddSingleton(fakeSshClientFactory);
                services.AddSingleton(fakeMediator);
            });

            var state = serviceProvider.GetRequiredService<RunDockerComposeOnInstanceState>();
            state.IpAddress = "ip-address";
            state.InstanceName = "some-instance-name";
            state.DockerComposeYmlFilePaths = new[]
            {
                "some-docker-compose-yml-contents"
            };
            state.Authentication = null;

            await state.InitializeAsync();

            //Act
            await state.UpdateAsync();

            //Assert
            await fakeSshClient
                .DidNotReceive()
                .ExecuteCommandAsync(
                    Arg.Any<SshRetryPolicy>(),
                    Arg.Any<SshResponseSensitivity>(),
                    Arg.Is<string>(arg =>
                        arg.Contains("docker login")));
        }
    }
}

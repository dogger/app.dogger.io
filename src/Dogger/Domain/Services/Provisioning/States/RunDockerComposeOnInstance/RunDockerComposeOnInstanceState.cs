using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Destructurama.Attributed;
using Dogger.Domain.Commands.Amazon.Lightsail.OpenFirewallPorts;
using Dogger.Domain.Events.ServerDeploymentCompleted;
using Dogger.Domain.Events.ServerDeploymentFailed;
using Dogger.Domain.Services.Provisioning.Arguments;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Infrastructure.Ssh;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Domain.Services.Provisioning.States.RunDockerComposeOnInstance
{
    public class RunDockerComposeOnInstanceState : SshInstanceState, IRunDockerComposeOnInstanceState
    {
        private readonly IMediator mediator;
        private readonly IDockerComposeParserFactory dockerComposeParserFactory;

        private string description;

        public override string Description => description;

        public override string? IpAddress { get; set; }
        public string? InstanceName { get; set; }

        public IDictionary<string, string>? BuildArguments { get; set; }
        public string[]? DockerComposeYmlFilePaths { get; set; }
        public IEnumerable<InstanceDockerFile>? Files { get; set; }

        [NotLogged]
        public IEnumerable<IDockerAuthenticationArguments>? Authentication { get; set; }

        public RunDockerComposeOnInstanceState(
            ISshClientFactory sshClientFactory,
            IMediator mediator,
            IDockerComposeParserFactory dockerComposeParserFactory) : base(sshClientFactory)
        {
            this.mediator = mediator;
            this.dockerComposeParserFactory = dockerComposeParserFactory;

            description = "Installing your services using Docker Compose";
        }

        protected override async Task<ProvisioningStateUpdateResult> OnUpdateAsync(ISshClient sshClient)
        {
            if (this.DockerComposeYmlFilePaths == null)
                throw new InvalidOperationException("Docker Compose file paths must be set.");

            await ClearExistingDoggerFilesAsync(sshClient);
            await CreateFilesOnServerAsync(sshClient);

            await OpenExposedFirewallPortsOnInstanceAsync(sshClient);
            await RunContainersAsync(sshClient);

            await SendServerDeploymentCompletedEventAsync();

            return ProvisioningStateUpdateResult.Succeeded;
        }

        private async Task SendServerDeploymentCompletedEventAsync()
        {
            if (this.InstanceName == null)
                throw new InvalidOperationException("No instance name was found.");

            await this.mediator.Send(new ServerDeploymentCompletedEvent(this.InstanceName));
        }

        private static async Task ClearExistingDoggerFilesAsync(ISshClient sshClient)
        {
            await RemoveDirectoryAsync(sshClient, "dogger");
            await EnsureDirectoryAsync(sshClient, "dogger");
        }

        private static async Task RemoveDirectoryAsync(ISshClient sshClient, string path)
        {
            await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                $"sudo rm ./{path} -rf");
        }

        private static async Task EnsureDirectoryAsync(ISshClient sshClient, string path)
        {
            await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                $"mkdir -m 777 -p ./{path}");

            await SetUserPermissionsOnPathAsync(sshClient, path);
        }

        private async Task CreateFilesOnServerAsync(ISshClient sshClient)
        {
            var files = this.Files;
            if (files == null)
                return;

            foreach (var file in files)
            {
                await CreateSensitiveFileOnServerAsync(
                    sshClient,
                    file.Path,
                    file.Contents);
            }
        }

        private async Task RunContainersAsync(ISshClient sshClient)
        {
            if (InstanceName == null)
                throw new InvalidOperationException("Could not find instance name.");

            await AuthenticateDockerAsync(sshClient);

            var filesArgument = GetDockerComposeFilesCommandLineArgumentString();
            var buildArgumentsArgument = PrependArgumentNameToStrings("--build-arg", GetBuildArgumentAssignments());

            try
            {
                await sshClient.ExecuteCommandAsync(
                    SshRetryPolicy.AllowRetries,
                    $"cd dogger && @environmentVariablesPrefix docker-compose {filesArgument} down --rmi all --volumes --remove-orphans",
                    GetEnvironmentVariablesCommandLinePrefixArguments());

                await sshClient.ExecuteCommandAsync(
                    SshRetryPolicy.AllowRetries,
                    $"cd dogger && @environmentVariablesPrefix docker-compose {filesArgument} pull --include-deps",
                    GetEnvironmentVariablesCommandLinePrefixArguments());

                await sshClient.ExecuteCommandAsync(
                    SshRetryPolicy.AllowRetries,
                    $"cd dogger && @environmentVariablesPrefix docker-compose {filesArgument} build --force-rm --parallel --no-cache {buildArgumentsArgument}",
                    GetEnvironmentVariablesCommandLinePrefixArguments());

                await sshClient.ExecuteCommandAsync(
                    SshRetryPolicy.ProhibitRetries,
                    $"cd dogger && @environmentVariablesPrefix docker-compose {filesArgument} --compatibility up --detach --remove-orphans --always-recreate-deps --force-recreate --renew-anon-volumes",
                    GetEnvironmentVariablesCommandLinePrefixArguments());
            }
            catch (SshCommandExecutionException ex) when (ex.Result.ExitCode == 1)
            {
                var listFilesDump = await sshClient.ExecuteCommandAsync(
                    SshRetryPolicy.AllowRetries,
                    "cd dogger && ls -R");

                await this.mediator.Send(new ServerDeploymentFailedEvent(
                    InstanceName,
                    ex.Result.Text,
                    listFilesDump));

                throw new StateUpdateException(
                    "Could not run containers: " + ex.Result.Text,
                    ex,
                    new BadRequestObjectResult(
                        new ValidationProblemDetails()
                        {
                            Type = "DOCKER_COMPOSE_UP_FAIL",
                            Title = ex.Result.Text
                        }));
            }
        }

        private string GetDockerComposeFilesCommandLineArgumentString()
        {
            if (this.DockerComposeYmlFilePaths == null)
                throw new InvalidOperationException("No Docker Compose file paths were found.");

            return PrependArgumentNameToStrings("-f", this
                .DockerComposeYmlFilePaths
                .Select(SanitizeRelativePath));
        }

        private Dictionary<string, string?> GetEnvironmentVariablesCommandLinePrefixArguments()
        {
            return new Dictionary<string, string?>()
            {
                {
                    "environmentVariablesPrefix", string.Join(' ', GetBuildArgumentAssignments())
                }
            };
        }

        private string[] GetBuildArgumentAssignments()
        {
            return this.BuildArguments == null
                ? Array.Empty<string>()
                : this.BuildArguments
                    .Select(x => $"{x.Key}={x.Value}")
                    .ToArray();
        }

        private async Task AuthenticateDockerAsync(ISshClient sshClient)
        {
            var authenticationArguments = this.Authentication;
            if (authenticationArguments == null)
                return;

            foreach (var authenticationArgument in authenticationArguments)
            {
                await sshClient.ExecuteCommandAsync(
                    SshRetryPolicy.AllowRetries,
                    "echo @password | docker login -u @username --password-stdin @registryHostName",
                    new Dictionary<string, string?>()
                    {
                        {
                            "username", authenticationArgument.Username
                        },
                        {
                            "password", authenticationArgument.Password
                        },
                        {
                            "registryHostName", authenticationArgument.RegistryHostName
                        }
                    });
            }
        }

        private async Task OpenExposedFirewallPortsOnInstanceAsync(ISshClient sshClient)
        {
            if (this.InstanceName == null)
                throw new InvalidOperationException("No instance name was found.");

            this.description = "Opening firewall for exposed ports and protocols";

            var mergedDockerComposeYmlContents = await GetMergedDockerComposeYmlFileContentsAsync(sshClient);
            var dockerComposeParser = this.dockerComposeParserFactory.Create(mergedDockerComposeYmlContents);

            var portsToOpen = new HashSet<ExposedPortRange>()
            {
                GetSshPort()
            };

            var ports = dockerComposeParser.GetExposedHostPorts();
            foreach (var port in ports)
                portsToOpen.Add(port);

            await this.mediator.Send(new OpenFirewallPortsCommand(
                this.InstanceName,
                portsToOpen));
        }

        private async Task<string> GetMergedDockerComposeYmlFileContentsAsync(ISshClient sshClient)
        {
            var dockerComposeYmlFilePathArguments = GetDockerComposeFilesCommandLineArgumentString();

            return await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                $"cd dogger && @environmentVariablesPrefix docker-compose {dockerComposeYmlFilePathArguments} config",
                GetEnvironmentVariablesCommandLinePrefixArguments());
        }

        private static string PrependArgumentNameToStrings(string argumentName, IEnumerable<string> arguments)
        {
            return string.Join(' ', arguments
                .Select(filePath => $"{argumentName} {filePath}"));
        }

        /// <summary>
        /// If we don't include the SSH port, we can't control the instance.
        /// </summary>
        private static ExposedPort GetSshPort()
        {
            return new ExposedPort()
            {
                Port = 22,
                Protocol = SocketProtocol.Tcp
            };
        }

        private static async Task CreateSensitiveFileOnServerAsync(
            ISshClient sshClient,
            string filePath,
            byte[] contents)
        {
            filePath = SanitizeRelativePath(filePath);

            if (filePath.Contains("/", StringComparison.InvariantCulture))
            {
                var folderPath = filePath.Substring(0, filePath.LastIndexOf('/'));
                await EnsureDirectoryAsync(sshClient, $"./dogger/{folderPath}");
            }

            await sshClient.TransferFileAsync(
                SshRetryPolicy.AllowRetries,
                $"dogger/{filePath}",
                contents);

            await SetUserPermissionsOnPathAsync(sshClient, $"dogger/{filePath}");
        }

        private static string SanitizeRelativePath(string filePath)
        {
            while (filePath.Contains("//", StringComparison.InvariantCulture))
                filePath = filePath.Replace("//", "/", StringComparison.InvariantCulture);

            if (filePath.StartsWith("/", StringComparison.InvariantCulture))
                filePath = filePath.Substring(1);
            return filePath;
        }

        private static async Task SetUserPermissionsOnPathAsync(ISshClient sshClient, string fileName)
        {
            await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                $"sudo chmod 777 ./{fileName}");
        }
    }
}

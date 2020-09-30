using System;
using System.Collections.Generic;
using Dogger.Domain.Controllers.Clusters;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Arguments;
using MediatR;

namespace Dogger.Domain.Commands.Clusters.DeployToCluster
{
    public class DeployToClusterCommand : IRequest<IProvisioningJob>
    {
        public DeployToClusterCommand(
            string[] dockerComposeYmlFilePaths)
        {
            this.DockerComposeYmlFilePaths = dockerComposeYmlFilePaths;
        }

        public Guid? UserId { get; set; }

        public Guid? ClusterId { get; set; }

        public string[] DockerComposeYmlFilePaths { get; }

        public IEnumerable<IDockerAuthenticationArguments>? Authentication { get; set; }

        public FileRequest[]? Files { get; set; }
    }
}

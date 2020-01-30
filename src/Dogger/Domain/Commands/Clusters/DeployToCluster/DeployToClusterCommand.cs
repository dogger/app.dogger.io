using System;
using System.Collections.Generic;
using Dogger.Controllers.Clusters;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Arguments;
using MediatR;

namespace Dogger.Domain.Commands.Clusters.DeployToCluster
{
    public class DeployToClusterCommand : IRequest<IProvisioningJob>
    {
        public DeployToClusterCommand(
            string[] dockerComposeYmlContents)
        {
            this.DockerComposeYmlContents = dockerComposeYmlContents;
        }

        public Guid? UserId { get; set; }

        public Guid? ClusterId { get; set; }

        public string[] DockerComposeYmlContents { get; }

        public IEnumerable<IDockerAuthenticationArguments>? Authentication { get; set; }

        public FileRequest[]? Files { get; set; }
    }
}

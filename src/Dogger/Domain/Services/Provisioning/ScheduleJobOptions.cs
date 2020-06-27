using System;
using Destructurama.Attributed;
using Dogger.Domain.Services.Provisioning.Arguments;

namespace Dogger.Domain.Services.Provisioning
{
    public class ScheduleJobOptions
    {
        public Guid? UserId { get; set; }

        [NotLogged]
        public string? DockerComposeYmlContents { get; set; }

        [NotLogged]
        public InstanceDockerFile[]? Files { get; set; }

        [NotLogged]
        public DockerAuthenticationArguments? Authentication { get; set; }
    }
}

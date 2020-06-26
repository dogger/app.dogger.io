using System;

namespace Dogger.Domain.Services.Provisioning.Stages.CompleteInstanceSetup
{
    public interface ICompleteInstanceSetupStage : IProvisioningStage
    {
        string? IpAddress { get; set; }
        string? InstanceName { get; set; }
        Guid? UserId { get; set; }
    }
}

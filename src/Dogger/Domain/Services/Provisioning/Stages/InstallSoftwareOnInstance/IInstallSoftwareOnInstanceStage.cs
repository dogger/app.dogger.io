using System;

namespace Dogger.Domain.Services.Provisioning.Stages.InstallSoftwareOnInstance
{
    public interface IInstallSoftwareOnInstanceStage : IProvisioningStage
    {
        Guid? UserId { get; set; }
        string? InstanceName { get; set; }
    }
}

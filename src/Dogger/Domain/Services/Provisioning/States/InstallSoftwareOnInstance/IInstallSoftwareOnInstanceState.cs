using System;

namespace Dogger.Domain.Services.Provisioning.States.InstallSoftwareOnInstance
{
    public interface IInstallSoftwareOnInstanceState : IProvisioningState
    {
        string? IpAddress { get; set; }
        Guid? UserId { get; set; }

        string? InstanceName { get; set; }
    }
}

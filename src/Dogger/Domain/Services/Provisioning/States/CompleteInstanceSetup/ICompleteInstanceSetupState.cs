using System;

namespace Dogger.Domain.Services.Provisioning.States.CompleteInstanceSetup
{
    public interface ICompleteInstanceSetupState : IProvisioningState
    {
        string? IpAddress { get; set; }
        string? InstanceName { get; set; }
        Guid? UserId { get; set; }
    }
}

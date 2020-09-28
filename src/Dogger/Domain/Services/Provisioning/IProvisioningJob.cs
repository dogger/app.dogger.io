using Dogger.Domain.Services.Provisioning.States;

namespace Dogger.Domain.Services.Provisioning
{
    public interface IProvisioningJob
    {
        bool IsEnded { get; }
        bool IsSucceeded { get; }
        bool IsFailed { get; }
        bool IsStarted { get; }

        string Id { get; }

        StateUpdateException? Exception { get; set; }
        IProvisioningState CurrentState { get; set; }
    }
}

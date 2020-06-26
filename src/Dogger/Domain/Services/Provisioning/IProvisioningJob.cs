using Dogger.Domain.Services.Provisioning.Stages;

namespace Dogger.Domain.Services.Provisioning
{
    public interface IProvisioningJob
    {
        bool IsEnded { get; }
        bool IsSucceeded { get; }
        bool IsFailed { get; }

        string Id { get; }

        StateUpdateException? Exception { get; set; }
        IProvisioningStage CurrentStage { get; set; }
    }
}

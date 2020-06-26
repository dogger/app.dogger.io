using Dogger.Domain.Services.Provisioning.Stages;

namespace Dogger.Domain.Services.Provisioning
{
    public interface IProvisioningJob
    {
        bool IsEnded { get; }
        bool IsSucceeded { get; }
        bool IsFailed { get; }

        string Id { get; }

        StageUpdateException? Exception { get; set; }
        IInstruction CurrentInstruction { get; set; }
    }
}

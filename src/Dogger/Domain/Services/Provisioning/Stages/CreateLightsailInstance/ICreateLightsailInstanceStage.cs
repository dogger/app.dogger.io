using Amazon.Lightsail.Model;

namespace Dogger.Domain.Services.Provisioning.Stages.CreateLightsailInstance
{
    public interface ICreateLightsailInstanceStage : IProvisioningStage
    {
        Models.Instance? DatabaseInstance { get; }
        string? PlanId { get; }
    }
}

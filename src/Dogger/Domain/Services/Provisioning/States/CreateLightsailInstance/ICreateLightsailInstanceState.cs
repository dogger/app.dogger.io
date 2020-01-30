using Amazon.Lightsail.Model;

namespace Dogger.Domain.Services.Provisioning.States.CreateLightsailInstance
{
    public interface ICreateLightsailInstanceState : IProvisioningState
    {
        Models.Instance? DatabaseInstance { get; }
        string? PlanId { get; }

        Instance CreatedLightsailInstance { get; }
    }
}

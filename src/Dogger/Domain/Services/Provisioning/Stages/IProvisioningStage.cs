using Dogger.Domain.Services.Provisioning.Instructions;

namespace Dogger.Domain.Services.Provisioning.Stages
{
    public interface IProvisioningStage
    {
        void AddInstructionsTo(
            IBlueprintBuilder blueprintBuilder);
    }

}

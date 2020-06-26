namespace Dogger.Domain.Services.Provisioning.Stages
{
    public interface IProvisioningStateContext
    {
        public IProvisioningStage CurrentStage { set; }
    }
}

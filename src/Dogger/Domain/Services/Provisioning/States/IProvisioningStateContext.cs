namespace Dogger.Domain.Services.Provisioning.States
{
    public interface IProvisioningStateContext
    {
        public IProvisioningState CurrentState { set; }
    }
}

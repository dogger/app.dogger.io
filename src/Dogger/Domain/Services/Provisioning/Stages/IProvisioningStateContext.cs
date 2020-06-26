namespace Dogger.Domain.Services.Provisioning.Stages
{
    public interface IProvisioningStateContext
    {
        IInstruction CurrentInstruction { get; set; }
    }
}

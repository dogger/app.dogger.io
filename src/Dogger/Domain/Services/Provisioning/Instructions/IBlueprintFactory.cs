using Dogger.Domain.Models;

namespace Dogger.Domain.Services.Provisioning.Instructions
{
    public interface IBlueprintFactory
    {
        Blueprint Create(
            string planId,
            Instance instance);
    }
}

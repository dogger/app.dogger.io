using Amazon.Lightsail.Model;
using Dogger.Domain.Services.Provisioning.Instructions.Models;

namespace Dogger.Domain.Services.Provisioning.Instructions
{
    public interface IAmazonLightsailInstructionFactory
    {
        HttpInstruction Create(CreateInstancesRequest request);
    }
}

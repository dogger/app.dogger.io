using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;

namespace Dogger.Domain.Services.Amazon.Lightsail
{
    public interface ILightsailOperationService
    {
        Task WaitForOperationsAsync(params Operation[] operations);
        Task WaitForOperationsAsync(IEnumerable<Operation> operations);

        Task<IReadOnlyCollection<Operation>> GetOperationsFromIdsAsync(IEnumerable<string> ids);
    }
}

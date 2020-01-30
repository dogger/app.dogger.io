using System.Threading.Tasks;
using Amazon.Runtime;

namespace Dogger.Domain.Services.Amazon.Identity
{
    public interface IUserAuthenticatedServiceFactory<T> where T : IAmazonService
    {
        Task<T> CreateAsync(string amazonUserName);
    }
}

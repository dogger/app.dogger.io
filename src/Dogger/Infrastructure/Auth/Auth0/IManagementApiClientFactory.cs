using System.Threading.Tasks;

namespace Dogger.Infrastructure.Auth.Auth0
{
    public interface IManagementApiClientFactory
    {
        Task<IManagementApiClient> CreateAsync();
    }
}

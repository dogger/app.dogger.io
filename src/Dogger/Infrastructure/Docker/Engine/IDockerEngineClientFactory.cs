using System.Threading.Tasks;

namespace Dogger.Infrastructure.Docker.Engine
{
    public interface IDockerEngineClientFactory
    {

        Task<IDockerEngineClient> CreateFromIpAddressAsync(string ipAddress);
    }
}

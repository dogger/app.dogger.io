using System.Threading.Tasks;

namespace Dogger.Infrastructure.Ssh
{
    public interface ISshClientFactory
    {
        Task<ISshClient> CreateForLightsailInstanceAsync(string ipAddress);
    }
}

using System.Threading;
using System.Threading.Tasks;

namespace Dogger.Infrastructure.AspNet
{
    public interface IDockerDependencyService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dogger.Infrastructure.Docker.Engine
{
    public interface IDockerEngineClient : IDisposable
    {
        Task<IReadOnlyCollection<ContainerResponse>> GetContainersAsync();

        Task<string> GetContainerLogsAsync(
            string containerId,
            int? linesToTake);
    }
}

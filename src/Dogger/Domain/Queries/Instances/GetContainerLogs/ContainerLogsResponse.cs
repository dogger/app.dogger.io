using Dogger.Infrastructure.Docker.Engine;

namespace Dogger.Domain.Queries.Instances.GetContainerLogs
{
    public class ContainerLogsResponse
    {
        public ContainerResponse Container { get; }
        public string Logs { get; }

        public ContainerLogsResponse(
            ContainerResponse container,
            string logs)
        {
            Container = container;
            Logs = logs;
        }
    }
}

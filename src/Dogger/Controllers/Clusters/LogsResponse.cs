namespace Dogger.Controllers.Clusters
{
    public class LogsResponse
    {
        public LogsResponse(
            string containerId,
            string containerImage,
            string logs)
        {
            this.ContainerImage = containerImage;
            this.ContainerId = containerId;
            this.Logs = logs;
        }

        public string ContainerImage { get; set; }
        public string ContainerId { get; set; }

        public string Logs { get; set; }
    }
}

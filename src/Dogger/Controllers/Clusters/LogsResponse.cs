namespace Dogger.Controllers.Clusters
{
    public class LogsResponse
    {
        public string? ContainerImage { get; set; }
        public string? ContainerId { get; set; }

        public string? Logs { get; set; }
    }
}

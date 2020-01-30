namespace Dogger.Infrastructure.Docker.Yml
{
    public struct ExposedPort
    {
        public int Port { get; set; }
        public SocketProtocol Protocol { get; set; }
    }
}

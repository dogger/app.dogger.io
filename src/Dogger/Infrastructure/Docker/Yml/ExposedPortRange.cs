namespace Dogger.Infrastructure.Docker.Yml
{
    public struct ExposedPortRange
    {
        public int FromPort { get; set; }
        public int ToPort { get; set; }

        public SocketProtocol Protocol { get; set; }

        public static implicit operator ExposedPortRange(ExposedPort port)
        {
            return new ExposedPortRange()
            {
                FromPort = port.Port,
                ToPort = port.Port,
                Protocol = port.Protocol
            };
        }
    }
}

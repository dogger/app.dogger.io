using Destructurama.Attributed;

namespace Dogger.Domain.Services.Provisioning.Arguments
{
    /// <summary>
    /// Represents a file located in the root folder on the server.
    /// </summary>
    public class InstanceDockerFile
    {
        public string Path { get; }

        [NotLogged]
        public byte[] Contents { get; }

        public InstanceDockerFile(
            string path,
            byte[] contents)
        {
            this.Path = path;
            this.Contents = contents;
        }
    }
}

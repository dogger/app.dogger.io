using Destructurama.Attributed;

namespace Dogger.Domain.Services.Provisioning.Arguments
{
    /// <summary>
    /// Represents a file located in the "dogger" folder on the server.
    /// </summary>
    public class InstanceDockerFile
    {
        public string Path { get; }

        [NotLogged]
        public string Contents { get; }

        public InstanceDockerFile(
            string path,
            string contents)
        {
            this.Path = path;
            this.Contents = contents;
        }
    }
}

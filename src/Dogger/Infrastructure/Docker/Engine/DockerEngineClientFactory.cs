using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ssh;
using Serilog;

namespace Dogger.Infrastructure.Docker.Engine
{
    [ExcludeFromCodeCoverage]
    public class DockerEngineClientFactory : IDockerEngineClientFactory
    {
        private readonly ISshClientFactory sshClientFactory;
        private readonly ILogger logger;

        public DockerEngineClientFactory(
            ISshClientFactory sshClientFactory,
            ILogger logger)
        {
            this.sshClientFactory = sshClientFactory;
            this.logger = logger;
        }

        private IDockerEngineClient CreateForSshClient(ISshClient sshClient)
        {
            return new DockerEngineClient(
                sshClient,
                logger);
        }

        public async Task<IDockerEngineClient> CreateFromIpAddressAsync(string ipAddress)
        {
            return CreateForSshClient(
                await sshClientFactory.CreateForLightsailInstanceAsync(ipAddress));
        }
    }
}

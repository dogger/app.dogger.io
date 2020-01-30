using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ssh;
using Serilog;

namespace Dogger.Infrastructure.Docker.Engine
{
    [ExcludeFromCodeCoverage]
    public class DockerEngineClient : IDockerEngineClient
    {
        private readonly ISshClient sshClient;
        private readonly ILogger logger;

        public DockerEngineClient(
            ISshClient sshClient,
            ILogger logger)
        {
            this.sshClient = sshClient;
            this.logger = logger;
        }

        public void Dispose()
        {
            sshClient.Dispose();
        }

        public async Task<IReadOnlyCollection<ContainerResponse>> GetContainersAsync()
        {
            return await ExecuteDockerEngineApiCallAsJsonResponse<ContainerResponse[]>("/containers/json");
        }

        public async Task<string> GetContainerLogsAsync(
            string containerId,
            int? linesToTake)
        {
            var tailArgument = linesToTake != null ?
                (object)linesToTake.Value :
                (object)"all";

            var response = await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                $"docker logs --details --tail {tailArgument} @containerId",
                new Dictionary<string, string?>()
                {
                    { "containerId", containerId }
                });
            return response;
        }

        private async Task<T> ExecuteDockerEngineApiCallAsJsonResponse<T>(
            string url,
            object? request = null)
        {
            var json = await ExecuteDockerEngineApiCallAsStringResponse(url, request);
            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (JsonException ex)
            {
                logger.Error(ex, "A JSON deserialization error occured while deserializing {JsonString} for url {Url}.", json, url);
                throw;
            }
        }

        private async Task<string> ExecuteDockerEngineApiCallAsStringResponse(string url, object? request)
        {
            const string dockerEngineApiVersion = "v1.40";

            var additionalOptions = string.Empty;
            if (request != null)
            {
                var requestJson = JsonSerializer.Serialize(request);
                additionalOptions = $@"-H ""Content-Type: application/json"" -d '{requestJson}' -X POST";
            }

            var text = await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                $@"sudo curl -s --unix-socket /var/run/docker.sock {additionalOptions} @url",
                new Dictionary<string, string?>()
                {
                    { "url", $"http:/{dockerEngineApiVersion}{url}" }
                });
            return text;
        }
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Infrastructure.AspNet.Options;
using Dogger.Infrastructure.Secrets;
using Serilog;
using Microsoft.Extensions.Options;
using Polly;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Dogger.Infrastructure.Ssh
{
    [ExcludeFromCodeCoverage]
    public class SshClientFactory : ISshClientFactory
    {
        private readonly ILogger logger;
        private readonly ISecretsScanner secretsScanner;
        private readonly IOptionsMonitor<AwsOptions> awsOptionsMonitor;
        private readonly IAmazonLightsail amazonLightsail;

        public SshClientFactory(
            ILogger logger,
            ISecretsScanner secretsScanner,
            IOptionsMonitor<AwsOptions> awsOptionsMonitor,
            IAmazonLightsail amazonLightsail)
        {
            this.logger = logger;
            this.secretsScanner = secretsScanner;
            this.awsOptionsMonitor = awsOptionsMonitor;
            this.amazonLightsail = amazonLightsail;
        }

        public async Task<ISshClient> CreateForLightsailInstanceAsync(string ipAddress)
        {
            var privateKey = this.awsOptionsMonitor
                .CurrentValue
                .LightsailPrivateKeyPem;
            if (string.IsNullOrWhiteSpace(privateKey))
                privateKey = await GetDefaultPrivateKeyAsync();

            var privateKeyPemBytes = Encoding.UTF8.GetBytes(privateKey);

            await using var stream = new MemoryStream(privateKeyPemBytes);
            var connectionInfo = new ConnectionInfo(
                ipAddress,
                "ubuntu",
                new PrivateKeyAuthenticationMethod(
                    "ubuntu",
                    new PrivateKeyFile(stream)))
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            var client = new SshClient(
                new SshClientDecorator(
                    new Renci.SshNet.SshClient(connectionInfo),
                    new Renci.SshNet.SftpClient(connectionInfo)),
                this.logger,
                secretsScanner);

            var policy = Policy
                .Handle<Exception>(exception =>
                    exception is SshOperationTimeoutException)
                .WaitAndRetryAsync(
                    5,
                    retryAttempt =>
                    {
                        if(retryAttempt > 3)
                            logger.Warning("Could not connect to SSH in attempt {RetryAttempt}.", retryAttempt);

                        return TimeSpan.FromSeconds(retryAttempt);
                    });

            await policy.ExecuteAsync(async () =>
                await client.ConnectAsync());

            return client;
        }

        private async Task<string> GetDefaultPrivateKeyAsync()
        {
            var response = await amazonLightsail.DownloadDefaultKeyPairAsync(new DownloadDefaultKeyPairRequest());
            return response.PrivateKeyBase64;
        }
    }
}

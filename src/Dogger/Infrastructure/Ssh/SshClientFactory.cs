using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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

        public SshClientFactory(
            ILogger logger,
            ISecretsScanner secretsScanner,
            IOptionsMonitor<AwsOptions> awsOptionsMonitor)
        {
            this.logger = logger;
            this.secretsScanner = secretsScanner;
            this.awsOptionsMonitor = awsOptionsMonitor;
        }

        public async Task<ISshClient> CreateForLightsailInstanceAsync(string ipAddress)
        {
            var privateKey = this.awsOptionsMonitor
                .CurrentValue
                .LightsailPrivateKeyPem;
            if (string.IsNullOrWhiteSpace(privateKey))
                throw new InvalidOperationException("Could not find the Lightsail private key PEM.");

            var privateKeyPemBytes = Encoding.UTF8.GetBytes(
                privateKey.Replace("\\n", "\n", StringComparison.InvariantCulture));

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
                    3,
                    retryAttempt =>
                    {
                        logger.Warning("Could not connect to SSH in attempt {RetryAttempt}.", retryAttempt);
                        return TimeSpan.FromSeconds(retryAttempt);
                    });

            await policy.ExecuteAsync(async () =>
                await client.ConnectAsync());

            return client;
        }
    }
}

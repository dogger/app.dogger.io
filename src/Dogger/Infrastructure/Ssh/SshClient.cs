using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Dogger.Infrastructure.Secrets;
using Serilog;
using Polly;
using Renci.SshNet.Common;

namespace Dogger.Infrastructure.Ssh
{
    public sealed class SshClient : ISshClient
    {
        private readonly ISshClientDecorator client;
        private readonly ILogger logger;
        private readonly ISecretsScanner secretsScanner;

        public SshClient(
            ISshClientDecorator client,
            ILogger logger,
            ISecretsScanner secretsScanner)
        {
            this.client = client;
            this.logger = logger;
            this.secretsScanner = secretsScanner;
        }

        public static string Sanitize(string? command)
        {
            if (string.IsNullOrEmpty(command))
                return string.Empty;

            var sanitized = command.Replace("'", @"'\''", StringComparison.InvariantCulture);
            return $"'{sanitized}'";
        }

        public async Task ConnectAsync()
        {
            const int totalWaitTimeInSeconds = 3 * 60;
            const int retryIntervalInSeconds = 10;

            var policy = Policy
                .Handle<Exception>(exception =>
                    exception is SshOperationTimeoutException ||
                    exception is SshConnectionException ||
                    exception is SocketException)
                .WaitAndRetryAsync(
                    totalWaitTimeInSeconds / retryIntervalInSeconds,
                    retryAttempt =>
                    {
                        if(retryAttempt > 3)
                            logger.Warning("Could not connect to SSH in attempt {RetryAttempt}.", retryAttempt);

                        return TimeSpan.FromSeconds(retryIntervalInSeconds);
                    });

            await policy.ExecuteAsync(client.ConnectAsync);
        }

        public async Task TransferFileAsync(
            SshRetryPolicy retryPolicy,
            string filePath,
            byte[] contents)
        {
            var policy = GetPollyPolicyFromSshRetryPolicy(retryPolicy);
            this.logger.Debug("Transfering file {FilePath}.", filePath);

            await policy.ExecuteAsync(async () =>
                await this.client.TransferFileAsync(
                    filePath,
                    contents));

            this.logger.Debug("File {FilePath} transferred.", filePath);
        }

        public async Task<string> ExecuteCommandAsync(
            SshRetryPolicy retryPolicy,
            SshResponseSensitivity dataSensitivity,
            string commandText,
            Dictionary<string, string?>? arguments = null)
        {
            this.secretsScanner.Scan(commandText);

            var policy = GetPollyPolicyFromSshRetryPolicy(retryPolicy);
            this.logger.Debug("Executing {CommandText}.", commandText);

            var commandResult = await policy.ExecuteAsync(async () =>
            {
                var result = await this.client.ExecuteCommandAsync(
                    GetSensitiveCommandText(commandText, arguments));

                var nonSensitiveText = dataSensitivity == SshResponseSensitivity.ContainsNoSensitiveData ? 
                    result.Text : 
                    string.Empty;

                if (result.ExitCode != 0)
                {
                    var sshCommandExecutionException = new SshCommandExecutionException(
                        commandText,
                        result);

                    this.logger.Debug(sshCommandExecutionException, "An error occured while executing the command {CommandText} with result {ExitCode}: {CommandResult}", commandText, result.ExitCode, StripSensitiveText(nonSensitiveText, arguments));

                    throw sshCommandExecutionException;
                }

                return result;
            });

            if (arguments == null && dataSensitivity == SshResponseSensitivity.ContainsNoSensitiveData)
            {
                this.logger.Debug("Command {CommandText} executed with response {ResponseText}.", commandText, commandResult.Text);
            }
            else
            {
                this.logger.Debug("Command {CommandText} executed.", commandText);
            }

            return commandResult.Text;
        }

        private static AsyncPolicy GetPollyPolicyFromSshRetryPolicy(SshRetryPolicy retryPolicy)
        {
            AsyncPolicy policy = Policy.NoOpAsync();
            if (retryPolicy == SshRetryPolicy.AllowRetries)
            {
                policy = Policy
                    .Handle<SshCommandExecutionException>()
                    .WaitAndRetryAsync(10, i => TimeSpan.FromSeconds(i * 5));
            }

            return policy;
        }

        private static string StripSensitiveText(string text, Dictionary<string, string?>? arguments)
        {
            if (arguments == null)
                return text;

            foreach (var argument in arguments)
            {
                if (string.IsNullOrWhiteSpace(argument.Value))
                    continue;

                text = text.Replace(
                    $"{argument.Value}",
                    "***",
                    StringComparison.InvariantCulture);
            }

            return text;
        }

        private static string GetSensitiveCommandText(string commandText, Dictionary<string, string?>? arguments)
        {
            if (arguments == null)
                return commandText.Trim();

            foreach (var argument in arguments)
            {
                var quotedKey = $"@{argument.Key}";
                var nonQuotedKey = $"@@{argument.Key}";
                if (!commandText.Contains(quotedKey, StringComparison.InvariantCulture) && !commandText.Contains(nonQuotedKey, StringComparison.InvariantCulture))
                    throw new CommandSanitizationException($"The argument {quotedKey} was not found in the command text.");

                commandText = commandText.Replace(
                    nonQuotedKey,
                    argument.Value,
                    StringComparison.InvariantCulture);

                commandText = commandText.Replace(
                    quotedKey,
                    Sanitize(argument.Value),
                    StringComparison.InvariantCulture);
            }

            return commandText.Trim();
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }

}

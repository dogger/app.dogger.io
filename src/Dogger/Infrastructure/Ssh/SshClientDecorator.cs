using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Dogger.Infrastructure.Ssh
{
    [ExcludeFromCodeCoverage]
    public class SshClientDecorator : ISshClientDecorator
    {
        private readonly Renci.SshNet.SshClient client;

        public SshClientDecorator(
            Renci.SshNet.SshClient client)
        {
            this.client = client;
        }

        public async Task<SshCommandResult> ExecuteCommandAsync(string text)
        {
            using var command = this.client.CreateCommand(text);
             command.CommandTimeout = TimeSpan.FromMinutes(15);

            await Task<string>.Factory
                .FromAsync(
                    command.BeginExecute(),
                    command.EndExecute,
                    TaskCreationOptions.AttachedToParent);

            return new SshCommandResult()
            {
                Text = string.IsNullOrWhiteSpace(command.Error) ? 
                    command.Result :
                    command.Error,
                ExitCode = command.ExitStatus
            };
        }

        public async Task ConnectAsync()
        {
            await Task.Factory.StartNew(
                this.client.Connect,
                default,
                default,
                TaskScheduler.Current);
        }

        public void Dispose()
        {
            this.client.Dispose();
        }
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Polly;
using Renci.SshNet;
using Renci.SshNet.Common;
using Serilog;

namespace Dogger.Infrastructure.Ssh
{
    using RenciSshClient = Renci.SshNet.SshClient;

    [ExcludeFromCodeCoverage]
    public class SshClientDecorator : ISshClientDecorator
    {
        private readonly RenciSshClient sshClient;
        private readonly SftpClient sftpClient;

        public SshClientDecorator(
            RenciSshClient sshClient,
            SftpClient sftpClient)
        {
            this.sshClient = sshClient;
            this.sftpClient = sftpClient;
        }

        public async Task<SshCommandResult> ExecuteCommandAsync(string text)
        {
            if (!this.sshClient.IsConnected)
                throw new InvalidOperationException("Not connected to SSH.");

            using var command = this.sshClient.CreateCommand(text);
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
            await Task.WhenAll(
                ConnectSshAsync(),
                ConnectSftpAsync());
        }

        public async Task TransferFileAsync(string filePath, byte[] contents)
        {
            if (!this.sftpClient.IsConnected)
                throw new InvalidOperationException("Not connected to SFTP.");

            await using var stream = new MemoryStream(contents);
            await Task.Factory
                .FromAsync(
                    this.sftpClient.BeginUploadFile(
                        stream, 
                        filePath),
                    this.sftpClient.EndUploadFile,
                    TaskCreationOptions.AttachedToParent);
        }

        private async Task ConnectSshAsync()
        {
            await Task.Factory.StartNew(
                this.sshClient.Connect,
                default,
                default,
                TaskScheduler.Current);
        }

        private async Task ConnectSftpAsync()
        {
            await Task.Factory.StartNew(
                this.sftpClient.Connect,
                default,
                default,
                TaskScheduler.Current);
        }

        public void Dispose()
        {
            this.sshClient.Dispose();
            this.sftpClient.Dispose();
        }
    }
}

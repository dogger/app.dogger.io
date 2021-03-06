﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;

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
            using var command = this.sshClient.CreateCommand(text);
            command.CommandTimeout = TimeSpan.FromMinutes(15);

            await Task<string>.Factory
                .FromAsync(
                    command.BeginExecute(),
                    command.EndExecute,
                    TaskCreationOptions.AttachedToParent);

            return new SshCommandResult()
            {
                Text = command.ExitStatus == 0 || string.IsNullOrWhiteSpace(command.Error) ? 
                    command.Result :
                    command.Error,
                ExitCode = command.ExitStatus
            };
        }

        public async Task ConnectAsync()
        {
            await ConnectSshAsync();
            await ConnectSftpAsync();
        }

        public async Task TransferFileAsync(string filePath, byte[] contents)
        {
            await using var stream = this.sftpClient.Create(filePath);
            await stream.WriteAsync(contents, CancellationToken.None);
        }

        private async Task ConnectSshAsync()
        {
            if (this.sshClient.IsConnected)
                return;

            await Task.Factory.StartNew(
                this.sshClient.Connect,
                default,
                default,
                TaskScheduler.Current);
        }

        private async Task ConnectSftpAsync()
        {
            if (this.sftpClient.IsConnected)
                return;

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

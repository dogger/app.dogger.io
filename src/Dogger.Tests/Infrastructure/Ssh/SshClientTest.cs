using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Infrastructure.Secrets;
using Dogger.Infrastructure.Ssh;
using Dogger.Tests.TestHelpers;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Renci.SshNet.Common;
using SshClient = Dogger.Infrastructure.Ssh.SshClient;

namespace Dogger.Tests.Infrastructure.Ssh
{
    [TestClass]
    public class SshClientTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Connect_SuccessfulConnection_ReturnsInstantly()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                Substitute.For<ISecretsScanner>());

            //Act
            await client.ConnectAsync();

            //Assert
            await fakeClientDecorator
                .Received(1)
                .ConnectAsync();
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Connect_FailedConnection_RetriesAgain()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                Substitute.For<ISecretsScanner>());

            fakeClientDecorator
                .ConnectAsync()
                .Returns(
                    Task.FromException(new SshConnectionException()),
                    Task.CompletedTask);

            //Act
            await client.ConnectAsync();

            //Assert
            await fakeClientDecorator
                .Received(2)
                .ConnectAsync();
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ExecuteCommand_SensitiveParameterNotFoundInCommandText_ExceptionThrown()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                Substitute.For<ISecretsScanner>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<CommandSanitizationException>(async () => 
                await client.ExecuteCommandAsync(
                    SshRetryPolicy.AllowRetries,
                    SshResponseSensitivity.ContainsNoSensitiveData,
                    "some-text",
                    new Dictionary<string, string>()
                    {
                        {
                            "foo", "bar"
                        }
                    }));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ExecuteCommand_AllowRetriesAndSuccessfulCommand_ReturnsInstantly()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                Substitute.For<ISecretsScanner>());

            //Act
            await client.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                SshResponseSensitivity.ContainsNoSensitiveData,
                "some-text");

            //Assert
            await fakeClientDecorator
                .Received(1)
                .ExecuteCommandAsync(
                    Arg.Any<string>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task SanitizeBashCommand_DummyCommandGiven_InsertsEscapeCharactersEverywhere()
        {
            //Arrange
            const string input = "I'm a s@fe $tring which ends in newline\n";

            //Act
            var output = SshClient.Sanitize(input);

            //Assert
            Assert.AreEqual("'I'\\''m a s@fe $tring which ends in newline\n'", output);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ExecuteCommand_AllowRetriesAndFailedCommand_RetriesAgain()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                Substitute.For<ISecretsScanner>());

            fakeClientDecorator
                .ExecuteCommandAsync("some-text")
                .Returns(
                    Task.FromResult(new SshCommandResult()
                    {
                        ExitCode = 1
                    }),
                    Task.FromResult(new SshCommandResult()));

            //Act
            await client.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                SshResponseSensitivity.ContainsNoSensitiveData,
                "some-text");

            //Assert
            await fakeClientDecorator
                .Received(2)
                .ExecuteCommandAsync(
                    Arg.Any<string>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ExecuteCommand_AllowRetriesAndFailedCommand_ScansSecretsTwice()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();
            var fakeSecretsScanner = Substitute.For<ISecretsScanner>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                fakeSecretsScanner);

            fakeClientDecorator
                .ExecuteCommandAsync("some-text")
                .Returns(
                    Task.FromResult(new SshCommandResult()
                    {
                        ExitCode = 1,
                        Text = "error-text"
                    }),
                    Task.FromResult(new SshCommandResult()
                    {
                        Text = "success-text"
                    }));

            //Act
            await client.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                SshResponseSensitivity.ContainsNoSensitiveData,
                "some-text");

            //Assert
            await fakeClientDecorator
                .Received(2)
                .ExecuteCommandAsync(
                    Arg.Any<string>());

            fakeSecretsScanner
                .Received(1)
                .Scan("some-text");

            fakeSecretsScanner
                .Received(1)
                .Scan("error-text");

            fakeSecretsScanner
                .Received(1)
                .Scan("success-text");
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ExecuteCommand_ProhibitRetriesAndSuccessfulCommand_ReturnsInstantly()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                Substitute.For<ISecretsScanner>());

            fakeClientDecorator
                .ExecuteCommandAsync("some-text")
                .Returns(
                    Task.FromResult(new SshCommandResult()));

            //Act
            await client.ExecuteCommandAsync(
                SshRetryPolicy.ProhibitRetries,
                SshResponseSensitivity.ContainsNoSensitiveData,
                "some-text");

            //Assert
            await fakeClientDecorator
                .Received(1)
                .ExecuteCommandAsync(
                    Arg.Any<string>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ExecuteCommand_ProhibitRetriesAndFailedCommand_ThrowsException()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                Substitute.For<ISecretsScanner>());

            fakeClientDecorator
                .ExecuteCommandAsync("some-text")
                .Returns(
                    Task.FromResult(new SshCommandResult()
                    {
                        ExitCode = 1
                    }));

            //Act
            var exception = await Assert.ThrowsExceptionAsync<SshCommandExecutionException>(async () =>
                await client.ExecuteCommandAsync(
                    SshRetryPolicy.ProhibitRetries,
                    SshResponseSensitivity.ContainsNoSensitiveData,
                    "some-text"));

            //Assert
            Assert.IsNotNull(exception);

            await fakeClientDecorator
                .Received(1)
                .ExecuteCommandAsync(
                    Arg.Any<string>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ExecuteCommand_ExposedDataSensitivityAndFailedCommand_SensitiveDataIncludedInThrownException()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                Substitute.For<ISecretsScanner>());

            fakeClientDecorator
                .ExecuteCommandAsync("some-text")
                .Returns(
                    Task.FromResult(new SshCommandResult()
                    {
                        ExitCode = 1,
                        Text = "some-result"
                    }));

            //Act
            var exception = await Assert.ThrowsExceptionAsync<SshCommandExecutionException>(async () =>
                await client.ExecuteCommandAsync(
                    SshRetryPolicy.ProhibitRetries,
                    SshResponseSensitivity.ContainsNoSensitiveData,
                    "some-text"));

            //Assert
            Assert.IsNotNull(exception);

            Assert.AreEqual("some-text", exception.CommandText);
            Assert.AreEqual(1, exception.Result.ExitCode);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ExecuteCommand_SensitiveDataSensitivityAndFailedCommand_SensitiveDataNotIncludedInThrownException()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                Substitute.For<ISecretsScanner>());

            fakeClientDecorator
                .ExecuteCommandAsync("'some-text'")
                .Returns(
                    Task.FromResult(new SshCommandResult()
                    {
                        ExitCode = 1,
                        Text = "some-result"
                    }));

            //Act
            var exception = await Assert.ThrowsExceptionAsync<SshCommandExecutionException>(async () =>
                await client.ExecuteCommandAsync(
                    SshRetryPolicy.ProhibitRetries,
                    SshResponseSensitivity.ContainsNoSensitiveData,
                    "@alias",
                    new Dictionary<string, string>()
                    {
                        { "alias", "some-text" }
                    }));

            //Assert
            Assert.IsNotNull(exception);

            Assert.AreEqual("@alias", exception.CommandText);
            Assert.AreEqual(1, exception.Result.ExitCode);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ExecuteCommand_SensitiveDataSensitivityAndSuccessfulCommand_HighwayTest()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                Substitute.For<ISecretsScanner>());

            fakeClientDecorator
                .ExecuteCommandAsync("'some-text'")
                .Returns(
                    Task.FromResult(new SshCommandResult()
                    {
                        ExitCode = 0,
                        Text = "some-result"
                    }));

            //Act
            var result = await client.ExecuteCommandAsync(
                SshRetryPolicy.ProhibitRetries,
                SshResponseSensitivity.ContainsNoSensitiveData,
                "@alias",
                new Dictionary<string, string>()
                {
                    { "alias", "some-text" }
                });

            //Assert
            Assert.AreEqual("some-result", result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Dispose_ClientDecoratorGiven_DisposesClientDecorator()
        {
            //Arrange
            var fakeClientDecorator = Substitute.For<ISshClientDecorator>();
            var fakeLogger = Substitute.For<ILogger>();

            var client = new SshClient(
                fakeClientDecorator,
                fakeLogger,
                Substitute.For<ISecretsScanner>());

            //Act
            client.Dispose();

            //Assert
            fakeClientDecorator
                .Received(1)
                .Dispose();
        }
    }
}

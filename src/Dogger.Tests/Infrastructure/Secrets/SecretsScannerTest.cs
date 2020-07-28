using System;
using Dogger.Infrastructure.Secrets;
using Dogger.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace Dogger.Tests.Infrastructure.Secrets
{
    [TestClass]
    public class SecretsScannerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void Scan_SecretFoundInContent_ThrowsException()
        {
            //Arrange
            var configuration = new ConfigurationBuilder()
                .AddCommandLine(Array.Empty<string>())
                .Build();
            configuration["SOME_KEY"] = "SOME_SECRET_VERY_LONG";

            var scanner = new SecretsScanner(
                configuration,
                Substitute.For<ILogger>());

            //Arrange
            var exception = Assert.ThrowsException<InvalidOperationException>(() =>
                scanner.Scan("hello some_secret_very_long lol"));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void Scan_SecretNotFoundInContent_DoesNotThrowException()
        {
            //Arrange
            var configuration = new ConfigurationBuilder()
                .AddCommandLine(Array.Empty<string>())
                .Build();
            configuration["SOME_KEY"] = "SOME_SECRET_VERY_LONG";

            var scanner = new SecretsScanner(
                configuration,
                Substitute.For<ILogger>());

            //Arrange
            scanner.Scan("hello some value lol");

            //No assert
        }
    }
}

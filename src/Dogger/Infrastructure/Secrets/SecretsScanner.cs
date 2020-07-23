using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Dogger.Infrastructure.Secrets
{

    public class SecretsScanner : ISecretsScanner
    {
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        public SecretsScanner(
            IConfiguration configuration,
            ILogger logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public void Scan(string? content)
        {
            if (content == null)
                return;

            logger.Verbose("Scanning for leaked secrets.");
            foreach (var pair in this.configuration.AsEnumerable())
            {
                var secretName = pair.Key;
                var secretValue = pair.Value;
                if (string.IsNullOrWhiteSpace(secretValue) || secretValue.Length < 10)
                    continue;

                if (content.Contains(secretValue, StringComparison.InvariantCultureIgnoreCase))
                    throw new InvalidOperationException($"Prevented a potential leak of secret {secretName}.");
            }
        }
    }
}

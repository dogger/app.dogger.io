using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Dogger.Infrastructure.Configuration
{
    public static class ConfigurationFactory
    {
        public static IConfigurationRoot BuildConfiguration(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCommandLine(args);

            if (Debugger.IsAttached)
            {
                configurationBuilder.AddJsonFile("appsettings.Development.json");
                configurationBuilder.AddUserSecrets("be404feb-b81c-425a-b355-029dbd854c3d");
            }

            var configuration = configurationBuilder.Build();
            return configuration;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Dogger.Infrastructure
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

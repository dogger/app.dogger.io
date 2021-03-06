﻿using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Dogger.Infrastructure.Configuration
{
    public static class ConfigurationFactory
    {
        public static IConfigurationRoot BuildConfiguration(string secretId, string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCommandLine(args);

            if (Debugger.IsAttached && !EnvironmentHelper.IsRunningInTest)
            {
                configurationBuilder.AddJsonFile("appsettings.Development.json");
                configurationBuilder.AddUserSecrets(secretId);
            }

            var configuration = configurationBuilder.Build();
            return configuration;
        }
    }
}

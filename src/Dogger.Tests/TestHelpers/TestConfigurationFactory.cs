using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Dogger.Tests.TestHelpers
{
    public static class TestConfigurationFactory
    {
        public static void ConfigureConfigurationBuilder(IConfigurationBuilder builder)
        {
            foreach (var source in builder.Sources.ToArray())
            {
                if (source is ChainedConfigurationSource)
                    continue;

                builder.Sources.Remove(source);
            }

            builder
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{Microsoft.Extensions.Hosting.Environments.Development}.json", false);
        }
    }
}

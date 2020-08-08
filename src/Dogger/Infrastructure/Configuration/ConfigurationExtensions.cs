﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationExtensions
    {
        public static string GetSectionNameFor<TOptions>(this IConfiguration _)
        {
            const string optionsSuffix = "Options";

            var configurationKey = typeof(TOptions).Name;
            if (configurationKey.EndsWith(optionsSuffix, StringComparison.InvariantCulture))
            {
                configurationKey = configurationKey.Replace(
                    optionsSuffix,
                    string.Empty,
                    StringComparison.InvariantCulture);
            }

            return configurationKey;
        }

        public static TOptions GetSection<TOptions>(this IConfiguration configuration, string? name = null)
        {
            return configuration
                .GetSection(name ?? GetSectionNameFor<TOptions>(configuration))
                .Get<TOptions>();
        }
    }
}
﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dogger.Infrastructure
{
    public static class JsonFactory
    {
        public static JsonSerializerOptions GetOptions()
        {
            return new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                IgnoreNullValues = true,
                IgnoreReadOnlyProperties = true,
                Converters =
                {
                    new JsonTimeSpanConverter(),
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
        }
    }
}

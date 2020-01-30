using System;
using System.Diagnostics.CodeAnalysis;

namespace Dogger.Infrastructure
{
    public static class EnvironmentHelper
    {
        private const string isRunningInTestEnvironmentVariableKey = "DOTNET_RUNNING_IN_TEST";

        public static bool IsRunningInTest => IsEnabled(isRunningInTestEnvironmentVariableKey);
        public static bool IsRunningInContainer => IsEnabled("DOTNET_RUNNING_IN_CONTAINER");

        private static bool IsEnabled(string key)
        {
            return Environment.GetEnvironmentVariable(key) == "true";
        }

        [ExcludeFromCodeCoverage]
        public static void SetRunningInTestFlag()
        {
            Environment.SetEnvironmentVariable(
                isRunningInTestEnvironmentVariableKey,
                "true");

            if (!IsRunningInTest)
                throw new InvalidOperationException("Could not set RunningInTest flag.");
        }
    }
}

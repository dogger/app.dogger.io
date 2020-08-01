using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Tests.TestHelpers
{
    public class EnvironmentSetupOptions
    {
        public string EnvironmentName { get; set; }
        public Action<IServiceCollection> IocConfiguration { get; set; }
        public bool SkipWebServer { get; set; }
    }
}

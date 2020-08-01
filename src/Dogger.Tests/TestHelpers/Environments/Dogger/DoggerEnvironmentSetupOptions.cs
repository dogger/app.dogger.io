using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Tests.TestHelpers.Environments.Dogger
{
    public class DoggerEnvironmentSetupOptions
    {
        public string EnvironmentName { get; set; }
        public Action<IServiceCollection> IocConfiguration { get; set; }
        public bool IncludeWebServer { get; set; }
    }
}

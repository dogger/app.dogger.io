using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Setup.Tests.TestHelpers.Environments
{
    class DoggerSetupEnvironmentSetupOptions
    {
        public Action<IServiceCollection> IocConfiguration { get; set; }
    }
}

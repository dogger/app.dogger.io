using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Tests.TestHelpers
{
    public static class AutoMapperFactory
    {
        public static IMapper CreateValidMapper()
        {
            var serviceCollection = TestServiceProviderFactory.CreateUsingStartup();
            return serviceCollection.GetRequiredService<IMapper>();
        }
    }
}

using AutoMapper;
using Dogger.Controllers.Clusters;
using Dogger.Controllers.Jobs;
using Dogger.Controllers.Payment;
using Dogger.Controllers.Plans;
using Dogger.Controllers.PullDog.Api;
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

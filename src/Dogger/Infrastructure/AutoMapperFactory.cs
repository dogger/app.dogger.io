using AutoMapper;
using Dogger.Controllers.Clusters;
using Dogger.Controllers.Jobs;
using Dogger.Controllers.Payment;
using Dogger.Controllers.Plans;
using Dogger.Controllers.PullDog.Api;

namespace Dogger.Infrastructure
{
    public static class AutoMapperFactory
    {
        public static IMapper CreateValidMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<PlansMappingProfile>();
                cfg.AddProfile<JobsMappingProfile>();
                cfg.AddProfile<ClustersMappingProfile>();
                cfg.AddProfile<PaymentMappingProfile>();
                cfg.AddProfile<PullDogApiMappingProfile>();
            });
            config.AssertConfigurationIsValid();

            return config.CreateMapper();
        }
    }
}

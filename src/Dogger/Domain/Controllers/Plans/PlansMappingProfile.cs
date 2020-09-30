using AutoMapper;
using Dogger.Domain.Services.Provisioning;

namespace Dogger.Domain.Controllers.Plans
{
    public class PlansMappingProfile : Profile
    {
        public PlansMappingProfile()
        {
            CreateMap<Domain.Queries.Plans.GetSupportedPlans.Plan, PlanResponse>()
                .ForMember(x => x.RamSizeInMegabytes, x => x.MapFrom(y => (int)(y.Bundle.RamSizeInGb * 1024)))
                .ForMember(x => x.CpuCount, x => x.MapFrom(y => y.Bundle.CpuCount))
                .ForMember(x => x.Power, x => x.MapFrom(y => y.Bundle.Power))
                .ForMember(x => x.DiskSizeInGigabytes, x => x.MapFrom(y => y.Bundle.DiskSizeInGb))
                .ForMember(x => x.TransferPerMonthInGigabytes, x => x.MapFrom(y => y.Bundle.TransferPerMonthInGb));

            CreateMap<Domain.Queries.Plans.GetSupportedPlans.PullDogPlan, PullDogPlanResponse>();

            CreateMap<ProvisioningJob, PlanProvisionResponse>()
                .ForMember(x => x.Status, x => x.MapFrom(y => y))
                .ForMember(x => x.JobId, x => x.MapFrom(y => y.Id));

            CreateMap<PreCompletedProvisioningJob, PlanProvisionResponse>()
                .ForMember(x => x.Status, x => x.MapFrom(y => y))
                .ForMember(x => x.JobId, x => x.MapFrom(y => y.Id));
        }
    }
}

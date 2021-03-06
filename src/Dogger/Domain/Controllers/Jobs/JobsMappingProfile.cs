﻿using AutoMapper;
using Dogger.Domain.Services.Provisioning;

namespace Dogger.Domain.Controllers.Jobs
{
    public class JobsMappingProfile : Profile
    {
        public JobsMappingProfile()
        {
            CreateMap<ProvisioningJob, JobStatusResponse>()
                .ForMember(x => x.StateDescription, x => x.MapFrom(y => y.CurrentState!.Description));

            CreateMap<PreCompletedProvisioningJob, JobStatusResponse>()
                .ForMember(x => x.StateDescription, x => x.MapFrom(y => string.Empty));
        }
    }
}

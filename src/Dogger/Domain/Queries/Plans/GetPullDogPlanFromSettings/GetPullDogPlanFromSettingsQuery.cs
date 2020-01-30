using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings
{
    public class GetPullDogPlanFromSettingsQuery : IRequest<PullDogPlan?>
    {
        public string DoggerPlanId { get; }
        public int PoolSize { get; }

        public GetPullDogPlanFromSettingsQuery(
            string doggerPlanId, 
            int poolSize)
        {
            this.DoggerPlanId = doggerPlanId;
            this.PoolSize = poolSize;
        }
    }
}

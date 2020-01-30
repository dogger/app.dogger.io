using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.ChangePullDogPlan
{
    public class ChangePullDogPlanCommand : IRequest, IDatabaseTransactionRequest
    {
        public Guid UserId { get; }

        public int PoolSize { get; }
        public string PlanId { get; }

        public ChangePullDogPlanCommand(
            Guid userId,
            int poolSize,
            string planId)
        {
            this.UserId = userId;
            this.PoolSize = poolSize;
            this.PlanId = planId;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

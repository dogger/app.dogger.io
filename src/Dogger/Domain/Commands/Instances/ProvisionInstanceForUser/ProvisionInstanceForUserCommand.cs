using System.Data;
using Amazon.Lightsail.Model;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Domain.Services.Provisioning;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.Instances.ProvisionInstanceForUser
{
    public class ProvisionInstanceForUserCommand : IRequest<IProvisioningJob>, IDatabaseTransactionRequest
    {
        public Plan Plan { get; }
        public User User { get; }

        public ProvisionInstanceForUserCommand(
            Plan plan,
            User user)
        {
            Plan = plan;
            User = user;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

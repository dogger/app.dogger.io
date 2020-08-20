using System.Data;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.InstallPullDogFromGitHub
{
    public class InstallPullDogFromGitHubCommand : IRequest, IDatabaseTransactionRequest
    {
        public string Code { get; }
        public long InstallationId { get; }

        public PullDogPlan? Plan { get; set; }

        public InstallPullDogFromGitHubCommand(
            string code,
            long installationId)
        {
            this.Code = code;
            this.InstallationId = installationId;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

using System.Data;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.InstallPullDogFromEmails
{
    public class InstallPullDogFromEmailsCommand : IRequest<User>, IDatabaseTransactionRequest
    {
        public string[] Emails { get; }

        public PullDogPlan? Plan { get; set; }

        public InstallPullDogFromEmailsCommand(
            string[] emails)
        {
            this.Emails = emails;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

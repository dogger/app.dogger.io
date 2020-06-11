using System.Data;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Queries.Instances.GetExpiredInstances
{
    public class GetExpiredInstancesQuery : IRequest<Instance[]>, IDatabaseTransactionRequest
    {
        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

using System;
using System.Data;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.Clusters.EnsureClusterWithId
{
    public class EnsureClusterWithIdCommand : IRequest<Cluster>, IDatabaseTransactionRequest
    {
        public EnsureClusterWithIdCommand(Guid id)
        {
            this.Id = id;
        }

        public Guid Id { get; }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

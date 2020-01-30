using System;
using System.Data;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.Clusters.EnsureClusterForUser
{
    public class EnsureClusterForUserCommand : IRequest<Cluster>, IDatabaseTransactionRequest
    {
        public Guid UserId { get; }

        public string? ClusterName { get; set; }

        public EnsureClusterForUserCommand(
            Guid userId)
        {
            this.UserId = userId;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

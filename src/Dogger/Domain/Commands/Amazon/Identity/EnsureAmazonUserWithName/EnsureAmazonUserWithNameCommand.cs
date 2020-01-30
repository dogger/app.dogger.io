using System;
using System.Data;
using System.Diagnostics;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.Identity.EnsureAmazonUserWithName
{
    [DebuggerStepThrough]
    public class EnsureAmazonUserWithNameCommand : IRequest<AmazonUser>, IDatabaseTransactionRequest
    {
        public string Name { get; }

        public Guid? UserId { get; set; }

        public EnsureAmazonUserWithNameCommand(string name)
        {
            this.Name = name;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

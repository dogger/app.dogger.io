using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.DeletePullDogRepository
{
    public class DeletePullDogRepositoryCommand : IRequest, IDatabaseTransactionRequest
    {
        public string Handle { get; }

        public DeletePullDogRepositoryCommand(
            string handle)
        {
            this.Handle = handle;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

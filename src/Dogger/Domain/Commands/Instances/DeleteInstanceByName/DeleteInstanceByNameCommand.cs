using System.Data;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.Instances.DeleteInstanceByName
{
    public class DeleteInstanceByNameCommand : IRequest, IDatabaseTransactionRequest
    {
        public string Name { get; set; }

        public DeleteInstanceByNameCommand(
            string name)
        {
            this.Name = name;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

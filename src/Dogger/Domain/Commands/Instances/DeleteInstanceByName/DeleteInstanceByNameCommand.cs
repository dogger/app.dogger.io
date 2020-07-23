using System.Data;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.Instances.DeleteInstanceByName
{
    public class DeleteInstanceByNameCommand : IRequest, IDatabaseTransactionRequest
    {
        public InitiatorType InitiatedBy { get; }

        public string Name { get; set; }

        public DeleteInstanceByNameCommand(string name, InitiatorType initiatedBy)
        {
            this.InitiatedBy = initiatedBy;
            this.Name = name;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }

}

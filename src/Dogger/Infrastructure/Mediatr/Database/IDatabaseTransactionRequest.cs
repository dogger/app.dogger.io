using System.Data;

namespace Dogger.Infrastructure.Mediatr.Database
{
    public interface IDatabaseTransactionRequest
    {
        public IsolationLevel? TransactionIsolationLevel { get; }
    }
}

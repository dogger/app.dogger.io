using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Infrastructure.Mediatr.Database
{
    public class DatabaseTransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
        where TRequest : notnull
    {
        private readonly DataContext dataContext;

        public DatabaseTransactionBehavior(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<TResponse> Handle(
            TRequest request, 
            CancellationToken cancellationToken, 
            RequestHandlerDelegate<TResponse> next)
        {
            if (!(request is IDatabaseTransactionRequest databaseTransactionRequest))
                return await next();

            return await this.dataContext.ExecuteInTransactionAsync(
                async () => await next(),
                databaseTransactionRequest.TransactionIsolationLevel,
                cancellationToken);
        }
    }
}

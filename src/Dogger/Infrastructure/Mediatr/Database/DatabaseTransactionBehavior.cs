using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Infrastructure.Mediatr.Database
{
    public class DatabaseTransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly DataContext dataContext;

        [DebuggerStepThrough]
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

            if (this.dataContext.Database.CurrentTransaction != null)
                return await next();

            var strategy = this.dataContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await this.dataContext.Database.BeginTransactionAsync(
                    databaseTransactionRequest.TransactionIsolationLevel ?? IsolationLevel.RepeatableRead,
                    cancellationToken);

                try
                {
                    var result = await next();
                    await transaction.CommitAsync(cancellationToken);

                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
    }
}

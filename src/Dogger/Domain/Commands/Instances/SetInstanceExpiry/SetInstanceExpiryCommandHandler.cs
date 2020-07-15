using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Commands.Instances.SetInstanceExpiry
{
    public class SetInstanceExpiryCommandHandler : IRequestHandler<SetInstanceExpiryCommand, Unit>
    {
        private readonly DataContext dataContext;

        public SetInstanceExpiryCommandHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<Unit> Handle(
            SetInstanceExpiryCommand request,
            CancellationToken cancellationToken)
        {
            var instance = await dataContext
                .Instances
                .SingleAsync(
                    x => x.Name == request.InstanceName,
                    cancellationToken);
            instance.ExpiresAtUtc = request.Expiry;

            await this.dataContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}


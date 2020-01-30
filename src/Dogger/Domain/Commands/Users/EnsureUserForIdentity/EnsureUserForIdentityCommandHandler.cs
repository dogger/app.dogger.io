using System;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Users.GetUserByIdentityName;
using MediatR;

namespace Dogger.Domain.Commands.Users.EnsureUserForIdentity
{
    public class EnsureUserForIdentityCommandHandler : IRequestHandler<EnsureUserForIdentityCommand, User>
    {
        private readonly IMediator mediator;

        public EnsureUserForIdentityCommandHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<User> Handle(EnsureUserForIdentityCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.IdentityName))
                throw new InvalidOperationException("No identity name specified.");

            var existingUser = await this.mediator.Send(
                new GetUserByIdentityNameQuery(request.IdentityName),
                cancellationToken);
            if (existingUser != null)
                return existingUser;

            var newUser = await this.mediator.Send(
                new CreateUserForIdentityCommand(
                    request.IdentityName,
                    request.Email), 
                cancellationToken);

            return newUser;
        }
    }
}

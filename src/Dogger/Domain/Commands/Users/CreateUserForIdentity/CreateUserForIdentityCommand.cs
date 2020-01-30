using System.Data;
using System.Linq;
using System.Security.Claims;
using Destructurama.Attributed;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.Users.CreateUserForIdentity
{
    public class CreateUserForIdentityCommand : IRequest<User>, IDatabaseTransactionRequest
    {
        public string IdentityName { get; }

        [LogMasked(ShowFirst = 2)]
        public string Email { get; }

        public CreateUserForIdentityCommand(
            string identityName,
            string email)
        {
            this.IdentityName = identityName;
            this.Email = email;
        }

        public CreateUserForIdentityCommand(ClaimsPrincipal claimsPrincipal) : this(
            claimsPrincipal.Identity?.Name ?? throw new IdentityNameNotProvidedException(),
            claimsPrincipal.Claims.Single(x => x.Type == ClaimTypes.Email).Value)
        {
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

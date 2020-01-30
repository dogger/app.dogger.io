using System.Data;
using System.Linq;
using System.Security.Claims;
using Destructurama.Attributed;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.Users.EnsureUserForIdentity
{
    public class EnsureUserForIdentityCommand : IRequest<User>, IDatabaseTransactionRequest
    {
        public string IdentityName { get; }

        [LogMasked(ShowFirst = 2)]
        public string Email { get; }

        public EnsureUserForIdentityCommand(
            string identityName,
            string email)
        {
            this.IdentityName = identityName;
            this.Email = email;
        }

        public EnsureUserForIdentityCommand(ClaimsPrincipal claimsPrincipal) : this(
            claimsPrincipal.Identity?.Name ?? throw new IdentityNameNotProvidedException(),
            claimsPrincipal.Claims.Single(x => x.Type == ClaimTypes.Email).Value)
        {
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}

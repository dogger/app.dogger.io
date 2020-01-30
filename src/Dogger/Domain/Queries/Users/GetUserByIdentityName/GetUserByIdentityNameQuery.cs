using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.Users.GetUserByIdentityName
{
    public class GetUserByIdentityNameQuery : IRequest<User>
    {
        public string IdentityName { get; }

        public GetUserByIdentityNameQuery(
            string identityName)
        {
            IdentityName = identityName;
        }
    }
}

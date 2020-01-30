using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.Users.GetUserByIdentityName
{
    public class GetUserByIdentityNameQueryHandler : IRequestHandler<GetUserByIdentityNameQuery, User>
    {
        private readonly DataContext dataContext;

        public GetUserByIdentityNameQueryHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<User> Handle(GetUserByIdentityNameQuery request, CancellationToken cancellationToken)
        {
            if(string.IsNullOrWhiteSpace(request.IdentityName))
                throw new IdentityNameNotProvidedException();

            return await this.dataContext
                .Users
                .Include(x => x.PullDogSettings)
                .ThenInclude(x => x!.Repositories)
                .FirstOrDefaultAsync(
                    userCandidate => userCandidate
                        .Identities
                        .Any(identity =>
                            identity.UserId == userCandidate.Id &&
                            identity.Name == request.IdentityName),
                    cancellationToken);
        }
    }
}

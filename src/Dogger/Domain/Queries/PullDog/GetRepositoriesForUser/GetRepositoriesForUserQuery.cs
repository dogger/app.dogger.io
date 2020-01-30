using System.Collections.Generic;
using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.PullDog.GetRepositoriesForUser
{
    public class GetRepositoriesForUserQuery : IRequest<IReadOnlyCollection<UserRepositoryResponse>>
    {
        public User User { get; }

        public GetRepositoriesForUserQuery(
            User user)
        {
            this.User = user;
        }
    }

}

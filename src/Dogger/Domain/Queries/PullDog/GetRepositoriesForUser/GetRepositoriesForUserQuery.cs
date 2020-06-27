using System;
using System.Collections.Generic;
using MediatR;

namespace Dogger.Domain.Queries.PullDog.GetRepositoriesForUser
{
    public class GetRepositoriesForUserQuery : IRequest<IReadOnlyCollection<UserRepositoryResponse>>
    {
        public Guid UserId { get; }

        public GetRepositoriesForUserQuery(
            Guid userId)
        {
            this.UserId = userId;
        }
    }

}

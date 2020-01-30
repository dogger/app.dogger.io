using System;
using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.Users.GetUserById
{
    public class GetUserByIdQuery : IRequest<User>
    {
        public Guid UserId { get; }

        public GetUserByIdQuery(Guid userId)
        {
            this.UserId = userId;
        }
    }
}

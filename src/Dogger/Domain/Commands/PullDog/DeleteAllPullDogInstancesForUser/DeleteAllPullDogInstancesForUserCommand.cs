using System;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.DeleteAllPullDogInstancesForUser
{
    public class DeleteAllPullDogInstancesForUserCommand : IRequest
    {
        public Guid UserId { get; }

        public DeleteAllPullDogInstancesForUserCommand(
            Guid userId)
        {
            this.UserId = userId;
        }
    }
}

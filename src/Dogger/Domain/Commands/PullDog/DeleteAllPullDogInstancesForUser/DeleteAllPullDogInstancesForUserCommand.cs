using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

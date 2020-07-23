using System;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.DeleteAllPullDogInstancesForUser
{
    public class DeleteAllPullDogInstancesForUserCommand : IRequest
    {
        public InitiatorType InitiatedBy { get; }
        public Guid UserId { get; }

        public DeleteAllPullDogInstancesForUserCommand(
            Guid userId, 
            InitiatorType initiatedBy)
        {
            this.UserId = userId;
            this.InitiatedBy = initiatedBy;
        }
    }
}

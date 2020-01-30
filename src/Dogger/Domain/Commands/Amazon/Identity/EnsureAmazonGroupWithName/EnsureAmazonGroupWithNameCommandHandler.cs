using System.Threading;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Dogger.Domain.Queries.Amazon.Identity.GetAmazonGroupByName;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.Identity.EnsureAmazonGroupWithName
{
    public class EnsureAmazonGroupWithNameCommandHandler : IRequestHandler<EnsureAmazonGroupWithNameCommand, Group>
    {
        private readonly IAmazonIdentityManagementService amazonIdentityManagementService;
        private readonly IMediator mediator;

        public EnsureAmazonGroupWithNameCommandHandler(
            IAmazonIdentityManagementService amazonIdentityManagementService,
            IMediator mediator)
        {
            this.amazonIdentityManagementService = amazonIdentityManagementService;
            this.mediator = mediator;
        }

        public async Task<Group> Handle(EnsureAmazonGroupWithNameCommand request, CancellationToken cancellationToken)
        {
            var group = await this.mediator.Send(
                new GetAmazonGroupByNameQuery(request.Name),
                cancellationToken);
            if (group != null)
                return group;

            var newGroupResponse = await this.amazonIdentityManagementService.CreateGroupAsync(
                new CreateGroupRequest(request.Name), 
                cancellationToken);
            return newGroupResponse.Group;
        }
    }
}

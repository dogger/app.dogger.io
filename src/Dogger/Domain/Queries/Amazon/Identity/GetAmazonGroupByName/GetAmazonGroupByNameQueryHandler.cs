using System.Threading;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Identity.GetAmazonGroupByName
{
    public class GetAmazonGroupByNameQueryHandler : IRequestHandler<GetAmazonGroupByNameQuery, Group?>
    {
        private readonly IAmazonIdentityManagementService amazonIdentityManagementService;

        public GetAmazonGroupByNameQueryHandler(
            IAmazonIdentityManagementService amazonIdentityManagementService)
        {
            this.amazonIdentityManagementService = amazonIdentityManagementService;
        }

        public async Task<Group?> Handle(GetAmazonGroupByNameQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var existingGroupResponse = await this.amazonIdentityManagementService.GetGroupAsync(
                    new GetGroupRequest(request.Name),
                    cancellationToken);
                var group = existingGroupResponse.Group;
                return @group;
            }
            catch (NoSuchEntityException)
            {
                return null;
            }
        }
    }
}

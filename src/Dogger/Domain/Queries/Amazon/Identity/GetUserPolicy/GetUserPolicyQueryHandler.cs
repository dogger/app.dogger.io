using System.Threading;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Identity.GetUserPolicy
{
    public class GetUserPolicyQueryHandler : IRequestHandler<GetUserPolicyQuery, GetUserPolicyResponse?>
    {
        private readonly IAmazonIdentityManagementService amazonIdentityManagementService;

        public GetUserPolicyQueryHandler(
            IAmazonIdentityManagementService amazonIdentityManagementService)
        {
            this.amazonIdentityManagementService = amazonIdentityManagementService;
        }

        public async Task<GetUserPolicyResponse?> Handle(GetUserPolicyQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var existingUserPolicy = await this.amazonIdentityManagementService.GetUserPolicyAsync(
                    new GetUserPolicyRequest(
                        request.UserName, 
                        request.PolicyName),
                    cancellationToken);
                return existingUserPolicy;
            }
            catch (NoSuchEntityException)
            {
                return null;
            }
        }
    }
}

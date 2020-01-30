using Amazon.IdentityManagement.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Identity.GetUserPolicy
{
    public class GetUserPolicyQuery : IRequest<GetUserPolicyResponse?>
    {
        public string UserName { get; }
        public string PolicyName { get; }

        public GetUserPolicyQuery(
            string userName,
            string policyName)
        {
            this.UserName = userName;
            this.PolicyName = policyName;
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Dogger.Infrastructure.Auth
{
    [ExcludeFromCodeCoverage]
    public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
    {
        public static bool HasScope(ClaimsPrincipal user, string scopeOrPermission)
        {
            return 
                HasClaimValuesForType(user, "scope", scopeOrPermission) ||
                HasClaimValuesForType(user, "permissions", scopeOrPermission);
        }

        private static bool HasClaimValuesForType(ClaimsPrincipal user, string type, string scope)
        {
            return user
               .Claims
               .Any(c => 
                    c.Type == type && 
                    c.Issuer == AuthConstants.Auth0Domain &&
                    c.Value?.Split(' ').Contains(scope) == true);
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
        {
            if (HasScope(context.User, requirement.Scope))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}

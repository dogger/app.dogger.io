using System;
using System.Security.Claims;

namespace Dogger.Tests.TestHelpers
{
    public class TestClaimsPrincipalFactory
    {
        public static ClaimsPrincipal CreateWithIdentityName(string identityName)
        {
            return new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.Name, identityName),
                        new Claim(ClaimTypes.Email, $"{identityName}@example.com") 
                    }));
        }
    }
}

//from https://auth0.com/docs/quickstart/backend/aspnet-core-webapi/01-authorization

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;

namespace Dogger.Infrastructure.Auth
{
    [ExcludeFromCodeCoverage]
    public class HasScopeRequirement : IAuthorizationRequirement
    {
        public string Scope { get; }

        public HasScopeRequirement(string scope)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }
    }
}

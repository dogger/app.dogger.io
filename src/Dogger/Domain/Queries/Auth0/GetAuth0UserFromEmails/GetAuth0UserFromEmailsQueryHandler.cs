using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using Dogger.Infrastructure.Auth.Auth0;
using Dogger.Infrastructure.Ioc;
using MediatR;

namespace Dogger.Domain.Queries.Auth0.GetAuth0UserFromEmails
{
    public class GetAuth0UserFromEmailsQueryHandler : IRequestHandler<GetAuth0UserFromEmailsQuery, User?>
    {
        private readonly IManagementApiClientFactory? managementApiClientFactory;

        public GetAuth0UserFromEmailsQueryHandler(
            IOptionalService<IManagementApiClientFactory> managementApiClientFactory)
        {
            this.managementApiClientFactory = managementApiClientFactory.Value;
        }

        public async Task<User?> Handle(GetAuth0UserFromEmailsQuery request, CancellationToken cancellationToken)
        {
            if (this.managementApiClientFactory == null)
                return null;

            if (request.Emails.Length == 0)
                throw new InvalidOperationException("No emails provided.");

            using var client = await managementApiClientFactory.CreateAsync();
            foreach (var email in request.Emails)
            {
                var users = await client.GetUsersByEmailAsync(email);
                var user = users.FirstOrDefault();
                if (user != null)
                    return user;
            }

            return null;
        }
    }
}

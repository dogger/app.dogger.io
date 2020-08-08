using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using Dogger.Infrastructure.Auth.Auth0;
using Dogger.Infrastructure.Ioc;
using MediatR;

namespace Dogger.Domain.Commands.Auth0.CreateAuth0User
{
    public class CreateAuth0UserCommandHandler : IRequestHandler<CreateAuth0UserCommand, User?>
    {
        private readonly IManagementApiClientFactory? managementApiClientFactory;
        private readonly IMediator mediator;

        public CreateAuth0UserCommandHandler(
            IOptionalService<IManagementApiClientFactory> managementApiClientFactory,
            IMediator mediator)
        {
            this.managementApiClientFactory = managementApiClientFactory.Value;
            this.mediator = mediator;
        }

        public async Task<User?> Handle(CreateAuth0UserCommand request, CancellationToken cancellationToken)
        {
            if (this.managementApiClientFactory == null)
                return null;

            if (request.Emails.Length == 0)
                throw new InvalidOperationException("No emails provided.");

            using var client = await managementApiClientFactory.CreateAsync();

            var createdUsers = new List<User>();
            foreach(var email in request.Emails) {
                var createdUser = await client.CreateUserAsync(new UserCreateRequest()
                {
                    Connection = "Username-Password-Authentication",
                    Email = email,
                    EmailVerified = true,
                    Password = Guid.NewGuid().ToString()
                });
                if (createdUsers.Count > 0)
                {
                    var firstUser = createdUsers.First();
                    try
                    {
                        await client.LinkUserAccountAsync(firstUser.UserId, new UserAccountLinkRequest()
                        {
                            UserId = createdUser.UserId,
                            Provider = "auth0"
                        });
                    }
                    catch
                    {
                        await client.DeleteUserAsync(createdUser.UserId);
                        throw;
                    }
                }

                createdUsers.Add(createdUser);
            }

            return createdUsers.First();
        }
    }
}

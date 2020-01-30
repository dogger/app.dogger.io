using Auth0.ManagementApi.Models;
using Destructurama.Attributed;
using MediatR;

namespace Dogger.Domain.Commands.Auth0.CreateAuth0User
{

    public class CreateAuth0UserCommand : IRequest<User>
    {
        [NotLogged]
        public string[] Emails { get; }

        public CreateAuth0UserCommand(
            string[] emails)
        {
            this.Emails = emails;
        }
    }
}

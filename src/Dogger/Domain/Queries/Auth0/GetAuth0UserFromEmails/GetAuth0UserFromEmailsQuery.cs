using Auth0.ManagementApi.Models;
using Destructurama.Attributed;
using MediatR;

namespace Dogger.Domain.Queries.Auth0.GetAuth0UserFromEmails
{
    public class GetAuth0UserFromEmailsQuery : IRequest<User?>
    {
        [NotLogged]
        public string[] Emails { get; }

        public GetAuth0UserFromEmailsQuery(
            string[] emails)
        {
            this.Emails = emails;
        }
    }
}

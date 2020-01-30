using Amazon.IdentityManagement.Model;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.Identity.EnsureAmazonGroupWithName
{
    public class EnsureAmazonGroupWithNameCommand : IRequest<Group>
    {
        public string Name { get; }

        public EnsureAmazonGroupWithNameCommand(
            string name)
        {
            this.Name = name;
        }
    }
}

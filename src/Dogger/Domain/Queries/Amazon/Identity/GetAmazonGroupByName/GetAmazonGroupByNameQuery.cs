using Amazon.IdentityManagement.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Identity.GetAmazonGroupByName
{
    public class GetAmazonGroupByNameQuery : IRequest<Group?>
    {
        public string Name { get; }

        public GetAmazonGroupByNameQuery(
            string name)
        {
            this.Name = name;
        }
    }
}

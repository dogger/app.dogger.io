using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Identity.GetAmazonUserByName
{
    public class GetAmazonUserByNameQuery : IRequest<AmazonUser>
    {
        public string Name { get; }

        public GetAmazonUserByNameQuery(
            string name)
        {
            this.Name = name;
        }
    }
}

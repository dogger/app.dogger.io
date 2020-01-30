using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryLoginByRepositoryName
{
    public class GetRepositoryLoginForUserQuery : IRequest<RepositoryLoginResponse>
    {
        public AmazonUser AmazonUser { get; }

        public GetRepositoryLoginForUserQuery(AmazonUser amazonUser)
        {
            this.AmazonUser = amazonUser;
        }
    }
}

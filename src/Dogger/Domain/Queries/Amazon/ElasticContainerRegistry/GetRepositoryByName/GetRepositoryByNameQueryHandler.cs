using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ECR;
using Amazon.ECR.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryByName
{
    public class GetRepositoryByNameQueryHandler : IRequestHandler<GetRepositoryByNameQuery, Repository?>
    {
        private readonly IAmazonECR amazonEcr;

        public GetRepositoryByNameQueryHandler(
            IAmazonECR amazonEcr)
        {
            this.amazonEcr = amazonEcr;
        }

        public async Task<Repository?> Handle(GetRepositoryByNameQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var repositoriesResponse = await this.amazonEcr.DescribeRepositoriesAsync(new DescribeRepositoriesRequest()
                {
                    RepositoryNames = new List<string>()
                    {
                        request.Name
                    }
                }, cancellationToken);

                var repository = repositoriesResponse
                    .Repositories
                    .SingleOrDefault();
                return repository;
            }
            catch (RepositoryNotFoundException)
            {
                return null;
            }
        }
    }
}

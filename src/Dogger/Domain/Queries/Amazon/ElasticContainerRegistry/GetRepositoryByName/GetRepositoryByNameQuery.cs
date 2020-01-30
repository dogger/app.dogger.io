using Amazon.ECR.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryByName
{
    public class GetRepositoryByNameQuery : IRequest<Repository?>
    {
        public string Name { get; }

        public GetRepositoryByNameQuery(
            string name)
        {
            this.Name = name;
        }
    }
}

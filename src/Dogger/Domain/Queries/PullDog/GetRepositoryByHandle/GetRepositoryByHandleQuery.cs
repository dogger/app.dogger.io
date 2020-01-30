using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.PullDog.GetRepositoryByHandle
{
    public class GetRepositoryByHandleQuery : IRequest<PullDogRepository?>
    {
        public string RepositoryHandle { get; }

        public GetRepositoryByHandleQuery(
            string repositoryHandle)
        {
            this.RepositoryHandle = repositoryHandle;
        }
    }
}

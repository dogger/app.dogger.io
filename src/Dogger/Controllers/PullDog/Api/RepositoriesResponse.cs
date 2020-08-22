namespace Dogger.Controllers.PullDog.Api
{
    public class RepositoriesResponse
    {
        public RepositoriesResponse(
            RepositoryResponse[] repositories)
        {
            this.Repositories = repositories;
        }

        public RepositoryResponse[] Repositories { get; }
    }
}

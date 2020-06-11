namespace Dogger.Domain.Services.PullDog
{
    public class PullRequestDetails
    {
        public string RepositoryFullName { get; }
        public string PullRequestCommentReference { get; }

        public PullRequestDetails(
            string repositoryFullName,
            string pullRequestCommentReference)
        {
            this.RepositoryFullName = repositoryFullName;
            this.PullRequestCommentReference = pullRequestCommentReference;
        }
    }
}

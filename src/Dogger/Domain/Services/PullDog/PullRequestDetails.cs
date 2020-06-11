namespace Dogger.Domain.Services.PullDog
{
    public class PullRequestDetails
    {
        public string RepositoryFullName { get; }

        public string IndirectPullRequestCommentReference { get; }
        public string DirectPullRequestCommentReference { get; }

        public PullRequestDetails(
            string repositoryFullName,
            string indirectPullRequestCommentReference, 
            string directPullRequestCommentReference)
        {
            this.RepositoryFullName = repositoryFullName;
            this.IndirectPullRequestCommentReference = indirectPullRequestCommentReference;
            this.DirectPullRequestCommentReference = directPullRequestCommentReference;
        }
    }
}

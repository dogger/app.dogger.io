namespace Dogger.Domain.Services.PullDog
{
    public class PullRequestDetails
    {
        public string PullRequestCommentReference { get; }

        public PullRequestDetails(
            string pullRequestCommentReference)
        {
            this.PullRequestCommentReference = pullRequestCommentReference;
        }
    }
}

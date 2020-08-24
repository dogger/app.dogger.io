using Dogger.Domain.Models.Builders;
using Octokit;

namespace Dogger.Infrastructure.GitHub.Octokit
{
    public class IssueBuilder : ModelBuilder<Issue>
    {
        private PullRequest? pullRequest;

        public IssueBuilder WithPullRequest(PullRequest pullRequest)
        {
            this.pullRequest = pullRequest;
            return this;
        }

        public override Issue Build()
        {
            return new Issue(
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                pullRequest,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default);
        }
    }
}

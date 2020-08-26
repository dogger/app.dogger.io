using Dogger.Domain.Models.Builders;
using Octokit;

namespace Dogger.Infrastructure.GitHub.Octokit
{
    public class PullRequestBuilder : ModelBuilder<PullRequest>
    {
        private int number;

        private ItemState state;

        private GitReference? head;
        private GitReference? @base;

        private User? user;

        public PullRequestBuilder WithNumber(int number)
        {
            this.number = number;
            return this;
        }

        public PullRequestBuilder WithState(ItemState state)
        {
            this.state = state;
            return this;
        }

        public PullRequestBuilder WithUser(User user)
        {
            this.user = user;
            return this;
        }

        public PullRequestBuilder WithHead(GitReference head)
        {
            this.head = head;
            return this;
        }

        public PullRequestBuilder WithBase(GitReference @base)
        {
            this.@base = @base;
            return this;
        }

        public override PullRequest Build()
        {
            return new PullRequest(
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                number,
                state,
                default,
                default,
                default,
                default,
                default,
                default,
                head,
                @base,
                user,
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
                default,
                default,
                default);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Dogger.Infrastructure.GitHub.Octokit
{
    public class OctokitPullRequestBuilder
    {
        private int number;

        private ItemState state;

        private GitReference? head;
        private GitReference? @base;

        private User? user;

        public OctokitPullRequestBuilder WithNumber(int number)
        {
            this.number = number;
            return this;
        }

        public OctokitPullRequestBuilder WithState(ItemState state)
        {
            this.state = state;
            return this;
        }

        public OctokitPullRequestBuilder WithUser(User user)
        {
            this.user = user;
            return this;
        }

        public OctokitPullRequestBuilder WithHead(GitReference head)
        {
            this.head = head;
            return this;
        }

        public OctokitPullRequestBuilder WithBase(GitReference @base)
        {
            this.@base = @base;
            return this;
        }

        public PullRequest Build()
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

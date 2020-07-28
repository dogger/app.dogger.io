using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace Dogger.Infrastructure.GitHub.Octokit
{
    public class IssueBuilder
    {
        private PullRequest? pullRequest;

        public IssueBuilder WithPullRequest(PullRequest pullRequest)
        {
            this.pullRequest = pullRequest;
            return this;
        }

        public Issue Build()
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

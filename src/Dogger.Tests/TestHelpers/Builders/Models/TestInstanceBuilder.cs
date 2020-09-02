using System;
using Dogger.Domain.Models.Builders;

namespace Dogger.Tests.TestHelpers.Builders.Models
{
    public class TestInstanceBuilder : InstanceBuilder
    {
        public TestInstanceBuilder()
        {
            WithId(Guid.NewGuid());
            WithName(Guid.NewGuid().ToString());
            WithPlanId(Guid.NewGuid().ToString());
            WithCluster();
        }

        public TestInstanceBuilder WithCluster()
        {
            WithCluster(new TestClusterBuilder().Build());
            return this;
        }

        public TestInstanceBuilder WithPullDogPullRequest()
        {
            WithPullDogPullRequest(new TestPullDogPullRequestBuilder().Build());
            return this;
        }
    }
}
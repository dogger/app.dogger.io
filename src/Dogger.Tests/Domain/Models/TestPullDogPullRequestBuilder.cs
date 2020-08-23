using System;
using Dogger.Domain.Models.Builders;
using Bogus;

namespace Dogger.Tests.Domain.Models
{
    public class TestPullDogPullRequestBuilder : PullDogPullRequestBuilder
    {
        public TestPullDogPullRequestBuilder()
        {
            WithId(Guid.NewGuid());
            WithHandle(Guid.NewGuid().ToString());
            WithPullDogRepository();
        }

        public TestPullDogPullRequestBuilder WithPullDogRepository()
        {
            WithPullDogRepository(new TestPullDogRepositoryBuilder().Build());
            return this;
        }
    }

}

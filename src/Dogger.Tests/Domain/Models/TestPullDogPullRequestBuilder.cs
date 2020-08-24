using System;
using Dogger.Domain.Models.Builders;

namespace Dogger.Tests.Domain.Models
{
    public class TestPullDogPullRequestBuilder : PullDogPullRequestBuilder
    {
        public TestPullDogPullRequestBuilder()
        {
            WithId(Guid.NewGuid());
            WithPullDogRepository();

            var random = new Random();
            WithHandle(random.Next().ToString());
        }

        public TestPullDogPullRequestBuilder WithPullDogRepository()
        {
            WithPullDogRepository(new TestPullDogRepositoryBuilder().Build());
            return this;
        }
    }

}

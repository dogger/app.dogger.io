using System;
using Dogger.Domain.Models.Builders;

namespace Dogger.Tests.TestHelpers.Builders.Models
{
    public class TestPullDogRepositoryBuilder : PullDogRepositoryBuilder
    {
        public TestPullDogRepositoryBuilder()
        {
            WithId(Guid.NewGuid());
            WithPullDogSettings();

            var random = new Random();
            WithHandle(random.Next().ToString());
        }

        public PullDogRepositoryBuilder WithPullDogSettings()
        {
            WithPullDogSettings(new TestPullDogSettingsBuilder().Build());
            return this;
        }
    }

}

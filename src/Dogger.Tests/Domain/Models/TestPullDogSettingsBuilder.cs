using System;
using Dogger.Domain.Models.Builders;
using Bogus;

namespace Dogger.Tests.Domain.Models
{
    public class TestPullDogSettingsBuilder : PullDogSettingsBuilder
    {
        public TestPullDogSettingsBuilder()
        {
            WithId(Guid.NewGuid());
            WithUser();
            WithPlanId(Guid.NewGuid().ToString());
            WithEncryptedApiKey(Array.Empty<byte>());
        }

        public TestPullDogSettingsBuilder WithUser()
        {
            WithUser(new TestUserBuilder().Build());
            return this;
        }
    }

}

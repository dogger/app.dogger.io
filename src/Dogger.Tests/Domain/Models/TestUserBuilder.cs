using System;
using System.Collections.Generic;
using System.Text;
using Dogger.Domain.Models.Builders;

namespace Dogger.Tests.Domain.Models
{
    public class TestUserBuilder : UserBuilder
    {
        public TestUserBuilder()
        {
            WithId(Guid.NewGuid());
        }

        public TestUserBuilder WithPullDogSettings()
        {
            WithPullDogSettings(new TestPullDogSettingsBuilder().Build());
            return this;
        }
    }
}

﻿using System;
using Dogger.Domain.Models.Builders;

namespace Dogger.Tests.TestHelpers.Builders.Models
{
    public class TestUserBuilder : UserBuilder
    {
        public TestUserBuilder()
        {
            WithId(Guid.NewGuid());
            WithStripeCustomerId(Guid.NewGuid().ToString());
        }

        public TestUserBuilder WithPullDogSettings()
        {
            WithPullDogSettings(new TestPullDogSettingsBuilder().Build());
            return this;
        }
    }
}

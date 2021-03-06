﻿using System;
using Dogger.Domain.Models.Builders;

namespace Dogger.Tests.TestHelpers.Builders.Models
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

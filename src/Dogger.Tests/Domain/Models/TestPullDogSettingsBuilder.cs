﻿using System;
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
        }

        public TestPullDogSettingsBuilder WithUser()
        {
            WithUser(new TestUserBuilder().Build());
            return this;
        }
    }

}

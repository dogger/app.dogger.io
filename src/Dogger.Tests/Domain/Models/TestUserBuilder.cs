using System;
using System.Collections.Generic;
using System.Text;
using Dogger.Domain.Models.Builders;

namespace Dogger.Tests.Domain.Models
{
    class TestUserBuilder : UserBuilder
    {
        public TestUserBuilder()
        {
            WithId(Guid.NewGuid());
        }
    }
}

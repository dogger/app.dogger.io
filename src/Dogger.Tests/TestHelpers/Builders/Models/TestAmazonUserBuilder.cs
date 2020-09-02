using System;
using Dogger.Domain.Models.Builders;

namespace Dogger.Tests.TestHelpers.Builders.Models
{
    public class TestAmazonUserBuilder : AmazonUserBuilder
    {
        public TestAmazonUserBuilder()
        {
            WithId(Guid.NewGuid());
            WithName(Guid.NewGuid().ToString());
            WithAwsCredentials(
                Array.Empty<byte>(),
                Array.Empty<byte>());
        }

        public TestAmazonUserBuilder WithUser()
        {
            WithUser(new TestUserBuilder().Build());
            return this;
        }
    }

}

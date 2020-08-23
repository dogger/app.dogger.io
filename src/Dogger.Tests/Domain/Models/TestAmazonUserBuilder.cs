using System;
using Dogger.Domain.Models.Builders;
using Bogus;

namespace Dogger.Tests.Domain.Models
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
    }

}

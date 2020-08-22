using System;
using Dogger.Domain.Models.Builders;

namespace Dogger.Tests.Domain.Models
{
    public class TestAmazonUserBuilder : AmazonUserBuilder
    {
        public TestAmazonUserBuilder()
        {
            WithName("dummy");
            WithAwsCredentials(
                Array.Empty<byte>(),
                Array.Empty<byte>());
        }
    }

}

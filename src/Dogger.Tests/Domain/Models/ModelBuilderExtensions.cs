// ReSharper disable CheckNamespace

using System;

namespace Dogger.Domain.Models.Builders
{
    public static class ModelBuilderExtensions
    {
        public static AmazonUserBuilder WithDummyData(
            this AmazonUserBuilder amazonUserBuilder)
        {
            return amazonUserBuilder
                .WithName("dummy")
                .WithAwsCredentials(
                    Array.Empty<byte>(),
                    Array.Empty<byte>());
        }
    }
}

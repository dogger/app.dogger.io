using System;

namespace Dogger.Domain.Models.Builders
{
    public interface IAmazonUserBuilder : IModelBuilder<AmazonUser>
    {
        AmazonUserBuilder WithId(Guid value);
        AmazonUserBuilder WithName(string value);

        AmazonUserBuilder WithAwsCredentials(
            byte[] encryptedAccessKeyId,
            byte[] encryptedSecretAccessKey);

        AmazonUserBuilder WithUser(User user);
    }
}

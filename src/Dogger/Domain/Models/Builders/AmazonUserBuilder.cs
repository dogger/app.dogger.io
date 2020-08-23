using System;

namespace Dogger.Domain.Models.Builders
{
    public class AmazonUserBuilder : IModelBuilder<AmazonUser>
    {
        private Guid id;
        private string? name;

        private byte[]? encryptedAccessKeyId;
        private byte[]? encryptedSecretAccessKey;

        private EntityReference<User>? user;

        public AmazonUserBuilder WithId(Guid value)
        {
            this.id = value;
            return this;
        }

        public AmazonUserBuilder WithName(string value)
        {
            this.name = value;
            return this;
        }

        public AmazonUserBuilder WithAwsCredentials(
            byte[] encryptedAccessKeyId,
            byte[] encryptedSecretAccessKey)
        {
            this.encryptedAccessKeyId = encryptedAccessKeyId;
            this.encryptedSecretAccessKey = encryptedSecretAccessKey;
            return this;
        }

        public AmazonUserBuilder WithUser(User? user)
        {
            if (user == null)
            {
                this.user = null;
                return this;
            }

            this.user = new EntityReference<User>(user);
            return this;
        }

        public AmazonUserBuilder WithUser(Guid userId)
        {
            this.user = new EntityReference<User>(userId);
            return this;
        }

        public AmazonUser Build()
        {
            if (this.encryptedSecretAccessKey == null || this.encryptedAccessKeyId == null)
                throw new InvalidOperationException("AWS credentials are required.");

            if (this.name == null)
                throw new InvalidOperationException("Name is required.");

            return new AmazonUser()
            {
                Id = this.id,
                EncryptedAccessKeyId = this.encryptedAccessKeyId,
                EncryptedSecretAccessKey = this.encryptedSecretAccessKey,
                Name = this.name,
                User = this.user?.Reference,
                UserId = this.user?.Id
            };
        }
    }
}

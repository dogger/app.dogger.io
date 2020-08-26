using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Dogger.Domain.Queries.Amazon.Identity.GetAmazonUserByName;
using Dogger.Infrastructure.Encryption;
using MediatR;

namespace Dogger.Domain.Services.Amazon.Identity
{
    public abstract class UserAuthenticatedServiceFactory<T> : IUserAuthenticatedServiceFactory<T> where T : IAmazonService
    {
        private readonly IMediator mediator;
        private readonly IAesEncryptionHelper aesEncryptionHelper;

        protected UserAuthenticatedServiceFactory(
            IMediator mediator,
            IAesEncryptionHelper aesEncryptionHelper)
        {
            this.mediator = mediator;
            this.aesEncryptionHelper = aesEncryptionHelper;
        }

        public async Task<T> CreateAsync(string amazonUserName)
        {
            var user = await this.mediator.Send(new GetAmazonUserByNameQuery(amazonUserName));
            if (user == null)
                throw new InvalidOperationException("Could not find Amazon user.");

            return OnCreate(
                new BasicAWSCredentials(
                    await this.aesEncryptionHelper.DecryptAsync(user.EncryptedAccessKeyId),
                    await this.aesEncryptionHelper.DecryptAsync(user.EncryptedSecretAccessKey)),
                RegionEndpoint.EUWest1);
        }

        protected abstract T OnCreate(AWSCredentials credentials, RegionEndpoint region);
    }
}

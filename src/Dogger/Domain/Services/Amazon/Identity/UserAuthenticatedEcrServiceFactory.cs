using Amazon;
using Amazon.ECR;
using Amazon.Runtime;
using Dogger.Infrastructure.Encryption;
using MediatR;

namespace Dogger.Domain.Services.Amazon.Identity
{
    public class UserAuthenticatedEcrServiceFactory : UserAuthenticatedServiceFactory<IAmazonECR>
    {
        public UserAuthenticatedEcrServiceFactory(
            IMediator mediator, 
            IAesEncryptionHelper aesEncryptionHelper) : base(mediator, aesEncryptionHelper)
        {
        }

        protected override IAmazonECR OnCreate(AWSCredentials credentials, RegionEndpoint region)
        {
            return new AmazonECRClient(
                credentials,
                region);
        }
    }

}

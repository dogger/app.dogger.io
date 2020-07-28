using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ECR;
using Amazon.ECR.Model;
using Dogger.Domain.Services.Amazon.Identity;
using MediatR;
using Polly;
using Polly.Retry;

namespace Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryLoginByRepositoryName
{

    public class GetRepositoryLoginByRepositoryNameQueryHandler : IRequestHandler<GetRepositoryLoginForUserQuery, RepositoryLoginResponse>
    {
        private readonly IUserAuthenticatedServiceFactory<IAmazonECR> userAuthenticatedServiceFactory;

        public GetRepositoryLoginByRepositoryNameQueryHandler(
            IUserAuthenticatedServiceFactory<IAmazonECR> userAuthenticatedServiceFactory)
        {
            this.userAuthenticatedServiceFactory = userAuthenticatedServiceFactory;
        }

        public async Task<RepositoryLoginResponse> Handle(GetRepositoryLoginForUserQuery request, CancellationToken cancellationToken)
        {
            var amazonEcrClient = await this.userAuthenticatedServiceFactory.CreateAsync(request.AmazonUser.Name);

            var policy = GetAuthenticationTokenReadinessRetryPolicy();

            var authorizationToken = await policy.ExecuteAsync(async () => 
                await amazonEcrClient.GetAuthorizationTokenAsync(
                    new GetAuthorizationTokenRequest(), 
                    cancellationToken));

            var token = authorizationToken
                .AuthorizationData
                .Single()
                .AuthorizationToken;

            var decodedString = Encoding.UTF8.GetString(
                Convert.FromBase64String(token));

            var credentials = decodedString.Split(':', 2);
            return new RepositoryLoginResponse(
                username: credentials[0],
                password: credentials[1]);
        }

        /// <summary>
        /// This policy is made because IAM credentials created are not working right after creation, so we allow to retry a couple of times until the credentials are valid.
        /// </summary>
        private static AsyncRetryPolicy GetAuthenticationTokenReadinessRetryPolicy()
        {
            return Policy
                .Handle<AmazonECRException>(exception =>
                    exception.Message == "The security token included in the request is invalid.")
                .WaitAndRetryAsync(10, _ => TimeSpan.FromSeconds(1));
        }
    }
}

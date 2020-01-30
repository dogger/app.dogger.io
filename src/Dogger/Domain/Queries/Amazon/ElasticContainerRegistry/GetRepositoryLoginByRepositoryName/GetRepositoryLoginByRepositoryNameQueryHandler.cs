using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ECR;
using Amazon.ECR.Model;
using Dogger.Domain.Services.Amazon.Identity;
using MediatR;

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
            var authorizationToken = await amazonEcrClient.GetAuthorizationTokenAsync(
                new GetAuthorizationTokenRequest(), 
                cancellationToken);

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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterForUser;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Models;
using Dogger.Domain.Services.PullDog;
using MediatR;

namespace Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest
{
    public class GetAvailableClusterFromPullRequestQueryHandler : IRequestHandler<GetAvailableClusterFromPullRequestQuery, Cluster>
    {
        private readonly IMediator mediator;
        private readonly IPullDogRepositoryClientFactory pullDogRepositoryClientFactory;

        public GetAvailableClusterFromPullRequestQueryHandler(
            IMediator mediator,
            IPullDogRepositoryClientFactory pullDogRepositoryClientFactory)
        {
            this.mediator = mediator;
            this.pullDogRepositoryClientFactory = pullDogRepositoryClientFactory;
        }

        public async Task<Cluster> Handle(GetAvailableClusterFromPullRequestQuery request, CancellationToken cancellationToken)
        {
            var pullRequest = request.PullRequest;
            if (pullRequest == null)
                throw new InvalidOperationException("Origin pull request was null.");

            var settings = pullRequest.PullDogRepository.PullDogSettings;
            var user = settings.User;

            if (settings.PoolSize == 0)
            {
                return await GetAvailableDemoClusterAsync(
                    user, 
                    pullRequest, 
                    cancellationToken);
            }

            return await GetAvailableCustomerClusterAsync(
                user, 
                pullRequest, 
                cancellationToken);
        }

        private async Task<Cluster> GetAvailableCustomerClusterAsync(User user, PullDogPullRequest pullRequest, CancellationToken cancellationToken)
        {
            var cluster = await this.mediator.Send(new EnsureClusterForUserCommand(user.Id)
            {
                ClusterName = "pull-dog"
            }, cancellationToken);
            var offendingPullRequests = GetPullRequestsFromCluster(cluster);
            if (HasExceededPoolSize(pullRequest, offendingPullRequests))
            {
                var details = await GetPullRequestDetailsFromPullRequestsAsync(offendingPullRequests);
                throw new PullDogPoolSizeExceededException(details);
            }

            return cluster;
        }

        private async Task<Cluster> GetAvailableDemoClusterAsync(User user, PullDogPullRequest pullRequest, CancellationToken cancellationToken)
        {
            var cluster = await this.mediator.Send(
                new EnsureClusterWithIdCommand(DataContext.PullDogDemoClusterId),
                cancellationToken);
            var offendingPullRequests = GetPullRequestsFromCluster(cluster);
            if (HasExceededPoolSize(pullRequest, offendingPullRequests))
            {
                var details = await GetPullRequestDetailsFromPullRequestsAsync(offendingPullRequests);
                throw new PullDogDemoInstanceAlreadyProvisionedException(details.Last());
            }

            cluster.User = user;

            return cluster;
        }

        private static bool HasExceededPoolSize(
            PullDogPullRequest originPullRequest, 
            IEnumerable<PullDogPullRequest> offendingPullRequests)
        {
            var offendingPullRequestCount = offendingPullRequests.Count(pr => pr.Handle != originPullRequest.Handle);

            var settings = originPullRequest.PullDogRepository.PullDogSettings;
            return offendingPullRequestCount > 0 && offendingPullRequestCount >= settings.PoolSize;
        }

        private static PullDogPullRequest[] GetPullRequestsFromCluster(Cluster cluster)
        {
            var offendingPullRequests = cluster
                .Instances
                .Select(x => x.PullDogPullRequest!)
                .Where(x => x != null)
                .ToArray();
            return offendingPullRequests;
        }

        private async Task<PullRequestDetails[]> GetPullRequestDetailsFromPullRequestsAsync(
            IEnumerable<PullDogPullRequest> offendingPullRequests)
        {
            var groupedPullRequestDetails = await Task.WhenAll(offendingPullRequests
                .GroupBy(pullRequest => pullRequest.PullDogRepositoryId)
                .Select(async group =>
                {
                    var client = await this.pullDogRepositoryClientFactory.CreateAsync(group.First());
                    return group.Select(client.GetPullRequestDetails);
                }));
            return groupedPullRequestDetails
                .SelectMany(x => x)
                .ToArray();
        }
    }
}

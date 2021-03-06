﻿using System;
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
                var cluster = await mediator.Send(
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
            else
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
                .Where(x => x.PullDogPullRequest != null)
                .Select(x => x.PullDogPullRequest!)
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
                    if (client == null)
                        return null!;
                    
                    return group.Select(client.GetPullRequestDetails);
                })
                .Where(x => x != null));
            return groupedPullRequestDetails
                .SelectMany(x => x)
                .ToArray();
        }
    }
}

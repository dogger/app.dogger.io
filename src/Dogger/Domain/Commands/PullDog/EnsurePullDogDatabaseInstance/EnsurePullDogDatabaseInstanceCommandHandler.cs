using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace Dogger.Domain.Commands.PullDog.EnsurePullDogDatabaseInstance
{
    public class EnsurePullDogDatabaseInstanceCommandHandler : IRequestHandler<EnsurePullDogDatabaseInstanceCommand, Instance>
    {
        private readonly IMediator mediator;
        private readonly IHostEnvironment hostEnvironment;

        private readonly DataContext dataContext;

        public EnsurePullDogDatabaseInstanceCommandHandler(
            IMediator mediator,
            IHostEnvironment hostEnvironment,
            DataContext dataContext)
        {
            this.mediator = mediator;
            this.hostEnvironment = hostEnvironment;
            this.dataContext = dataContext;
        }

        public async Task<Instance> Handle(EnsurePullDogDatabaseInstanceCommand request, CancellationToken cancellationToken)
        {
            var pullRequest = request.PullRequest;

            var cluster = await this.mediator.Send(
                new GetAvailableClusterFromPullRequestQuery(pullRequest),
                cancellationToken);

            var settings = pullRequest.PullDogRepository.PullDogSettings;
            var user = settings.User;

            var expiryDuration = request.Configuration.Expiry;

            var isDemoPlan = settings.PoolSize == 0;
            var maximumDemoExpiryTime = TimeSpan.FromMinutes(55);
            if (isDemoPlan && (expiryDuration > maximumDemoExpiryTime || expiryDuration.TotalMinutes < 1))
            {
                expiryDuration = maximumDemoExpiryTime;
            }

            var expiryTime = expiryDuration.TotalMinutes < 1 ?
                (DateTime?)null :
                DateTime.UtcNow.Add(expiryDuration);

            try
            {
                var existingInstance = cluster
                    .Instances
                    .SingleOrDefault(x =>
                        x.PullDogPullRequest == pullRequest);
                if (existingInstance != null)
                {
                    if (!isDemoPlan || existingInstance.ExpiresAtUtc == null)
                    {
                        existingInstance.ExpiresAtUtc = expiryTime;
                    }

                    return existingInstance;
                }

                var newInstance = new Instance()
                {
                    Name = $"{hostEnvironment.EnvironmentName}_pull-dog_{user.Id}_{request.PullRequest.Id}",
                    Cluster = cluster,
                    IsProvisioned = false,
                    PlanId = settings.PlanId,
                    PullDogPullRequest = pullRequest,
                    ExpiresAtUtc = expiryTime
                };
                pullRequest.Instance = newInstance;

                cluster.Instances.Add(newInstance);
                await this.dataContext.Instances.AddAsync(newInstance, cancellationToken);

                return newInstance;
            }
            finally
            {
                await this.dataContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

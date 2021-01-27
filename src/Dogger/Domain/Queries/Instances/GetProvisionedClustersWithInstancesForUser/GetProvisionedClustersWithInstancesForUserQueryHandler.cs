using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.Instances.GetProvisionedClustersWithInstancesForUser
{
    public class GetProvisionedClustersWithInstancesForUserQueryHandler : IRequestHandler<GetProvisionedClustersWithInstancesForUserQuery, IReadOnlyList<UserClusterResponse>>
    {
        private readonly DataContext dataContext;
        private readonly IMediator mediator;

        public GetProvisionedClustersWithInstancesForUserQueryHandler(
            DataContext dataContext,
            IMediator mediator)
        {
            this.dataContext = dataContext;
            this.mediator = mediator;
        }

        public async Task<IReadOnlyList<UserClusterResponse>> Handle(GetProvisionedClustersWithInstancesForUserQuery request, CancellationToken cancellationToken)
        {
            var clusters = await this.dataContext.Clusters
                .Include(x => x.Instances)
                .Where(x => 
                    x.UserId == request.UserId &&
                    x.Instances.Any(i => i.IsProvisioned != null))
                .ToArrayAsync(cancellationToken);
            var instances = await Task.WhenAll(clusters
                .Select(async cluster => new UserClusterResponse(
                    cluster.Id,
                    await Task.WhenAll(cluster
                        .Instances
                        .Select(async instance => new UserClusterInstanceResponse(
                            amazonModel: 
                                await this.mediator.Send(
                                    new GetLightsailInstanceByNameQuery(instance.Name),
                                    cancellationToken) ??
                                throw new InvalidOperationException("Could not find Lightsail instance."),
                            databaseModel: instance))))));
            return instances.ToArray();
        }
    }

}

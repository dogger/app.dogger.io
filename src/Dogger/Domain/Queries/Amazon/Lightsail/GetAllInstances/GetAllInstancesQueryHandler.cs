using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Lightsail.GetAllInstances
{
    public class GetAllInstancesQueryHandler : IRequestHandler<GetAllInstancesQuery, IReadOnlyCollection<Instance>>
    {
        private readonly IAmazonLightsail lightsailClient;

        public GetAllInstancesQueryHandler(
            IAmazonLightsail lightsailClient)
        {
            this.lightsailClient = lightsailClient;
        }

        public async Task<IReadOnlyCollection<Instance>> Handle(GetAllInstancesQuery request, CancellationToken cancellationToken)
        {
            var response = await this.lightsailClient.GetInstancesAsync(
                new GetInstancesRequest(), 
                cancellationToken);

            return response.Instances;
        }
    }
}

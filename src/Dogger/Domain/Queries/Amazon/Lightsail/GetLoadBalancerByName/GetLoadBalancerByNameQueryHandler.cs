using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Lightsail.GetLoadBalancerByName
{
    public class GetLoadBalancerByNameQueryHandler : IRequestHandler<GetLoadBalancerByNameQuery, LoadBalancer?>
    {
        private readonly IAmazonLightsail lightsailClient;

        public GetLoadBalancerByNameQueryHandler(
            IAmazonLightsail lightsailClient)
        {
            this.lightsailClient = lightsailClient;
        }

        public async Task<LoadBalancer?> Handle(GetLoadBalancerByNameQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.lightsailClient.GetLoadBalancerAsync(new GetLoadBalancerRequest()
                {
                    LoadBalancerName = request.Name
                }, cancellationToken);
                return response.LoadBalancer;
            }
            catch (NotFoundException)
            {
                return null;
            }
        }
    }
}

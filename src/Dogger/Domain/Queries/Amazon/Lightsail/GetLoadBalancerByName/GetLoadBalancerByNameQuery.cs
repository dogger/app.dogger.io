using Amazon.Lightsail.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Lightsail.GetLoadBalancerByName
{
    public class GetLoadBalancerByNameQuery : IRequest<LoadBalancer?>
    {
        public string Name { get; }

        public GetLoadBalancerByNameQuery(
            string name)
        {
            this.Name = name;
        }
    }
}

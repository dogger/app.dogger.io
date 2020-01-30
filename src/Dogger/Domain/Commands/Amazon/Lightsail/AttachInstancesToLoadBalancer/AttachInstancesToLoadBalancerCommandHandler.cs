using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Services.Amazon.Lightsail;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.Lightsail.AttachInstancesToLoadBalancer
{
    public class AttachInstancesToLoadBalancerCommandHandler : IRequestHandler<AttachInstancesToLoadBalancerCommand>
    {
        private readonly IAmazonLightsail lightsailClient;
        private readonly ILightsailOperationService lightsailOperationService;

        public AttachInstancesToLoadBalancerCommandHandler(
            IAmazonLightsail lightsailClient,
            ILightsailOperationService lightsailOperationService)
        {
            this.lightsailClient = lightsailClient;
            this.lightsailOperationService = lightsailOperationService;
        }

        public async Task<Unit> Handle(AttachInstancesToLoadBalancerCommand request, CancellationToken cancellationToken)
        {
            var response = await this.lightsailClient.AttachInstancesToLoadBalancerAsync(new AttachInstancesToLoadBalancerRequest()
            {
                LoadBalancerName = request.LoadBalancerName,
                InstanceNames = request.InstanceNames.ToList()
            }, cancellationToken);

            await this.lightsailOperationService.WaitForOperationsAsync(response.Operations);

            return Unit.Value;
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Services.Amazon.Lightsail;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.Lightsail.AssignStaticIpToInstance
{
    public class AssignStaticIpToInstanceCommandHandler : IRequestHandler<AssignStaticIpToInstanceCommand>
    {
        private readonly IAmazonLightsail lightsailClient;
        private readonly ILightsailOperationService lightsailOperationService;

        public AssignStaticIpToInstanceCommandHandler(
            IAmazonLightsail lightsailClient,
            ILightsailOperationService lightsailOperationService)
        {
            this.lightsailClient = lightsailClient;
            this.lightsailOperationService = lightsailOperationService;
        }

        public async Task<Unit> Handle(
            AssignStaticIpToInstanceCommand request, 
            CancellationToken cancellationToken)
        {
            var response = await this.lightsailClient.AttachStaticIpAsync(new AttachStaticIpRequest()
            {
                StaticIpName = request.StaticIpName,
                InstanceName = request.InstanceName
            }, cancellationToken);

            await this.lightsailOperationService.WaitForOperationsAsync(response.Operations);

            return Unit.Value;
        }
    }
}

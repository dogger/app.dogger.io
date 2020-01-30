using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Services.Amazon.Lightsail;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.Lightsail.CreateStaticIp
{
    public class CreateStaticIpCommandHandler : IRequestHandler<CreateStaticIpCommand>
    {
        private readonly IAmazonLightsail lightsailClient;
        private readonly ILightsailOperationService lightsailOperationService;

        public CreateStaticIpCommandHandler(
            IAmazonLightsail lightsailClient,
            ILightsailOperationService lightsailOperationService)
        {
            this.lightsailClient = lightsailClient;
            this.lightsailOperationService = lightsailOperationService;
        }

        public async Task<Unit> Handle(CreateStaticIpCommand request, CancellationToken cancellationToken)
        {
            var response = await this.lightsailClient.AllocateStaticIpAsync(new AllocateStaticIpRequest()
            {
                StaticIpName = request.Name
            }, cancellationToken);

            await this.lightsailOperationService.WaitForOperationsAsync(response.Operations);

            return Unit.Value;
        }
    }
}

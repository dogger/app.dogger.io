using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Infrastructure.Docker.Yml;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.Lightsail.OpenFirewallPorts
{
    public class OpenFirewallPortsCommandHandler : IRequestHandler<OpenFirewallPortsCommand>
    {
        private readonly IAmazonLightsail client;
        private readonly ILightsailOperationService lightsailOperationService;

        public OpenFirewallPortsCommandHandler(
            IAmazonLightsail client,
            ILightsailOperationService lightsailOperationService)
        {
            this.client = client;
            this.lightsailOperationService = lightsailOperationService;
        }

        public async Task<Unit> Handle(OpenFirewallPortsCommand request, CancellationToken cancellationToken)
        {
            var response = await this.client.PutInstancePublicPortsAsync(new PutInstancePublicPortsRequest()
            {
                InstanceName = request.InstanceName,
                PortInfos = request
                    .Ports
                    .Select(x => new PortInfo()
                    {
                        FromPort = x.FromPort,
                        ToPort = x.ToPort,
                        Protocol = x.Protocol == SocketProtocol.Udp ?
                            NetworkProtocol.Udp :
                            NetworkProtocol.Tcp
                    })
                    .ToList()
            }, cancellationToken);

            await this.lightsailOperationService.WaitForOperationsAsync(response.Operation);

            return Unit.Value;
        }
    }
}

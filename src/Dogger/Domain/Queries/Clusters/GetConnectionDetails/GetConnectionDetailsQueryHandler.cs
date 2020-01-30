using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Dogger.Domain.Queries.Amazon.Lightsail.GetAllLightsailDomains;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Infrastructure.Docker.Yml;
using MediatR;

namespace Dogger.Domain.Queries.Clusters.GetConnectionDetails
{
    public class GetConnectionDetailsQueryHandler : IRequestHandler<GetConnectionDetailsQuery, ConnectionDetailsResponse?> 
    {
        private readonly IMediator mediator;

        public GetConnectionDetailsQueryHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<ConnectionDetailsResponse?> Handle(
            GetConnectionDetailsQuery request, 
            CancellationToken cancellationToken)
        {
            var instance = await this.mediator.Send(
                new GetLightsailInstanceByNameQuery(request.ClusterId),
                cancellationToken);
            if (instance == null)
                return null;

            var allDomains = await this.mediator.Send(
                new GetAllLightsailDomainsQuery(),
                cancellationToken);
            var domainForInstance = allDomains
                .SelectMany(x => x.DomainEntries)
                .FirstOrDefault(x => x.Target == instance.PublicIpAddress);

            //map all ports except port 22 (SSH).
            var ports = instance
                .Networking
                .Ports
                .SelectMany(x => Enumerable
                    .Range(
                        x.FromPort,
                        x.ToPort - x.FromPort + 1)
                    .Select(p => new ExposedPort()
                    {
                        Port = p,
                        Protocol = x.Protocol == NetworkProtocol.Udp ?
                            SocketProtocol.Udp :
                            SocketProtocol.Tcp
                    }))
                .Where(x =>
                    x.Port != 22 ||
                    x.Protocol != SocketProtocol.Tcp);

            return new ConnectionDetailsResponse(
                instance.PublicIpAddress,
                domainForInstance?.Name,
                ports);
        }
    }
}

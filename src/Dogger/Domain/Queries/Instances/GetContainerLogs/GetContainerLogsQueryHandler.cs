using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Infrastructure.Docker.Engine;
using MediatR;

namespace Dogger.Domain.Queries.Instances.GetContainerLogs
{
    public class GetContainerLogsQueryHandler : IRequestHandler<GetContainerLogsQuery, ICollection<ContainerLogsResponse>>
    {
        private readonly IMediator mediator;
        private readonly IDockerEngineClientFactory dockerEngineClientFactory;

        public GetContainerLogsQueryHandler(
            IMediator mediator,
            IDockerEngineClientFactory dockerEngineClientFactory)
        {
            this.mediator = mediator;
            this.dockerEngineClientFactory = dockerEngineClientFactory;
        }

        public async Task<ICollection<ContainerLogsResponse>> Handle(GetContainerLogsQuery request, CancellationToken cancellationToken)
        {
            var instance = await mediator.Send(
                new GetLightsailInstanceByNameQuery(request.InstanceName),
                cancellationToken);
            if (instance == null)
                throw new InvalidOperationException("Unknown instance.");

            using var dockerEngineClient = await this.dockerEngineClientFactory.CreateFromIpAddressAsync(instance.PublicIpAddress);
            var containerLogResponses = new HashSet<ContainerLogsResponse>();
                
            var containers = await dockerEngineClient.GetContainersAsync();
            foreach(var container in containers)
            {
                var logs = await dockerEngineClient.GetContainerLogsAsync(
                    container.Id,
                    request.LinesToReturnPerContainer);
                containerLogResponses.Add(new ContainerLogsResponse(
                    container,
                    logs));
            }

            return containerLogResponses;
        }
    }
}

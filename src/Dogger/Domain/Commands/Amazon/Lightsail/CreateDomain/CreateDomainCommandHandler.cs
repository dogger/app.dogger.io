using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Services.Amazon.Lightsail;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.Lightsail.CreateDomain
{
    public class CreateDomainCommandHandler : IRequestHandler<CreateDomainCommand>
    {
        private readonly IAmazonLightsail lightsailClient;
        private readonly ILightsailOperationService lightsailOperationService;

        public CreateDomainCommandHandler(
            IAmazonLightsail lightsailClient,
            ILightsailOperationService lightsailOperationService)
        {
            this.lightsailClient = lightsailClient;
            this.lightsailOperationService = lightsailOperationService;
        }

        public async Task<Unit> Handle(CreateDomainCommand request, CancellationToken cancellationToken)
        {
            var createDomainResponse = await this.lightsailClient.CreateDomainAsync(new CreateDomainRequest()
            {
                DomainName = request.HostName
            }, cancellationToken);
            await this.lightsailOperationService.WaitForOperationsAsync(createDomainResponse.Operation);

            return Unit.Value;
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName
{
    public class GetLightsailInstanceByNameQueryHandler : IRequestHandler<GetLightsailInstanceByNameQuery, Instance?>
    {
        private readonly IAmazonLightsail lightsailClient;

        public GetLightsailInstanceByNameQueryHandler(
            IAmazonLightsail lightsailClient)
        {
            this.lightsailClient = lightsailClient;
        }

        public async Task<Instance?> Handle(
            GetLightsailInstanceByNameQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.lightsailClient.GetInstanceAsync(new GetInstanceRequest()
                {
                    InstanceName = request.Name
                }, cancellationToken);

                return response?.Instance;
            }
            catch (NotFoundException)
            {
                return null;
            }
        }
    }
}

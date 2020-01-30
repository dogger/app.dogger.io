using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Services.Amazon.Lightsail;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Lightsail.GetAllLightsailDomains
{
    public class GetAllLightsailDomainsQueryHandler : IRequestHandler<GetAllLightsailDomainsQuery, ICollection<global::Amazon.Lightsail.Model.Domain>>
    {
        private readonly IAmazonLightsailDomain lightsailClient;

        public GetAllLightsailDomainsQueryHandler(
            IAmazonLightsailDomain lightsailClient)
        {
            this.lightsailClient = lightsailClient;
        }

        public async Task<ICollection<global::Amazon.Lightsail.Model.Domain>> Handle(GetAllLightsailDomainsQuery request, CancellationToken cancellationToken)
        {
            var response = await this.lightsailClient.GetDomainsAsync(
                new global::Amazon.Lightsail.Model.GetDomainsRequest(), 
                cancellationToken);

            return response.Domains;
        }
    }
}

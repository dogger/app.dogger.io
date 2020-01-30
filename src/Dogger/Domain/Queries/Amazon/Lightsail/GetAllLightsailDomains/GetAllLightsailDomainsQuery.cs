using System.Collections.Generic;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Lightsail.GetAllLightsailDomains
{
    public class GetAllLightsailDomainsQuery : IRequest<ICollection<global::Amazon.Lightsail.Model.Domain>>
    {
    }
}

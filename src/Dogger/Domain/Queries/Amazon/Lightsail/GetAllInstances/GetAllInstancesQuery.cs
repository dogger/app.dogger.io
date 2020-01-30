using System.Collections.Generic;
using Amazon.Lightsail.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Lightsail.GetAllInstances
{
    public class GetAllInstancesQuery : IRequest<IReadOnlyCollection<Instance>>
    {
    }
}

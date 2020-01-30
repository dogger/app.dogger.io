using Amazon.Lightsail.Model;
using MediatR;

namespace Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName
{
    public class GetLightsailInstanceByNameQuery : IRequest<Instance?>
    {
        public string Name { get; }

        public GetLightsailInstanceByNameQuery(string name)
        {
            this.Name = name;
        }
    }
}

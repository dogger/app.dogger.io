using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.Instances.GetInstanceByName
{
    public class GetInstanceByNameQuery : IRequest<Instance?>
    {
        public GetInstanceByNameQuery(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}

using System;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.ElasticContainerRegistry.EnsureRepositoryWithName
{
    public class EnsureRepositoryWithNameCommand : IRequest<RepositoryResponse>
    {
        public string Name { get; }

        public Guid? UserId { get; set; }

        public EnsureRepositoryWithNameCommand(string name)
        {
            this.Name = name;
        }
    }
}

using System;
using System.Diagnostics.CodeAnalysis;

namespace Dogger.Domain.Controllers.PullDog.Api
{
    [ExcludeFromCodeCoverage]
    public class RepositoryResponse
    {
        public RepositoryResponse(Guid pullDogId, string handle, string name)
        {
            this.PullDogId = pullDogId;
            this.Handle = handle;
            this.Name = name;
        }

        public Guid PullDogId { get; set; }

        public string Handle { get; set; }
        public string Name { get; set; }
    }
}

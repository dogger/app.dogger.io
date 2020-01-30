using System;

namespace Dogger.Domain.Queries.PullDog.GetRepositoriesForUser
{
    public class UserRepositoryResponse
    {
        public Guid? PullDogId { get; set; }

        public string? Handle { get; set; }
        public string? Name { get; set; }
    }
}

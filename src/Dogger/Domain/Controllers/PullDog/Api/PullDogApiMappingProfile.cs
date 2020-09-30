using AutoMapper;
using Dogger.Domain.Queries.PullDog.GetRepositoriesForUser;

namespace Dogger.Domain.Controllers.PullDog.Api
{
    public class PullDogApiMappingProfile : Profile
    {
        public PullDogApiMappingProfile()
        {
            CreateMap<UserRepositoryResponse, RepositoryResponse>();
        }
    }
}

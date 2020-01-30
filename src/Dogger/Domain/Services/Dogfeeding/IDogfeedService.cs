using System.Threading.Tasks;

namespace Dogger.Domain.Services.Dogfeeding
{
    public interface IDogfeedService
    {
        Task DogfeedAsync();
    }
}

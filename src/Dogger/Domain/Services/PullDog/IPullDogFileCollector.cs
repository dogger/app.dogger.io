using System.Threading.Tasks;

namespace Dogger.Domain.Services.PullDog
{
    public interface IPullDogFileCollector
    {
        Task<ConfigurationFile?> GetConfigurationFileAsync();

        Task<RepositoryFile[]?> GetRepositoryFilesFromConfiguration(ConfigurationFile configuration);
    }
}

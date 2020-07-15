namespace Dogger.Domain.Services.PullDog
{
    public class RepositoryPullDogFileContext
    {
        public string[] DockerComposeYmlFilePaths { get; }
        public RepositoryFile[] Files { get; }

        public RepositoryPullDogFileContext(
            string[] dockerComposeYmlFilePaths,
            RepositoryFile[] files)
        {
            this.DockerComposeYmlFilePaths = dockerComposeYmlFilePaths;
            this.Files = files;
        }
    }
}

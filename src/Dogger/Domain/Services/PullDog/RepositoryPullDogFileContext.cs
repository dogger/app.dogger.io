namespace Dogger.Domain.Services.PullDog
{
    public class RepositoryPullDogFileContext
    {
        public string[] DockerComposeYmlContents { get; }
        public RepositoryFile[] Files { get; }

        public RepositoryPullDogFileContext(
            string[] dockerComposeYmlContents,
            RepositoryFile[] files)
        {
            this.DockerComposeYmlContents = dockerComposeYmlContents;
            this.Files = files;
        }
    }
}

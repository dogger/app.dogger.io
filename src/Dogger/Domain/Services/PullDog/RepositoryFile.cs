namespace Dogger.Domain.Services.PullDog
{
    public class RepositoryFile
    {
        public string Path { get; }
        public string Contents { get; set; }

        public RepositoryFile(
            string path, 
            string contents)
        {
            this.Path = path;
            this.Contents = contents;
        }
    }
}

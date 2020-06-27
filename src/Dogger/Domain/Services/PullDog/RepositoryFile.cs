namespace Dogger.Domain.Services.PullDog
{
    public class RepositoryFile
    {
        public string Path { get; }
        public byte[] Contents { get; set; }

        public RepositoryFile(
            string path,
            byte[] contents)
        {
            this.Path = path;
            this.Contents = contents;
        }
    }
}

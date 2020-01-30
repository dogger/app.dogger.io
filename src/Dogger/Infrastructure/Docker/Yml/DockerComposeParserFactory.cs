namespace Dogger.Infrastructure.Docker.Yml
{
    public class DockerComposeParserFactory : IDockerComposeParserFactory
    {
        public IDockerComposeParser Create(string dockerComposeYmlContents)
        {
            return new DockerComposeParser(dockerComposeYmlContents);
        }
    }
}

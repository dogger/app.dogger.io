namespace Dogger.Infrastructure.Docker.Yml
{
    public interface IDockerComposeParserFactory
    {
        IDockerComposeParser Create(string dockerComposeYmlContents);
    }
}

using System;
using YamlDotNet.Core;

namespace Dogger.Infrastructure.Docker.Yml
{
    public class DockerComposeSyntaxErrorException : Exception
    {
        public DockerComposeSyntaxErrorException(SemanticErrorException ex) : base("A Docker Compose YML semantic error was encountered: " + ex.Message, ex)
        {
        }

        public DockerComposeSyntaxErrorException(SyntaxErrorException ex) : base("A Docker Compose YML syntax error was encountered: " + ex.Message, ex)
        {
        }
    }
}

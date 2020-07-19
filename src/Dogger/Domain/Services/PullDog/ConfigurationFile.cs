using System;
using System.Collections.Generic;

namespace Dogger.Domain.Services.PullDog
{
    public class ConfigurationFile : ConfigurationFileBase
    {
        public ConfigurationFile()
        {
            DockerComposeYmlFilePaths = new[]
            {
                "docker-compose.yml"
            };
        }

        public ConfigurationFile(string[] dockerComposeYmlFilePaths) : this()
        {
            if (dockerComposeYmlFilePaths.Length == 0)
                return;

            this.DockerComposeYmlFilePaths = dockerComposeYmlFilePaths;
        }

        public bool IsLazy { get; set; }

        public string[] DockerComposeYmlFilePaths { get; set; }
    }

    public class ConfigurationFileOverride : ConfigurationFileBase
    {
        public string[]? DockerComposeYmlFilePaths { get; set; }
    }

    public abstract class ConfigurationFileBase
    {
        public IDictionary<string, string>? BuildArguments { get; set; }

        public TimeSpan Expiry { get; set; }
        public ConversationMode ConversationMode { get; set; }
    }

    public enum ConversationMode
    {
        SingleComment,
        MultipleComments
    }
}

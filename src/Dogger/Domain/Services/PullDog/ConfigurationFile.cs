using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Dogger.Domain.Services.PullDog
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ConfigurationFile : ConfigurationFileBase
    {
        private string[]? dockerComposeYmlFilePaths;

        public ConfigurationFile()
        {
        }

        public ConfigurationFile(string[] dockerComposeYmlFilePaths) : this()
        {
            if (dockerComposeYmlFilePaths.Length == 0)
                return;

            this.DockerComposeYmlFilePaths = dockerComposeYmlFilePaths;
        }

        public bool IsLazy { get; set; }

        public string[] DockerComposeYmlFilePaths
        {
            get => this.dockerComposeYmlFilePaths ?? new[]
            {
                "docker-compose.yml"
            };
            set => this.dockerComposeYmlFilePaths = value;
        }
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

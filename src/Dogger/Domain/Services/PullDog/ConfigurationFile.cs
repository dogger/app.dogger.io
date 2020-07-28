using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Dogger.Domain.Services.PullDog
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class ConfigurationFile : ConfigurationFileBase
    {
        private List<string>? dockerComposeYmlFilePaths;

        public ConfigurationFile()
        {
        }

        public ConfigurationFile(List<string> dockerComposeYmlFilePaths) : this()
        {
            if (dockerComposeYmlFilePaths.Count == 0)
                return;

            this.DockerComposeYmlFilePaths = dockerComposeYmlFilePaths;
        }

        public bool IsLazy { get; set; }

        public List<string> DockerComposeYmlFilePaths
        {
            get => this.dockerComposeYmlFilePaths ?? new List<string>()
            {
                "docker-compose.yml"
            };
            set => this.dockerComposeYmlFilePaths = value;
        }
    }

    public class ConfigurationFileOverride : ConfigurationFileBase
    {
        public List<string>? DockerComposeYmlFilePaths { get; set; }
    }

    public abstract class ConfigurationFileBase
    {
        public IDictionary<string, string>? BuildArguments { get; set; }

        public string? Label { get; set; }
        public TimeSpan Expiry { get; set; }
        public ConversationMode ConversationMode { get; set; }
    }

    public enum ConversationMode
    {
        SingleComment,
        MultipleComments
    }
}

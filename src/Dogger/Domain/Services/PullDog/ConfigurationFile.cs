using System;
using System.Collections.Generic;

namespace Dogger.Domain.Services.PullDog
{
    public class ConfigurationFile : ConfigurationFileBase
    {
        public bool IsLazy { get; set; }
    }

    public class ConfigurationFileOverride : ConfigurationFileBase
    {

    }

    public abstract class ConfigurationFileBase
    {
        public IDictionary<string, string>? BuildArguments { get; set; }
        public string[]? DockerComposeYmlFilePaths { get; set; }

        public TimeSpan Expiry { get; set; }
        public ConversationMode ConversationMode { get; set; }
    }

    public enum ConversationMode
    {
        SingleComment,
        MultipleComments
    }
}

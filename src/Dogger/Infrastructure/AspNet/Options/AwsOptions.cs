using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Infrastructure.AspNet.Options
{
    [ExcludeFromCodeCoverage]
    public class AwsOptions
    {
        [NotLogged]
        public string? AccessKeyId { get; set; }

        [NotLogged]
        public string? SecretAccessKey { get; set; }

        [NotLogged]
        public string? LightsailPrivateKeyPem { get; set; }

        [NotLogged]
        public string? KeyPairName { get; set; }
    }
}

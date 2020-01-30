using System.Diagnostics.CodeAnalysis;
using Amazon;
using Amazon.Lightsail;
using Amazon.Runtime;

namespace Dogger.Domain.Services.Amazon.Lightsail
{
    /// <summary>
    /// This client exists solely as a way to have an additional client only for domain APIs in Lightsail, which are only available for the us-east-1 region.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AmazonLightsailDomainClient : AmazonLightsailClient, IAmazonLightsailDomain
    {
        public AmazonLightsailDomainClient()
        {
        }

        public AmazonLightsailDomainClient(RegionEndpoint region) : base(region)
        {
        }

        public AmazonLightsailDomainClient(AmazonLightsailConfig config) : base(config)
        {
        }

        public AmazonLightsailDomainClient(AWSCredentials credentials) : base(credentials)
        {
        }

        public AmazonLightsailDomainClient(AWSCredentials credentials, RegionEndpoint region) : base(credentials, region)
        {
        }

        public AmazonLightsailDomainClient(AWSCredentials credentials, AmazonLightsailConfig clientConfig) : base(credentials, clientConfig)
        {
        }

        public AmazonLightsailDomainClient(string awsAccessKeyId, string awsSecretAccessKey) : base(awsAccessKeyId, awsSecretAccessKey)
        {
        }

        public AmazonLightsailDomainClient(string awsAccessKeyId, string awsSecretAccessKey, RegionEndpoint region) : base(awsAccessKeyId, awsSecretAccessKey, region)
        {
        }

        public AmazonLightsailDomainClient(string awsAccessKeyId, string awsSecretAccessKey, AmazonLightsailConfig clientConfig) : base(awsAccessKeyId, awsSecretAccessKey, clientConfig)
        {
        }

        public AmazonLightsailDomainClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken) : base(awsAccessKeyId, awsSecretAccessKey, awsSessionToken)
        {
        }

        public AmazonLightsailDomainClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, RegionEndpoint region) : base(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, region)
        {
        }

        public AmazonLightsailDomainClient(string awsAccessKeyId, string awsSecretAccessKey, string awsSessionToken, AmazonLightsailConfig clientConfig) : base(awsAccessKeyId, awsSecretAccessKey, awsSessionToken, clientConfig)
        {
        }
    }
}

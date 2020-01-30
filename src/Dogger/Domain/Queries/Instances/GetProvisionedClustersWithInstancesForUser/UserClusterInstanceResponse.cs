using Dogger.Domain.Models;
using AmazonModel = Amazon.Lightsail.Model.Instance;

namespace Dogger.Domain.Queries.Instances.GetProvisionedClustersWithInstancesForUser
{
    public class UserClusterInstanceResponse
    {
        public AmazonModel AmazonModel { get; }
        public Instance DatabaseModel { get; }

        public UserClusterInstanceResponse(
            AmazonModel amazonModel,
            Instance databaseModel)
        {
            this.AmazonModel = amazonModel;
            this.DatabaseModel = databaseModel;
        }
    }
}

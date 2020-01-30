using System;

namespace Dogger.Domain.Queries.Clusters.GetClusterForUser
{
    public class ClusterQueryTooBroadException : Exception
    {
        public ClusterQueryTooBroadException(string message) : base(message)
        {
            
        }
    }
}

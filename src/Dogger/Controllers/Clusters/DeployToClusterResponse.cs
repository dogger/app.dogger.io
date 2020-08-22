﻿using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class DeployToClusterResponse
    {
        public DeployToClusterResponse(string jobId)
        {
            this.JobId = jobId;
        }

        public string JobId { get; set; }
    }
}

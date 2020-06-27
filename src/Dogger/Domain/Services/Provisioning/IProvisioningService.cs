using System;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Provisioning.Stages;
using Microsoft.Extensions.Hosting;

namespace Dogger.Domain.Services.Provisioning
{
    public interface IProvisioningService : IHostedService
    {
        IProvisioningJob ScheduleJob(
            Blueprint blueprint,
            ScheduleJobOptions? options = null);

        IProvisioningJob? GetJobById(string jobId);

        Task ProcessPendingJobsAsync();

        IProvisioningJob GetCompletedJob();
    }

}

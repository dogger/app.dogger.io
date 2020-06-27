using System;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Stages;
using Microsoft.Extensions.Hosting;

namespace Dogger.Domain.Services.Provisioning
{
    public interface IProvisioningService : IHostedService
    {
        IProvisioningJob ScheduleJob(params Func<IProvisioningStageFactory>[] stageFactories);

        IProvisioningJob? GetJobById(string jobId);

        Task ProcessPendingJobsAsync();

        IProvisioningJob GetCompletedJob();
    }
}

using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Flows;
using Microsoft.Extensions.Hosting;

namespace Dogger.Domain.Services.Provisioning
{
    public interface IProvisioningService : IHostedService
    {
        Task<IProvisioningJob> ScheduleJobAsync(IProvisioningStageFlow flow);

        IProvisioningJob? GetJobById(string jobId);

        Task ProcessPendingJobsAsync();

        IProvisioningJob GetCompletedJob();
    }
}

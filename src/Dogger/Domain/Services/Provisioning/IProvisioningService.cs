using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Flows;
using Microsoft.Extensions.Hosting;

namespace Dogger.Domain.Services.Provisioning
{
    public interface IProvisioningService : IHostedService
    {
        Task<IProvisioningJob> ScheduleJobAsync(IProvisioningStateFlow flow);

        Task<IProvisioningJob?> GetJobByIdAsync(string jobId);

        Task ProcessPendingJobsAsync();
        Task ExecuteJobAsync(ProvisioningJob job);

        IProvisioningJob GetCompletedJob();
    }
}

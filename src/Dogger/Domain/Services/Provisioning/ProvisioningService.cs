using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Infrastructure;
using Dogger.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Dogger.Domain.Services.Provisioning
{
    public class ProvisioningService : IProvisioningService
    {
        public const string CompletedJobId = "J_SUCCEEDED";
        public const string ProtectedResourcePrefix = "main-";

        private readonly ITime time;
        private readonly IServiceProvider serviceProvider;

        private readonly ILogger logger;

        private readonly Queue<ProvisioningJob> jobQueue;
        private readonly ConcurrentDictionary<string, ProvisioningJob> jobsByIds;

        public ProvisioningService(
            ITime time,
            IServiceProvider serviceProvider,
            ILogger logger)
        {
            this.time = time;
            this.serviceProvider = serviceProvider;
            this.logger = logger;

            this.jobQueue = new Queue<ProvisioningJob>();
            this.jobsByIds = new ConcurrentDictionary<string, ProvisioningJob>();
        }

        public static bool IsProtectedResourceName(string? resourceName)
        {
            if (resourceName == null)
                return false;

            if (Debugger.IsAttached && !EnvironmentHelper.IsRunningInTest)
                return false;

            const string whitespacePattern = "\\s";
            while (Regex.IsMatch(resourceName, whitespacePattern))
            {
                resourceName = Regex.Replace(
                    resourceName,
                    whitespacePattern,
                    string.Empty);
            }

            return resourceName
                .Trim()
                .StartsWith(
                    ProtectedResourcePrefix,
                    ignoreCase: true,
                    CultureInfo.InvariantCulture);
        }

        public Task<IProvisioningJob?> GetJobByIdAsync(string jobId)
        {
            if (jobId == CompletedJobId)
                return Task.FromResult<IProvisioningJob?>(GetCompletedJob());

            return this.jobsByIds.TryGetValue(jobId, out var job) ? 
                Task.FromResult<IProvisioningJob?>(job) : 
                Task.FromResult<IProvisioningJob?>(null);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The IServiceScope is disposed once the job has finished running in this case.")]
        public async Task<IProvisioningJob> ScheduleJobAsync(IProvisioningStateFlow flow)
        {
            var scope = this.serviceProvider.CreateScope();

            var job = new ProvisioningJob(flow, scope);
            await job.InitializeAsync();

            if (!this.jobsByIds.TryAdd(job.Id, job))
                throw new InvalidOperationException("Could not add job to concurrent dictionary.");

            this.jobQueue.Enqueue(job);

            return job;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await RunAsync(cancellationToken);
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            do
            {
                await this.time.WaitAsync(1000);

                if (this.jobQueue.Count == 0)
                    continue;

                await ProcessPendingJobsAsync();
            } while (!cancellationToken.IsCancellationRequested);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The provisioning service must never crash.")]
        public async Task ProcessPendingJobsAsync()
        {
            while (this.jobQueue.Count > 0)
            {
                var job = this.jobQueue.Dequeue();
                try
                {
                    var result = await job.CurrentState.UpdateAsync();
                    if (result == ProvisioningStateUpdateResult.InProgress)
                    {
                        this.logger.Debug("Job is still in progress.");

                        this.jobQueue.Enqueue(job);
                        await this.time.WaitAsync(1000);
                    }
                    else
                    {
                        this.logger.Debug("Switching job to next state.");

                        var nextState = await job.Flow.GetNextStateAsync(new NextStateContext(
                            job.Mediator,
                            job.StateFactory,
                            job.CurrentState));
                        if (nextState == null)
                        {
                            this.logger.Information("Job has succeeded.");

                            job.IsSucceeded = true;
                            job.Dispose();
                        }
                        else
                        {
                            this.logger.Debug("Job is initializing.");
                            
                            await nextState.InitializeAsync();

                            job.CurrentState = nextState;
                            this.jobQueue.Enqueue(job);

                            this.logger.Information("Job has initialized.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "An error occured while trying to switch state with the message {ExceptionMessage}.", ex.Message);

                    job.Dispose();
                    job.Exception = ex is StateUpdateException suex ?
                        suex :
                        new StateUpdateException(
                            "A generic error occured while trying to switch state.",
                            ex);
                }
            }
        }

        public IProvisioningJob GetCompletedJob()
        {
            return new PreCompletedProvisioningJob();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

}

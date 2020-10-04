using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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

        private readonly ConcurrentDictionary<string, ProvisioningJob> jobsById;
        private readonly ConcurrentDictionary<string, FirstLastQueue<ProvisioningJob>> jobQueuesByIdempotencyKey;

        public ProvisioningService(
            ITime time,
            IServiceProvider serviceProvider,
            ILogger logger)
        {
            this.time = time;
            this.serviceProvider = serviceProvider;
            this.logger = logger;

            this.jobsById = new ConcurrentDictionary<string, ProvisioningJob>();
            this.jobQueuesByIdempotencyKey = new ConcurrentDictionary<string, FirstLastQueue<ProvisioningJob>>();
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

            return this.jobsById.TryGetValue(jobId, out var job) ?
                Task.FromResult<IProvisioningJob?>(job) :
                Task.FromResult<IProvisioningJob?>(null);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The IServiceScope is disposed once the job has finished running in this case.")]
        public async Task<IProvisioningJob> ScheduleJobAsync(
            string idempotencyKey,
            IProvisioningStateFlow flow)
        {
            var scope = this.serviceProvider.CreateScope();

            var job = new ProvisioningJob(flow, scope);
            await job.InitializeAsync();

            this.jobsById.TryAdd(job.Id, job);

            var queue = GetJobQueueByIdempotencyKey(idempotencyKey);
            queue.Enqueue(job);

            this.logger.Information("Scheduled job #{JobCount} with idempotency key {IdempotencyKey}.", queue.Count, idempotencyKey);

            return job;
        }

        private FirstLastQueue<ProvisioningJob> GetJobQueueByIdempotencyKey(string idempotencyKey)
        {
            return this.jobQueuesByIdempotencyKey.GetOrAdd(
                idempotencyKey,
                new FirstLastQueue<ProvisioningJob>());
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.Information("Starting provisioning service.");
            await RunAsync(cancellationToken);
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            do
            {
                await this.time.WaitAsync(1000);

                if (this.jobQueuesByIdempotencyKey.IsEmpty)
                    continue;

                await ProcessPendingJobsAsync();
            } while (!cancellationToken.IsCancellationRequested);
        }

        public async Task ProcessPendingJobsAsync()
        {
            var taskCreations = this.jobQueuesByIdempotencyKey.Keys.Select(async key =>
                await Task.Factory.StartNew(
                    ProcessPendingJob,
                    key,
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Current));
            var tasks = await Task.WhenAll(taskCreations);
            await Task.WhenAll(tasks);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The provisioning service must never crash.")]
        private async Task ProcessPendingJob(object? state)
        {
            if (!(state is string idempotencyKey))
                throw new InvalidOperationException("A non-string idempotency key was used.");

            var jobQueue = this.jobQueuesByIdempotencyKey.GetOrAdd(
                idempotencyKey, 
                new FirstLastQueue<ProvisioningJob>());
            while (jobQueue.Count > 0)
            {
                var job = jobQueue.Peek();
                if (job == null)
                    throw new InvalidOperationException("A scheduled job was null.");

                try
                {
                    var result = await job.CurrentState.UpdateAsync();
                    if (result == ProvisioningStateUpdateResult.InProgress)
                    {
                        this.logger.Debug("Job {IdempotencyKey} is still in progress.", idempotencyKey);

                        await this.time.WaitAsync(1000);
                    }
                    else
                    {
                        this.logger.Debug("Switching job {IdempotencyKey} to next state.", idempotencyKey);

                        var nextState = await job.Flow.GetNextStateAsync(new NextStateContext(
                            job.Mediator,
                            job.StateFactory,
                            job.CurrentState));
                        if (nextState == null)
                        {
                            this.logger.Information("Job {IdempotencyKey} has succeeded.", idempotencyKey);

                            job.IsSucceeded = true;
                            job.Dispose();
                            jobQueue.Dequeue();
                        }
                        else
                        {
                            this.logger.Debug("Job {IdempotencyKey} is initializing.", idempotencyKey);

                            await nextState.InitializeAsync();

                            job.CurrentState = nextState;

                            this.logger.Information("Job {IdempotencyKey} has initialized.", idempotencyKey);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "An error occured while trying to switch state for job {IdempotencyKey} with the message {ExceptionMessage}.", idempotencyKey, ex.Message);

                    job.Dispose();
                    job.Exception = ex is StateUpdateException suex
                        ? suex
                        : new StateUpdateException(
                            "A generic error occured while trying to switch state.",
                            ex);

                    jobQueue.Dequeue();
                }
            }

            this.jobQueuesByIdempotencyKey.TryRemove(idempotencyKey, out _);
        }

        public IProvisioningJob GetCompletedJob()
        {
            return new PreCompletedProvisioningJob();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.Information("Stopping provisioning service.");
            return Task.CompletedTask;
        }
    }

}

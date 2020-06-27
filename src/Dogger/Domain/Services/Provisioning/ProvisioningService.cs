using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.Instructions;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Dogger.Domain.Services.Provisioning
{
    public class ProvisioningService : IProvisioningService
    {
        public const string CompletedJobId = "J_SUCCEEDED";

        private readonly ITime time;
        private readonly ILogger logger;
        private readonly IInstructionGroupCollectorFactory instructionGroupCollectorFactory;

        private readonly Queue<ProvisioningJob> jobQueue;
        private readonly ConcurrentDictionary<string, ProvisioningJob> jobsByIds;

        public ProvisioningService(
            ITime time,
            ILogger logger,
            IInstructionGroupCollectorFactory instructionGroupCollectorFactory)
        {
            this.time = time;
            this.logger = logger;
            this.instructionGroupCollectorFactory = instructionGroupCollectorFactory;

            this.jobQueue = new Queue<ProvisioningJob>();
            this.jobsByIds = new ConcurrentDictionary<string, ProvisioningJob>();
        }

        public IProvisioningJob? GetJobById(string jobId)
        {
            if (jobId == CompletedJobId)
                return GetCompletedJob();

            return this.jobsByIds.TryGetValue(jobId, out var job) ? job : null;
        }

        public IProvisioningJob ScheduleJob(
            params Func<IProvisioningStageFactory, IProvisioningStage>[] stageFactories)
        {
            var collector = instructionGroupCollectorFactory.Create();
            collector.CollectFromStages(stageFactories);

            var instructions = collector.RetrieveCollectedInstructions();
            var job = new ProvisioningJob(instructions[0]);

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
                if (job.CurrentStage == null)
                    throw new InvalidOperationException("A job's state was not set.");

                try
                {
                    var result = await job.CurrentStage.UpdateAsync();
                    if (result == ProvisioningStateUpdateResult.InProgress)
                    {
                        this.jobQueue.Enqueue(job);
                        await this.time.WaitAsync(1000);
                    }
                    else
                    {
                        var nextState = await job.Flow.GetNextState(new NextStageContext(
                            job.Mediator,
                            job.StateFactory,
                            job.CurrentStage));
                        if (nextState == null)
                        {
                            job.IsSucceeded = true;
                            job.Dispose();
                        }
                        else
                        {
                            await nextState.InitializeAsync();

                            job.CurrentStage = nextState;
                            this.jobQueue.Enqueue(job);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "An error occured while trying to switch state with the message {ExceptionMessage}.", ex.Message);

                    job.Dispose();
                    job.Exception = ex is StageUpdateException suex ?
                        suex :
                        new StageUpdateException(
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

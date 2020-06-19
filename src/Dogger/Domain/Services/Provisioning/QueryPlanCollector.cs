using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.States;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Domain.Services.Provisioning
{
    public class QueryPlanCollector
    {
        private readonly IProvisioningService provisioningService;

        public QueryPlanCollector(
            IProvisioningService provisioningService)
        {
            this.provisioningService = provisioningService;
        }

        public async Task<QueryPlan> CollectFromFlowAsync(IProvisioningStateFlow flow)
        {
            var queryPlan = new QueryPlan();

            var services = new ServiceCollection();
            services.AddMediatR(typeof(QueryPlanCollector).Assembly);

            await using var provider = services.BuildServiceProvider();
            var stateFactory = new ProvisioningStateFactory(provider);

            var mediator = provider.GetRequiredService<IMediator>();

            var nextState = await flow.GetInitialStateAsync(new InitialStateContext(
                mediator,
                stateFactory));
            while (nextState != null)
            {
                await ExecuteStateAsync(nextState);

                nextState = await flow.GetNextStateAsync(new NextStateContext(
                    mediator,
                    stateFactory,
                    nextState));
            }

            return queryPlan;
        }

        private static async Task ExecuteStateAsync(IProvisioningState state)
        {
            using (state)
            {
                await state.InitializeAsync();

                var status = ProvisioningStateUpdateResult.InProgress;
                while (status != ProvisioningStateUpdateResult.Succeeded)
                    status = await state.UpdateAsync();
            }
        }
    }

    public class QueryPlan
    {
        public InstructionGroup[] Groups { get; }
    }

    public class InstructionGroup
    {
        public string Title { get; }
        public IInstruction[] Instructions { get; }
    }

    public interface IInstruction
    {
        string Type { get; }
    }

    public class SshInstruction : IInstruction
    {
        public string Type => "ssh";

        public string CommandText { get; }
    }

    public class HttpInstruction : IInstruction
    {
        public enum HttpInstructionVerb
        {
            Get,
            Put,
            Post,
            Patch,
            Delete
        }

        public string Type => "http";

        public HttpInstructionVerb Verb { get; set; }

        public Dictionary<string, string>? Headers { get; set; }

        public string Url { get; }

        public string? Body { get; set; }
    }
}

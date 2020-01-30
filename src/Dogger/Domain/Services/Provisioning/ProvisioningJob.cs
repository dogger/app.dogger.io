using System;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.States;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Domain.Services.Provisioning
{

    public class ProvisioningJob : IProvisioningStateContext, IDisposable, IProvisioningJob
    {
        public IMediator Mediator { get; }

        private IProvisioningState? state;

        public string Id
        {
            get;
        }

        public bool IsEnded => IsSucceeded || IsFailed;

        public bool IsSucceeded
        {
            get; set;
        }

        public bool IsFailed => Exception != null;

        public StateUpdateException? Exception { get; set; }

        public IProvisioningStateFlow Flow { get; }
        public IProvisioningStateFactory StateFactory { get; }
        public IServiceScope ServiceScope { get; }

        public IProvisioningState CurrentState
        {
            get => 
                this.state ?? 
                throw new InvalidOperationException("CurrentState has not been initialized on the job.");
            set
            {
                if (this.state != null && this.state != value)
                    this.state.Dispose();

                this.state = value;
            }
        }

        public ProvisioningJob(
            IProvisioningStateFlow flow,
            IServiceScope serviceScope)
        {
            this.Flow = flow;
            this.ServiceScope = serviceScope;
            this.StateFactory = new ProvisioningStateFactory(serviceScope.ServiceProvider);
            this.Mediator = serviceScope.ServiceProvider.GetService<IMediator>();

            Id = Guid.NewGuid().ToString();
        }

        public async Task InitializeAsync()
        {
            this.state = await this.Flow.GetInitialStateAsync(new InitialStateContext(
                this.Mediator,
                this.StateFactory));
            await this.state.InitializeAsync();
        }

        public void Dispose()
        {
            this.state?.Dispose();
        }
    }
}

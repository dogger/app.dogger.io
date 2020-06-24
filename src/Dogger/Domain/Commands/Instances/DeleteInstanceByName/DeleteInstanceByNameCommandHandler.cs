using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Payment.UpdateUserSubscription;
using Dogger.Domain.Events.InstanceDeleted;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Amazon.Lightsail;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Instance = Dogger.Domain.Models.Instance;

namespace Dogger.Domain.Commands.Instances.DeleteInstanceByName
{
    public class DeleteInstanceByNameCommandHandler : IRequestHandler<DeleteInstanceByNameCommand>
    {
        private readonly IAmazonLightsail lightsailClient;
        private readonly ILightsailOperationService lightsailOperationService;

        private readonly ILogger logger;
        private readonly IMediator mediator;

        private readonly DataContext dataContext;

        public DeleteInstanceByNameCommandHandler(
            IAmazonLightsail lightsailClient,
            ILightsailOperationService lightsailOperationService,
            ILogger logger,
            IMediator mediator,
            DataContext dataContext)
        {
            this.lightsailClient = lightsailClient;
            this.lightsailOperationService = lightsailOperationService;
            this.logger = logger;
            this.mediator = mediator;
            this.dataContext = dataContext;
        }

        public async Task<Unit> Handle(DeleteInstanceByNameCommand request, CancellationToken cancellationToken)
        {
            var instance = await this.dataContext.Instances
                .Include(x => x.PullDogPullRequest!)
                .ThenInclude(x => x.PullDogRepository!)
                .ThenInclude(x => x.PullDogSettings!)
                .Include(x => x.Cluster)
                .ThenInclude(x => x.Instances)
                .Include(x => x.Cluster)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.Name == request.Name,
                    cancellationToken);
            if (instance != null)
            {
                var userId = instance.Cluster.UserId;

                await RemoveInstanceFromDatabaseAsync(instance, cancellationToken);

                if (userId != null)
                {
                    await mediator.Send(
                        new UpdateUserSubscriptionCommand(userId.Value),
                        cancellationToken);
                }
            }

            await DeleteLightsailInstanceAsync(request, cancellationToken);

            if (instance != null)
            {
                await this.mediator.Send(
                    new InstanceDeletedEvent(instance),
                    cancellationToken);
            }

            return Unit.Value;
        }

        private async Task RemoveInstanceFromDatabaseAsync(
            Instance instance, 
            CancellationToken cancellationToken)
        {
            var cluster = instance.Cluster;
            cluster.Instances.Remove(instance);

            if (cluster.Instances.Count == 0)
                this.dataContext.Clusters.Remove(cluster);

            this.dataContext.Instances.Remove(instance);

            await this.dataContext.SaveChangesAsync(cancellationToken);
        }

        private async Task DeleteLightsailInstanceAsync(DeleteInstanceByNameCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.lightsailClient.DeleteInstanceAsync(new DeleteInstanceRequest()
                {
                    ForceDeleteAddOns = true,
                    InstanceName = request.Name
                }, cancellationToken);

                await this.lightsailOperationService.WaitForOperationsAsync(response.Operations);
            }
            catch (NotFoundException)
            {
                this.logger.Debug("The instance was already deleted, so no action was performed.");
            }
        }
    }
}

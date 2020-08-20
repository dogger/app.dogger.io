using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Auth0.CreateAuth0User;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Auth0.GetAuth0UserFromEmails;
using Dogger.Domain.Queries.Plans.GetDemoPlan;
using Dogger.Infrastructure.Encryption;
using MediatR;
using User = Auth0.ManagementApi.Models.User;
using DoggerUser = Dogger.Domain.Models.User;

namespace Dogger.Domain.Commands.PullDog.InstallPullDogFromEmails
{
    public class InstallPullDogFromEmailsCommandHandler : IRequestHandler<InstallPullDogFromEmailsCommand, DoggerUser>
    {
        private readonly IMediator mediator;
        private readonly IAesEncryptionHelper aesEncryptionHelper;

        private readonly DataContext dataContext;

        public InstallPullDogFromEmailsCommandHandler(
            IMediator mediator,
            IAesEncryptionHelper aesEncryptionHelper,
            DataContext dataContext)
        {
            this.mediator = mediator;
            this.aesEncryptionHelper = aesEncryptionHelper;
            this.dataContext = dataContext;
        }

        public async Task<DoggerUser> Handle(
            InstallPullDogFromEmailsCommand request,
            CancellationToken cancellationToken)
        {
            var auth0User = await EnsureAuth0UserForEmailsAsync(
                request.Emails, 
                cancellationToken);

            var user = await this.mediator.Send(
                new EnsureUserForIdentityCommand(
                    auth0User.UserId,
                    request.Emails.First()),
                cancellationToken);
            if (user.PullDogSettings == null)
            {
                var doggerDemoPlan = await this.mediator.Send(
                    new GetDemoPlanQuery(),
                    cancellationToken);

                user.PullDogSettings = new PullDogSettings()
                {
                    PlanId = request.Plan?.Id ?? doggerDemoPlan.Id,
                    PoolSize = request.Plan?.PoolSize ?? 0,
                    EncryptedApiKey = await this.aesEncryptionHelper.EncryptAsync(
                        Guid.NewGuid().ToString())
                };

                await this.dataContext.SaveChangesAsync(cancellationToken);
            }

            return user;
        }

        private async Task<User> EnsureAuth0UserForEmailsAsync(
            string[] emails, 
            CancellationToken cancellationToken)
        {
            return
                await this.mediator.Send(new GetAuth0UserFromEmailsQuery(emails), cancellationToken) ??
                await this.mediator.Send(new CreateAuth0UserCommand(emails), cancellationToken) ??
                throw new InvalidOperationException("No user could be created.");
        }
    }
}


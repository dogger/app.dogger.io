using System;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Users.GetUserByIdentityName;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Dogger.Domain.Commands.Users.EnsureUserForIdentity
{
    public class EnsureUserForIdentityCommandHandler : IRequestHandler<EnsureUserForIdentityCommand, User>
    {
        private readonly IMediator mediator;
        private readonly ILogger logger;

        public EnsureUserForIdentityCommandHandler(
            IMediator mediator,
            ILogger logger)
        {
            this.mediator = mediator;
            this.logger = logger;
        }

        public async Task<User> Handle(EnsureUserForIdentityCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.IdentityName))
                throw new InvalidOperationException("No identity name specified.");

            var existingUser = await GetExistingIdentityAsync(request, cancellationToken);
            if (existingUser != null)
                return existingUser;

            try
            {
                var newUser = await this.mediator.Send(
                    new CreateUserForIdentityCommand(
                        request.IdentityName,
                        request.Email),
                    cancellationToken);
                return newUser;
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                var conflictingPullRequest = await GetExistingIdentityAsync(request, cancellationToken);
                return conflictingPullRequest!;
            }
            catch (DbUpdateException dbe) when (dbe.InnerException is SqlException sqe)
            {
                this.logger.Error("An unknown database error occured while ensuring a Pull Dog pull request with code {Code}.", sqe.Number);
                throw;
            }
        }

        private async Task<User?> GetExistingIdentityAsync(
            EnsureUserForIdentityCommand request, 
            CancellationToken cancellationToken)
        {
            return await this.mediator.Send(
                new GetUserByIdentityNameQuery(request.IdentityName),
                cancellationToken);
        }
    }
}

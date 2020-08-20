﻿using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Dogger.Domain.Commands.PullDog.EnsurePullDogPullRequest
{
    public class EnsurePullDogPullRequestCommandHandler : IRequestHandler<EnsurePullDogPullRequestCommand, PullDogPullRequest>
    {
        private readonly DataContext dataContext;
        private readonly ILogger logger;

        public EnsurePullDogPullRequestCommandHandler(
            DataContext dataContext,
            ILogger logger)
        {
            this.dataContext = dataContext;
            this.logger = logger;
        }

        public async Task<PullDogPullRequest> Handle(EnsurePullDogPullRequestCommand request, CancellationToken cancellationToken)
        {
            var existingPullRequest = await GetExistingPullRequestAsync(request, cancellationToken);
            if (existingPullRequest != null)
                return existingPullRequest;

            try
            {
                var newPullRequest = new PullDogPullRequest()
                {
                    Handle = request.PullRequestHandle,
                    PullDogRepository = request.Repository
                };

                await this.dataContext.PullDogPullRequests.AddAsync(newPullRequest, cancellationToken);
                await this.dataContext.SaveChangesAsync(cancellationToken);

                return newPullRequest;
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                var conflictingPullRequest = await GetExistingPullRequestAsync(request, cancellationToken);
                return conflictingPullRequest!;
            }
            catch (DbUpdateException dbe) when(dbe.InnerException is SqlException sqe)
            {
                this.logger.Error("An unknown database error occured while ensuring a Pull Dog pull request with code {Code}.", sqe.Number);
                throw;
            }
        }

        private async Task<PullDogPullRequest?> GetExistingPullRequestAsync(EnsurePullDogPullRequestCommand request, CancellationToken cancellationToken)
        {
            return await this.dataContext
                .PullDogPullRequests
                .Include(x => x.Instance)
                .Include(x => x.PullDogRepository)
                .ThenInclude(x => x.PullDogSettings)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(
                    pullRequest =>
                        pullRequest.Handle == request.PullRequestHandle &&
                        pullRequest.PullDogRepository == request.Repository,
                    cancellationToken);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Dogger.Domain.Commands.Amazon.Identity.EnsureAmazonGroupWithName;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Amazon.Identity.GetAmazonUserByName;
using Dogger.Domain.Services.Amazon.Identity;
using Dogger.Infrastructure.Amazon;
using Dogger.Infrastructure.Encryption;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.Identity.EnsureAmazonUserWithName
{
    public class EnsureAmazonUserWithNameCommandHandler : IRequestHandler<EnsureAmazonUserWithNameCommand, AmazonUser>
    {
        private readonly DataContext dataContext;

        private readonly IAmazonIdentityManagementService amazonIdentityManagementService;
        private readonly IAesEncryptionHelper aesEncryptionHelper;
        private readonly IMediator mediator;

        public EnsureAmazonUserWithNameCommandHandler(
            DataContext dataContext,
            IAmazonIdentityManagementService amazonIdentityManagementService,
            IAesEncryptionHelper aesEncryptionHelper,
            IMediator mediator)
        {
            this.dataContext = dataContext;
            this.amazonIdentityManagementService = amazonIdentityManagementService;
            this.aesEncryptionHelper = aesEncryptionHelper;
            this.mediator = mediator;
        }

        public async Task<AmazonUser> Handle(EnsureAmazonUserWithNameCommand request, CancellationToken cancellationToken)
        {
            var amazonUser = await mediator.Send(
                new GetAmazonUserByNameQuery(request.Name),
                cancellationToken);
            if (amazonUser != null)
                return amazonUser;

            var newUser = new AmazonUser()
            {
                Name = request.Name,
                UserId = request.UserId,
                EncryptedSecretAccessKey = Array.Empty<byte>(),
                EncryptedAccessKeyId = Array.Empty<byte>()
            };
            await this.dataContext.AmazonUsers.AddAsync(newUser, cancellationToken);
            await this.dataContext.SaveChangesAsync(cancellationToken);

            await amazonIdentityManagementService.CreateUserAsync(new CreateUserRequest(request.Name)
            {
                Path = AmazonPathHelper.GetUserPath(request.UserId),
                Tags = new List<Tag>()
                {
                    new Tag()
                    {
                        Key = "UserId",
                        Value = request.UserId?.ToString() ?? string.Empty
                    }
                }
            }, cancellationToken);

            try
            {
                var keyResponse = await this.amazonIdentityManagementService.CreateAccessKeyAsync(new CreateAccessKeyRequest()
                {
                    UserName = request.Name
                }, cancellationToken);

                try
                {
                    newUser.EncryptedAccessKeyId = await this.aesEncryptionHelper.EncryptAsync(keyResponse.AccessKey.AccessKeyId);
                    newUser.EncryptedSecretAccessKey = await this.aesEncryptionHelper.EncryptAsync(keyResponse.AccessKey.SecretAccessKey);
                    await this.dataContext.SaveChangesAsync(cancellationToken);

                    await EnsureGroupMembershipAsync(newUser, cancellationToken);
                }
                catch
                {
                    await this.amazonIdentityManagementService.DeleteAccessKeyAsync(new DeleteAccessKeyRequest(request.Name), cancellationToken);
                    throw;
                }
            }
            catch
            {
                await this.amazonIdentityManagementService.DeleteUserAsync(new DeleteUserRequest(request.Name), cancellationToken);
                throw;
            }

            return newUser;
        }

        private async Task EnsureGroupMembershipAsync(
            AmazonUser newUser, 
            CancellationToken cancellationToken)
        {
            if (newUser.UserId == null)
                return;

            var group = await this.mediator.Send(
                new EnsureAmazonGroupWithNameCommand(newUser.UserId.Value.ToString()),
                cancellationToken);
            await this.amazonIdentityManagementService.AddUserToGroupAsync(new AddUserToGroupRequest()
            {
                GroupName = group.GroupName,
                UserName = newUser.Name
            }, cancellationToken);
        }
    }
}

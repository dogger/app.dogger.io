using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Dogger.Domain.Commands.Amazon.Identity.EnsureAmazonUserWithName;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryByName;
using MediatR;
using IdentityTag = Amazon.IdentityManagement.Model.Tag;
using EcrTag = Amazon.ECR.Model.Tag;

namespace Dogger.Domain.Commands.Amazon.ElasticContainerRegistry.EnsureRepositoryWithName
{
    public class EnsureRepositoryWithNameCommandHandler : IRequestHandler<EnsureRepositoryWithNameCommand, RepositoryResponse>
    {
        private readonly IAmazonECR amazonEcr;
        private readonly IAmazonIdentityManagementService amazonIdentityManagementService;
        private readonly IMediator mediator;

        public EnsureRepositoryWithNameCommandHandler(
            IAmazonECR amazonEcr,
            IAmazonIdentityManagementService amazonIdentityManagementService,
            IMediator mediator)
        {
            this.amazonEcr = amazonEcr;
            this.amazonIdentityManagementService = amazonIdentityManagementService;
            this.mediator = mediator;
        }

        public async Task<RepositoryResponse> Handle(EnsureRepositoryWithNameCommand request, CancellationToken cancellationToken)
        {
            var repository = await EnsureCleanRepositoryAsync(request, cancellationToken);

            var readUser = await EnsureEcrUserWithPermissionsAsync(
                request,
                "ecr-read",
                new[]
                {
                    "ecr:BatchGetImage",
                    "ecr:GetDownloadUrlForLayer"
                },
                cancellationToken);

            var writeUser = await EnsureEcrUserWithPermissionsAsync(
                request,
                "ecr-write",
                new[]
                {
                    "ecr:InitiateLayerUpload",
                    "ecr:UploadLayerPart",
                    "ecr:CompleteLayerUpload",
                    "ecr:PutImage",
                    "ecr:BatchCheckLayerAvailability"
                },
                cancellationToken);

            return new RepositoryResponse(
                request.Name,
                RemoveSchemeFromUrl(repository.RepositoryUri),
                readUser,
                writeUser);
        }

        private static string RemoveSchemeFromUrl(string url)
        {
            var schemes = new[]
            {
                "http://", "https://"
            };
            foreach (var scheme in schemes)
            {
                if (url.StartsWith(scheme, StringComparison.InvariantCulture))
                    url = url.Substring(scheme.Length);
            }

            return url;
        }

        private async Task<AmazonUser> EnsureEcrUserWithPermissionsAsync(
            EnsureRepositoryWithNameCommand request, 
            string ecrUserType,
            string[] permissions,
            CancellationToken cancellationToken)
        {
            var user = await this.mediator.Send(new EnsureAmazonUserWithNameCommand($"{ecrUserType}-{request.Name}")
            {
                UserId = request.UserId
            }, cancellationToken);

            await EnsureAuthTokenPolicyForUserAsync(user);
            await EnsureRepositoryPolicyWithPermissions(
                user.Name,
                ecrUserType,
                request.Name,
                permissions);

            return user;
        }

        private async Task EnsureAuthTokenPolicyForUserAsync(AmazonUser user)
        {
            await EnsurePolicyWithPermissionsForResource(
                user.Name,
                "ecr-login",
                $"*",
                new[]
                {
                    "ecr:GetAuthorizationToken"
                });
        }

        private async Task EnsureRepositoryPolicyWithPermissions(
            string userName,
            string policyName,
            string repositoryName,
            string[] permissions)
        {
            await EnsurePolicyWithPermissionsForResource(
                userName,
                policyName,
                $"arn:aws:ecr:*:715796587228:repository/{repositoryName}",
                permissions);
        }

        private async Task EnsurePolicyWithPermissionsForResource(
            string userName,
            string policyName,
            string resource,
            string[] permissions)
        {
            var policyDocumentJson = $@"{{
    ""Statement"": [
        {{
            ""Effect"": ""Allow"",
            ""Action"": [
                {string
                    .Join(",", permissions
                    .Select(x => $"\"{x}\""))}
            ],
            ""Resource"": ""{resource}""
        }}
    ]
}}";
            await this.amazonIdentityManagementService.PutUserPolicyAsync(new PutUserPolicyRequest()
            {
                UserName = userName,
                PolicyName = policyName,
                PolicyDocument = policyDocumentJson
            });
        }

        private async Task<Repository> EnsureCleanRepositoryAsync(EnsureRepositoryWithNameCommand request, CancellationToken cancellationToken)
        {
            var repository = await this.mediator.Send(new GetRepositoryByNameQuery(request.Name), cancellationToken);
            if (repository != null)
            {
                await CleanupRepositoryAsync(request, cancellationToken);
                return repository;
            }

            var createRepositoryResponse = await this.amazonEcr.CreateRepositoryAsync(new CreateRepositoryRequest()
            {
                ImageTagMutability = ImageTagMutability.MUTABLE,
                RepositoryName = request.Name,
                Tags = new List<EcrTag>()
                {
                    new EcrTag()
                    {
                        Key = "UserId",
                        Value = request.UserId?.ToString() ?? string.Empty
                    }
                }
            }, cancellationToken);

            try
            {
                await this.amazonEcr.PutLifecyclePolicyAsync(new PutLifecyclePolicyRequest()
                {
                    RepositoryName = request.Name,
                    LifecyclePolicyText = $@"{{
    ""rules"":[{{
        ""action"":{{
            ""type"":""expire""
        }},
        ""selection"":{{
            ""countType"":""sinceImagePushed"",
            ""countUnit"":""days"",
            ""countNumber"":1,
            ""tagStatus"":""any""
        }},
        ""description"":""cleanup-old"",
        ""rulePriority"":1
    }}]
}}"
                }, cancellationToken);
            }
            catch
            {
                await this.amazonEcr.DeleteRepositoryAsync(new DeleteRepositoryRequest()
                {
                    RepositoryName = request.Name,
                    Force = true
                }, cancellationToken);
                throw;
            }

            return createRepositoryResponse.Repository;
        }

        private async Task CleanupRepositoryAsync(EnsureRepositoryWithNameCommand request, CancellationToken cancellationToken)
        {
            var imagesResponse = await this.amazonEcr.DescribeImagesAsync(new DescribeImagesRequest()
            {
                RepositoryName = request.Name
            }, cancellationToken);

            var imageIdentifiersToDelete = imagesResponse
                .ImageDetails
                .Where(x =>
                    x.ImagePushedAt < DateTime.UtcNow.AddDays(-2) ||
                    x.ImageSizeInBytes > 1024L * 1024L * 1024L * 3L ||
                    x.ImageTags.Count == 0)
                .Select(x => new ImageIdentifier()
                {
                    ImageDigest = x.ImageDigest
                })
                .ToList();
            if (imageIdentifiersToDelete.Count == 0)
                return;

            await this.amazonEcr.BatchDeleteImageAsync(new BatchDeleteImageRequest()
            {
                ImageIds = imageIdentifiersToDelete,
                RepositoryName = request.Name
            }, cancellationToken);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Dogger.Domain.Commands.Amazon.ElasticContainerRegistry.EnsureRepositoryWithName;
using Dogger.Domain.Commands.Amazon.Identity.EnsureAmazonUserWithName;
using Dogger.Domain.Models;
using Dogger.Domain.Models.Builders;
using Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryByName;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Dogger.Tests.Domain.Commands.Amazon.ElasticContainerRegistry
{
    [TestClass]
    public class EnsureRepositoryWithNameCommandHandlerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoExistingRepository_NewRepositoryCreated()
        {
            //Arrange
            var fakeAmazonEcr = Substitute.For<IAmazonECR>();
            fakeAmazonEcr
                .CreateRepositoryAsync(Arg.Is<CreateRepositoryRequest>(args => args.RepositoryName == "some-repository-name"))
                .Returns(new CreateRepositoryResponse()
                {
                    Repository = new Repository()
                    {
                        RepositoryUri = "some-repository-url"
                    }
                });

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureAmazonUserWithNameCommand>())
                .Returns(new AmazonUser());

            var handler = new EnsureRepositoryWithNameCommandHandler(
                fakeAmazonEcr,
                Substitute.For<IAmazonIdentityManagementService>(),
                fakeMediator,
                Substitute.For<IHostEnvironment>());

            //Act
            var repository = await handler.Handle(
                new EnsureRepositoryWithNameCommand("some-repository-name"),
                default);

            //Assert
            Assert.IsNotNull(repository);
            Assert.AreEqual("some-repository-url", repository.HostName);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ExistingRepositoryWithOldImage_CleansOldImageFromExistingRepository()
        {
            //Arrange
            var fakeAmazonEcr = Substitute.For<IAmazonECR>();
            fakeAmazonEcr
                .DescribeImagesAsync(Arg.Is<DescribeImagesRequest>(args => args.RepositoryName == "some-repository-name"))
                .Returns(new DescribeImagesResponse()
                {
                    ImageDetails = new List<ImageDetail>()
                    {
                        new ImageDetail()
                        {
                            ImageDigest = "some-old-image",
                            ImagePushedAt = DateTime.UtcNow.AddDays(-3),
                            ImageTags = new List<string>()
                            {
                                "some-tag"
                            }
                        }
                    }
                });

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetRepositoryByNameQuery>())
                .Returns(new Repository()
                {
                    RepositoryUri = "some-repository-url"
                });

            fakeMediator
                .Send(Arg.Any<EnsureAmazonUserWithNameCommand>())
                .Returns(new AmazonUser());

            var handler = new EnsureRepositoryWithNameCommandHandler(
                fakeAmazonEcr,
                Substitute.For<IAmazonIdentityManagementService>(),
                fakeMediator,
                Substitute.For<IHostEnvironment>());

            //Act
            var repository = await handler.Handle(
                new EnsureRepositoryWithNameCommand("some-repository-name"),
                default);

            //Assert
            Assert.IsNotNull(repository);

            await fakeAmazonEcr
                .Received(1)
                .BatchDeleteImageAsync(Arg.Is<BatchDeleteImageRequest>(args =>
                    args.RepositoryName == "some-repository-name" &&
                    args.ImageIds.Any(i => i.ImageDigest == "some-old-image")));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ExistingRepositoryWithLargeImage_CleansLargeImageFromExistingRepository()
        {
            //Arrange
            var fakeAmazonEcr = Substitute.For<IAmazonECR>();
            fakeAmazonEcr
                .DescribeImagesAsync(Arg.Is<DescribeImagesRequest>(args => args.RepositoryName == "some-repository-name"))
                .Returns(new DescribeImagesResponse()
                {
                    ImageDetails = new List<ImageDetail>()
                    {
                        new ImageDetail()
                        {
                            ImageDigest = "some-large-image",
                            ImageSizeInBytes = 1024L * 1024L * 1024L * 4L,
                            ImageTags = new List<string>()
                            {
                                "some-tag"
                            }
                        }
                    }
                });

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetRepositoryByNameQuery>())
                .Returns(new Repository()
                {
                    RepositoryUri = "some-repository-url"
                });

            fakeMediator
                .Send(Arg.Any<EnsureAmazonUserWithNameCommand>())
                .Returns(new AmazonUser());

            var handler = new EnsureRepositoryWithNameCommandHandler(
                fakeAmazonEcr,
                Substitute.For<IAmazonIdentityManagementService>(),
                fakeMediator,
                Substitute.For<IHostEnvironment>());

            //Act
            var repository = await handler.Handle(
                new EnsureRepositoryWithNameCommand("some-repository-name"),
                default);

            //Assert
            Assert.IsNotNull(repository);

            await fakeAmazonEcr
                .Received(1)
                .BatchDeleteImageAsync(Arg.Is<BatchDeleteImageRequest>(args =>
                    args.RepositoryName == "some-repository-name" &&
                    args.ImageIds.Any(i => i.ImageDigest == "some-large-image")));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ExistingRepositoryWithUntaggedImage_CleansUntaggedImagesFromExistingRepository()
        {
            //Arrange
            var fakeAmazonEcr = Substitute.For<IAmazonECR>();
            fakeAmazonEcr
                .DescribeImagesAsync(Arg.Any<DescribeImagesRequest>())
                .Returns(new DescribeImagesResponse()
                {
                    ImageDetails = new List<ImageDetail>()
                    {
                        new ImageDetail()
                        {
                            ImageDigest = "some-untagged-image",
                            ImagePushedAt = DateTime.UtcNow,
                            ImageSizeInBytes = 1024 * 1024,
                            ImageTags = new List<string>()
                        }
                    }
                });

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetRepositoryByNameQuery>())
                .Returns(new Repository()
                {
                    RepositoryUri = "some-repository-url"
                });

            fakeMediator
                .Send(Arg.Any<EnsureAmazonUserWithNameCommand>())
                .Returns(new AmazonUser());

            var handler = new EnsureRepositoryWithNameCommandHandler(
                fakeAmazonEcr,
                Substitute.For<IAmazonIdentityManagementService>(),
                fakeMediator,
                Substitute.For<IHostEnvironment>());

            //Act
            var repository = await handler.Handle(
                new EnsureRepositoryWithNameCommand("some-repository-name"),
                default);

            //Assert
            Assert.IsNotNull(repository);

            await fakeAmazonEcr
                .Received(1)
                .BatchDeleteImageAsync(Arg.Is<BatchDeleteImageRequest>(args =>
                    args.RepositoryName == "some-repository-name" &&
                    args.ImageIds.Any(i => i.ImageDigest == "some-untagged-image")));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ExistingRepositoryWithValidImages_CleansNothingFromExistingRepository()
        {
            //Arrange
            var fakeAmazonEcr = Substitute.For<IAmazonECR>();
            fakeAmazonEcr
                .DescribeImagesAsync(Arg.Any<DescribeImagesRequest>())
                .Returns(new DescribeImagesResponse()
                {
                    ImageDetails = new List<ImageDetail>()
                    {
                        new ImageDetail()
                        {
                            ImageDigest = "some-valid-image",
                            ImagePushedAt = DateTime.UtcNow,
                            ImageSizeInBytes = 1024 * 1024,
                            ImageTags = new List<string>()
                            {
                                "some-tag"
                            }
                        }
                    }
                });

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetRepositoryByNameQuery>())
                .Returns(new Repository()
                {
                    RepositoryUri = "some-repository-url"
                });

            fakeMediator
                .Send(Arg.Any<EnsureAmazonUserWithNameCommand>())
                .Returns(new AmazonUser());

            var handler = new EnsureRepositoryWithNameCommandHandler(
                fakeAmazonEcr,
                Substitute.For<IAmazonIdentityManagementService>(),
                fakeMediator,
                Substitute.For<IHostEnvironment>());

            //Act
            var repository = await handler.Handle(
                new EnsureRepositoryWithNameCommand("some-repository-name"),
                default);

            //Assert
            Assert.IsNotNull(repository);

            await fakeAmazonEcr
                .DidNotReceive()
                .BatchDeleteImageAsync(Arg.Any<BatchDeleteImageRequest>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoExistingRepository_ReadUserCreatedWithProperPolicies()
        {
            //Arrange
            var fakeAmazonEcr = Substitute.For<IAmazonECR>();
            fakeAmazonEcr
                .CreateRepositoryAsync(Arg.Is<CreateRepositoryRequest>(args => args.RepositoryName == "some-repository-name"))
                .Returns(new CreateRepositoryResponse()
                {
                    Repository = new Repository()
                    {
                        RepositoryUri = "https://example.com"
                    }
                });

            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureAmazonUserWithNameCommand>())
                .Returns(new AmazonUser());

            fakeMediator
                .Send(Arg.Is<EnsureAmazonUserWithNameCommand>(args => args.Name == "environment-ecr-read-some-repository-name"))
                .Returns(new TestAmazonUserBuilder()
                    .WithName("environment-ecr-read-some-repository-name")
                    .Build());

            var fakeHostEnvironment = Substitute.For<IHostEnvironment>();
            fakeHostEnvironment.EnvironmentName.Returns("environment");

            var handler = new EnsureRepositoryWithNameCommandHandler(
                fakeAmazonEcr,
                fakeAmazonIdentityManagementService,
                fakeMediator,
                fakeHostEnvironment);

            //Act
            var repository = await handler.Handle(
                new EnsureRepositoryWithNameCommand("some-repository-name"),
                default);

            //Assert
            Assert.IsNotNull(repository);

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<EnsureAmazonUserWithNameCommand>(args =>
                    args.Name == "environment-ecr-read-some-repository-name"));

            await fakeAmazonIdentityManagementService
                .Received(1)
                .PutUserPolicyAsync(Arg.Is<PutUserPolicyRequest>(args =>
                    args.UserName == "environment-ecr-read-some-repository-name" &&
                    args.PolicyDocument.Contains("ecr:BatchGetImage") &&
                    args.PolicyDocument.Contains("ecr:GetDownloadUrlForLayer") &&
                    args.PolicyDocument.Contains("arn:aws:ecr:*:715796587228:repository/some-repository-name")));

            await fakeAmazonIdentityManagementService
                .Received(1)
                .PutUserPolicyAsync(Arg.Is<PutUserPolicyRequest>(args =>
                    args.UserName == "environment-ecr-read-some-repository-name" &&
                    args.PolicyDocument.Contains("ecr:GetAuthorizationToken")));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoExistingRepository_WriteUserCreatedWithProperPolicies()
        {
            //Arrange
            var fakeAmazonEcr = Substitute.For<IAmazonECR>();
            fakeAmazonEcr
                .CreateRepositoryAsync(Arg.Is<CreateRepositoryRequest>(args => args.RepositoryName == "some-repository-name"))
                .Returns(new CreateRepositoryResponse()
                {
                    Repository = new Repository()
                    {
                        RepositoryUri = "https://example.com"
                    }
                });

            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureAmazonUserWithNameCommand>())
                .Returns(new AmazonUser());

            fakeMediator
                .Send(Arg.Is<EnsureAmazonUserWithNameCommand>(args => args.Name == "environment-ecr-write-some-repository-name"))
                .Returns(new TestAmazonUserBuilder()
                    .WithName("environment-ecr-write-some-repository-name")
                    .Build());

            var fakeHostEnvironment = Substitute.For<IHostEnvironment>();
            fakeHostEnvironment.EnvironmentName.Returns("environment");

            var handler = new EnsureRepositoryWithNameCommandHandler(
                fakeAmazonEcr,
                fakeAmazonIdentityManagementService,
                fakeMediator,
                fakeHostEnvironment);

            //Act
            var repository = await handler.Handle(
                new EnsureRepositoryWithNameCommand("some-repository-name"),
                default);

            //Assert
            Assert.IsNotNull(repository);

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<EnsureAmazonUserWithNameCommand>(args =>
                    args.Name == "environment-ecr-write-some-repository-name"));

            await fakeAmazonIdentityManagementService
                .Received(1)
                .PutUserPolicyAsync(Arg.Is<PutUserPolicyRequest>(args =>
                    args.UserName == "environment-ecr-write-some-repository-name" &&
                    args.PolicyDocument.Contains("ecr:InitiateLayerUpload") &&
                    args.PolicyDocument.Contains("ecr:UploadLayerPart") &&
                    args.PolicyDocument.Contains("ecr:CompleteLayerUpload") &&
                    args.PolicyDocument.Contains("ecr:PutImage") &&
                    args.PolicyDocument.Contains("ecr:BatchCheckLayerAvailability") &&
                    args.PolicyDocument.Contains("arn:aws:ecr:*:715796587228:repository/some-repository-name")));

            await fakeAmazonIdentityManagementService
                .Received(1)
                .PutUserPolicyAsync(Arg.Is<PutUserPolicyRequest>(args =>
                    args.UserName == "environment-ecr-write-some-repository-name" &&
                    args.PolicyDocument.Contains("ecr:GetAuthorizationToken")));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_FailsAtSettingRepositoryRetentionPolicy_DeletesRepository()
        {
            //Arrange
            var fakeAmazonEcr = Substitute.For<IAmazonECR>();
            fakeAmazonEcr
                .CreateRepositoryAsync(Arg.Is<CreateRepositoryRequest>(args => args.RepositoryName == "some-repository-name"))
                .Returns(new CreateRepositoryResponse()
                {
                    Repository = new Repository()
                });

            fakeAmazonEcr
                .PutLifecyclePolicyAsync(Arg.Any<PutLifecyclePolicyRequest>())
                .Throws(new TestException());

            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureAmazonUserWithNameCommand>())
                .Returns(new AmazonUser());

            var handler = new EnsureRepositoryWithNameCommandHandler(
                fakeAmazonEcr,
                fakeAmazonIdentityManagementService,
                fakeMediator,
                Substitute.For<IHostEnvironment>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () => 
                await handler.Handle(
                    new EnsureRepositoryWithNameCommand("some-repository-name"),
                    default));

            //Assert
            Assert.IsNotNull(exception);

            await fakeAmazonEcr
                .Received(1)
                .DeleteRepositoryAsync(Arg.Is<DeleteRepositoryRequest>(args =>
                    args.RepositoryName == "some-repository-name" &&
                    args.Force));
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Dogger.Controllers.PullDog.Api;
using Dogger.Domain.Commands.PullDog.ChangePullDogPlan;
using Dogger.Domain.Commands.PullDog.EnsurePullDogPullRequest;
using Dogger.Domain.Commands.PullDog.OverrideConfigurationForPullRequest;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetPullRequestDetailsByHandle;
using Dogger.Domain.Queries.PullDog.GetPullRequestDetailsFromBranchReference;
using Dogger.Domain.Queries.PullDog.GetRepositoriesForUser;
using Dogger.Domain.Queries.PullDog.GetRepositoryByHandle;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.Encryption;
using Dogger.Infrastructure.GitHub.Octokit;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;
using RepositoriesResponse = Dogger.Controllers.PullDog.Api.RepositoriesResponse;
using User = Dogger.Domain.Models.User;

namespace Dogger.Tests.Controllers.PullDog
{
    [TestClass]
    public class PullDogApiControllerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ChangePlan_PullDogNotInstalledByUser_ReturnsBadRequest()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<EnsureUserForIdentityCommand>(args =>
                    args.IdentityName == "some-identity-name"))
                .Returns(new TestUserBuilder()
                    .WithPullDogSettings(null));

            var fakeMapper = Substitute.For<IMapper>();
            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.ChangePlan(new ChangePlanRequest()
            {
                PlanId = "dummy",
                PoolSize = 1337
            }) as BadRequestObjectResult;

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ChangePlan_ConditionsPassed_ChangesPullDogPlan()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<EnsureUserForIdentityCommand>(args =>
                    args.IdentityName == "some-identity-name"))
                .Returns(new TestUserBuilder()
                    .WithPullDogSettings());

            var fakeMapper = Substitute.For<IMapper>();
            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.ChangePlan(new ChangePlanRequest()
            {
                PlanId = "some-plan-id",
                PoolSize = 1337
            }) as OkResult;

            //Assert
            Assert.IsNotNull(result);

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<ChangePullDogPlanCommand>(args =>
                    args.PoolSize == 1337 &&
                    args.PlanId == "some-plan-id"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Provision_PullRequestHandleAndBranchReferenceMissing_ReturnsBadRequest()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            var fakeMapper = Substitute.For<IMapper>();
            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);

            //Act
            var result = await controller.Provision(new ProvisionRequest()
            {
                PullRequestHandle = null,
                BranchReference = null
            }) as BadRequestObjectResult;

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Provision_RepositoryNotFound_ReturnsNotFound()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            var fakeMapper = Substitute.For<IMapper>();
            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);

            //Act
            var result = await controller.Provision(new ProvisionRequest()
            {
                PullRequestHandle = "dummy"
            }) as NotFoundObjectResult;

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Provision_EncryptedApiKeyIsWrong_ReturnsUnauthorized()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args =>
                    args.RepositoryHandle == "some-repository-handle"))
                .Returns(new TestPullDogRepositoryBuilder()
                    .WithPullDogSettings());

            var fakeMapper = Substitute.For<IMapper>();

            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();
            fakeAesEncryptionHelper
                .DecryptAsync(Arg.Any<byte[]>())
                .Returns("some-decrypted-value");

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);

            //Act
            var result = await controller.Provision(new ProvisionRequest()
            {
                RepositoryHandle = "some-repository-handle",
                PullRequestHandle = "dummy",
                ApiKey = "wrong-api-key"
            }) as UnauthorizedObjectResult;

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Provision_OnlyPullRequestHandleSpecifiedAndPullRequestNotFound_ReturnsNotFound()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args =>
                    args.RepositoryHandle == "some-repository-handle"))
                .Returns(new TestPullDogRepositoryBuilder().Build());

            fakeMediator
                .Send(Arg.Is<GetPullRequestDetailsByHandleQuery>(args =>
                    args.Handle == "some-pull-request-handle"))
                .Returns((PullRequest)null);

            var fakeMapper = Substitute.For<IMapper>();

            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();
            fakeAesEncryptionHelper
                .DecryptAsync(Arg.Any<byte[]>())
                .Returns("dummy");

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);

            //Act
            var result = await controller.Provision(new ProvisionRequest()
            {
                RepositoryHandle = "some-repository-handle",
                PullRequestHandle = "some-pull-request-handle",
                ApiKey = "dummy"
            }) as NotFoundObjectResult;

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Provision_OnlyBranchReferenceSpecifiedAndPullRequestNotInferred_ReturnsNotFound()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args =>
                    args.RepositoryHandle == "some-repository-handle"))
                .Returns(new TestPullDogRepositoryBuilder().Build());

            fakeMediator
                .Send(Arg.Is<GetPullRequestDetailsFromBranchReferenceQuery>(args =>
                    args.BranchReference == "some-branch-reference"))
                .Returns((PullRequest)null);

            var fakeMapper = Substitute.For<IMapper>();

            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();
            fakeAesEncryptionHelper
                .DecryptAsync(Arg.Any<byte[]>())
                .Returns("dummy");

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);

            //Act
            var result = await controller.Provision(new ProvisionRequest()
            {
                RepositoryHandle = "some-repository-handle",
                BranchReference = "some-branch-reference",
                ApiKey = "dummy"
            }) as NotFoundObjectResult;

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Provision_ConfigurationOverridePresentInRequest_UpdatesPullRequestConfigurationOverride()
        {
            //Arrange
            var fakePullDogPullRequest = new TestPullDogPullRequestBuilder().Build();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args =>
                    args.RepositoryHandle == "some-repository-handle"))
                .Returns(new TestPullDogRepositoryBuilder().Build());

            fakeMediator
                .Send(Arg.Is<EnsurePullDogPullRequestCommand>(args =>
                    args.PullRequestHandle == "1337"))
                .Returns(fakePullDogPullRequest);

            fakeMediator
                .Send(Arg.Is<GetPullRequestDetailsFromBranchReferenceQuery>(args =>
                    args.BranchReference == "some-branch-reference"))
                .Returns(new PullRequestBuilder()
                    .WithNumber(1337)
                    .WithState(ItemState.Open)
                    .WithUser(new Octokit.User()));

            var fakeMapper = Substitute.For<IMapper>();

            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();
            fakeAesEncryptionHelper
                .DecryptAsync(Arg.Any<byte[]>())
                .Returns("dummy");

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);

            //Act
            var result = await controller.Provision(new ProvisionRequest()
            {
                RepositoryHandle = "some-repository-handle",
                BranchReference = "some-branch-reference",
                ApiKey = "dummy",
                Configuration = new ConfigurationFileOverride()
            }) as OkObjectResult;

            //Assert
            Assert.IsNotNull(result);

            await fakeMediator
                .Received(1)
                .Send(Arg.Any<OverrideConfigurationForPullRequestCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Provision_PullRequestInferredFromBranchReference_ProvisionsPullDogEnvironment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args =>
                    args.RepositoryHandle == "some-repository-handle"))
                .Returns(new TestPullDogRepositoryBuilder().Build());

            fakeMediator
                .Send(Arg.Is<GetPullRequestDetailsFromBranchReferenceQuery>(args =>
                    args.BranchReference == "some-branch-reference"))
                .Returns(new PullRequestBuilder()
                    .WithNumber(1337)
                    .WithState(ItemState.Open)
                    .WithUser(new Octokit.User()));

            var fakeMapper = Substitute.For<IMapper>();

            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();
            fakeAesEncryptionHelper
                .DecryptAsync(Arg.Any<byte[]>())
                .Returns("dummy");

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);

            //Act
            var result = await controller.Provision(new ProvisionRequest()
            {
                RepositoryHandle = "some-repository-handle",
                BranchReference = "some-branch-reference",
                ApiKey = "dummy"
            });

            //Assert
            Assert.IsNotNull(result as OkObjectResult);

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<ProvisionPullDogEnvironmentCommand>(args =>
                    args.PullRequestHandle == "1337"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Provision_PullRequestInferredFromPullRequestHandle_ProvisionsPullDogEnvironment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args =>
                    args.RepositoryHandle == "some-repository-handle"))
                .Returns(new TestPullDogRepositoryBuilder().Build());

            fakeMediator
                .Send(Arg.Is<GetPullRequestDetailsByHandleQuery>(args =>
                    args.Handle == "some-pull-request-handle"))
                .Returns(new PullRequestBuilder()
                    .WithNumber(1337)
                    .WithState(ItemState.Open)
                    .WithUser(new Octokit.User()));

            var fakeMapper = Substitute.For<IMapper>();

            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();
            fakeAesEncryptionHelper
                .DecryptAsync(Arg.Any<byte[]>())
                .Returns("dummy");

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);

            //Act
            var result = await controller.Provision(new ProvisionRequest()
            {
                RepositoryHandle = "some-repository-handle",
                PullRequestHandle = "some-pull-request-handle",
                ApiKey = "dummy"
            });

            //Assert
            Assert.IsNotNull(result as OkObjectResult);

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<ProvisionPullDogEnvironmentCommand>(args =>
                    args.PullRequestHandle == "1337"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetSettings_NoPullDogSettingsFound_IsInstalledIsFalseAndValuesAreNull()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new TestUserBuilder().Build());

            var fakeMapper = Substitute.For<IMapper>();
            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.GetSettings() as OkObjectResult;

            //Assert
            Assert.IsNotNull(result);

            var response = result.ToObject<SettingsResponse>();
            Assert.IsNull(response.PoolSize);
            Assert.IsNull(response.PlanId);
            Assert.IsNull(response.ApiKey);
            Assert.IsFalse(response.IsInstalled);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetSettings_PullDogSettingsFound_IsInstalledIsTrueAndValuesAreSet()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new TestUserBuilder()
                    .WithPullDogSettings(new TestPullDogSettingsBuilder()
                        .WithPoolSize(1337)
                        .WithPlanId("some-plan-id")));

            var fakeMapper = Substitute.For<IMapper>();

            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();
            fakeAesEncryptionHelper
                .DecryptAsync(Arg.Is<byte[]>(args => args.Single() == 1))
                .Returns("some-api-key");

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.GetSettings() as OkObjectResult;

            //Assert
            Assert.IsNotNull(result);

            var response = result.ToObject<SettingsResponse>();
            Assert.IsTrue(response.IsInstalled);
            Assert.AreEqual(1337, response.PoolSize);
            Assert.AreEqual("some-plan-id", response.PlanId);
            Assert.AreEqual("some-api-key", response.ApiKey);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetRepositories_NoPullDogSettingsFound_ReturnsNotFound()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new TestUserBuilder().Build());

            var fakeMapper = Substitute.For<IMapper>();
            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.GetRepositories() as NotFoundObjectResult;

            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetRepositories_PullDogSettingsFound_ReturnsMappedRepositories()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new TestUserBuilder()
                    .WithPullDogSettings());

            fakeMediator
                .Send(Arg.Any<GetRepositoriesForUserQuery>())
                .Returns(new[] {
                    new UserRepositoryResponse()
                });

            var fakeMapper = Substitute.For<IMapper>();
            var fakeAesEncryptionHelper = Substitute.For<IAesEncryptionHelper>();

            var controller = new PullDogApiController(
                fakeMediator,
                fakeMapper,
                fakeAesEncryptionHelper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.GetRepositories() as OkObjectResult;

            //Assert
            Assert.IsNotNull(result);

            var response = result.ToObject<RepositoriesResponse>();
            Assert.AreEqual(1, response.Repositories.Length);
        }
    }
}

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Dogger.Controllers.PullDog.Webhooks;
using Dogger.Controllers.PullDog.Webhooks.Handlers;
using Dogger.Controllers.PullDog.Webhooks.Models;
using Dogger.Domain.Commands.PullDog.EnsurePullDogPullRequest;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetRepositoryByHandle;
using Dogger.Infrastructure.AspNet.Options.GitHub;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;

namespace Dogger.Tests.Controllers.PullDog.Webhooks
{
    [TestClass]
    public class WebhooksControllerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task PullDogWebhook_SeveralHandlersWithTwoAcceptableOnes_AcceptableHandlersHandled()
        {
            //Arrange
            var context = new WebhookPayloadContext(
                new WebhookPayload()
                {
                    Repository = new RepositoryPayload()
                    {
                        Id = 1337
                    },
                    PullRequest = new PullRequestPayload()
                    {
                        Number = 1339
                    },
                    Installation = new InstallationPayload()
                    {
                        Id = 1338
                    }
                },
                null!,
                null!,
                null!);

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<EnsurePullDogPullRequestCommand>(args => args.PullRequestHandle == "1339"))
                .Returns(new TestPullDogPullRequestBuilder().Build());

            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args => args.RepositoryHandle == "1337"))
                .Returns(new TestPullDogRepositoryBuilder()
                    .WithGitHubInstallationId(1338));

            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            var fakeHandler1 = Substitute.For<IWebhookPayloadHandler>();
            var fakeHandler2 = Substitute.For<IWebhookPayloadHandler>();
            var fakeHandler3 = Substitute.For<IWebhookPayloadHandler>();
            var fakeHandler4 = Substitute.For<IWebhookPayloadHandler>();

            fakeHandler1.Event.Returns("some-event");
            fakeHandler2.Event.Returns("some-event");
            fakeHandler3.Event.Returns("some-event");
            fakeHandler4.Event.Returns("some-event");

            fakeHandler1.CanHandle(context.Payload).Returns(false);
            fakeHandler2.CanHandle(context.Payload).Returns(true);
            fakeHandler3.CanHandle(context.Payload).Returns(true);
            fakeHandler4.CanHandle(context.Payload).Returns(false);

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeGitHubClient);

                    services.AddSingleton(fakeMediator);

                    services.AddSingleton(fakeHandler1);
                    services.AddSingleton(fakeHandler2);
                    services.AddSingleton(fakeHandler3);
                    services.AddSingleton(fakeHandler4);

                    services.AddSingleton(Substitute.For<IConfigurationPayloadHandler>());
                    services.AddSingleton(GenerateTestGitHubOptions());
                }
            });

            var webhooksController = environment.ServiceProvider.GetRequiredService<WebhooksController>();
            FakeOutAuthenticResponse(webhooksController);

            //Act
            await webhooksController.PullDogWebhook(context.Payload, default);

            //Assert
            await fakeHandler1.DidNotReceive().HandleAsync(Arg.Any<WebhookPayloadContext>());
            await fakeHandler2.Received().HandleAsync(Arg.Any<WebhookPayloadContext>());
            await fakeHandler3.Received().HandleAsync(Arg.Any<WebhookPayloadContext>());
            await fakeHandler4.DidNotReceive().HandleAsync(Arg.Any<WebhookPayloadContext>());
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task PullDogWebhook_RepositoryNotFound_DoesNothing()
        {
            //Arrange
            var context = new WebhookPayloadContext(
                new WebhookPayload()
                {
                    Repository = new RepositoryPayload()
                    {
                        Id = 1337
                    },
                    PullRequest = new PullRequestPayload()
                    {
                        Number = 1339
                    },
                    Installation = new InstallationPayload()
                    {
                        Id = 1338
                    }
                },
                null!,
                null!,
                null!);

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args => args.RepositoryHandle == "1337"))
                .Returns((PullDogRepository)null);

            var fakeHandler = Substitute.For<IWebhookPayloadHandler>();

            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeGitHubClient);
                    services.AddSingleton(fakeMediator);
                    services.AddSingleton(fakeHandler);

                    services.AddSingleton(Substitute.For<IConfigurationPayloadHandler>());
                    services.AddSingleton(GenerateTestGitHubOptions());
                }
            });

            var webhooksController = environment.ServiceProvider.GetRequiredService<WebhooksController>();
            FakeOutAuthenticResponse(webhooksController);

            //Act
            await webhooksController.PullDogWebhook(context.Payload, default);

            //Assert
            fakeHandler
                .DidNotReceive()
                .CanHandle(Arg.Any<WebhookPayload>());

            await fakeHandler
                .DidNotReceive()
                .HandleAsync(Arg.Any<WebhookPayloadContext>());
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task PullDogWebhook_ConfigurationCommitPayloadGiven_InvokesConfigurationPayloadHandlerAndNothingElse()
        {
            //Arrange
            var context = new WebhookPayloadContext(
                new WebhookPayload()
                {
                    Repository = new RepositoryPayload()
                    {
                        Id = 1337
                    },
                    PullRequest = new PullRequestPayload()
                    {
                        Number = 1339
                    },
                    Installation = new InstallationPayload()
                    {
                        Id = 1338,
                        Account = new UserPayload()
                        {
                            Id = 1341
                        }
                    }
                },
                null!,
                null!,
                null!);

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args => 
                    args.RepositoryHandle == "1337"))
                .Returns(new TestPullDogRepositoryBuilder()
                    .WithGitHubInstallationId(1338));

            var fakeWebhookPayloadHandler = Substitute.For<IWebhookPayloadHandler>();

            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            var fakeConfigurationCommitPayloadHandler = Substitute.For<IConfigurationPayloadHandler>();
            fakeConfigurationCommitPayloadHandler
                .Events
                .Returns(new []
                {
                    "some-event"
                });

            fakeConfigurationCommitPayloadHandler
                .CanHandle(Arg.Any<WebhookPayload>())
                .Returns(true);

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeGitHubClient);
                    services.AddSingleton(fakeMediator);
                    services.AddSingleton(fakeWebhookPayloadHandler);

                    services.AddSingleton(fakeConfigurationCommitPayloadHandler);
                    services.AddSingleton(GenerateTestGitHubOptions());
                }
            });

            var webhooksController = environment.ServiceProvider.GetRequiredService<WebhooksController>();
            FakeOutAuthenticResponse(webhooksController);

            //Act
            await webhooksController.PullDogWebhook(context.Payload, default);

            //Assert
            fakeWebhookPayloadHandler
                .DidNotReceive()
                .CanHandle(Arg.Any<WebhookPayload>());

            await fakeWebhookPayloadHandler
                .DidNotReceive()
                .HandleAsync(Arg.Any<WebhookPayloadContext>());

            fakeConfigurationCommitPayloadHandler
                .Received(1)
                .CanHandle(Arg.Any<WebhookPayload>());

            await fakeConfigurationCommitPayloadHandler
                .Received(1)
                .HandleAsync(Arg.Any<WebhookPayload>());
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task PullDogWebhook_InstallationIdNotMatching_ThrowsException()
        {
            //Arrange
            var context = new WebhookPayloadContext(
                new WebhookPayload()
                {
                    Repository = new RepositoryPayload()
                    {
                        Id = 1337
                    },
                    PullRequest = new PullRequestPayload()
                    {
                        Number = 1339
                    },
                    Installation = new InstallationPayload()
                    {
                        Id = 13371337
                    }
                },
                null!,
                null!,
                null!);

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args => args.RepositoryHandle == "1337"))
                .Returns(new TestPullDogRepositoryBuilder()
                    .WithGitHubInstallationId(13381338));

            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            var fakeWebhookPayloadHandler = Substitute.For<IWebhookPayloadHandler>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeGitHubClient);
                    services.AddSingleton(fakeMediator);
                    services.AddSingleton(fakeWebhookPayloadHandler);

                    services.AddSingleton(Substitute.For<IConfigurationPayloadHandler>());
                    services.AddSingleton(GenerateTestGitHubOptions());
                }
            });

            var webhooksController = environment.ServiceProvider.GetRequiredService<WebhooksController>();
            FakeOutAuthenticResponse(webhooksController);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await webhooksController.PullDogWebhook(context.Payload, default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task PullDogWebhook_PullRequestNumberGivenViaIssueInstead_FetchesPullRequestFromIssueNumber()
        {
            //Arrange
            var context = new WebhookPayloadContext(
                new WebhookPayload()
                {
                    Repository = new RepositoryPayload()
                    {
                        Id = 1337
                    },
                    Issue = new IssuePayload()
                    {
                        Number = 1339,
                        PullRequest = new IssuePullRequestPayload()
                    },
                    Installation = new InstallationPayload()
                    {
                        Id = 1338
                    }
                },
                null!,
                null!,
                null!);

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<EnsurePullDogPullRequestCommand>(args =>
                    args.PullRequestHandle == "1339"))
                .Returns(new TestPullDogPullRequestBuilder().Build());

            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args => args.RepositoryHandle == "1337"))
                .Returns(new TestPullDogRepositoryBuilder()
                    .WithGitHubInstallationId(1338));

            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            var fakeWebhookPayloadHandler = Substitute.For<IWebhookPayloadHandler>();
            fakeWebhookPayloadHandler.Event.Returns("some-event");

            fakeWebhookPayloadHandler
                .CanHandle(Arg.Any<WebhookPayload>())
                .Returns(true);

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeGitHubClient);
                    services.AddSingleton(fakeMediator);
                    services.AddSingleton(fakeWebhookPayloadHandler);

                    services.AddSingleton(Substitute.For<IConfigurationPayloadHandler>());
                    services.AddSingleton(GenerateTestGitHubOptions());
                }
            });

            var webhooksController = environment.ServiceProvider.GetRequiredService<WebhooksController>();
            FakeOutAuthenticResponse(webhooksController);

            //Act
            await webhooksController.PullDogWebhook(context.Payload, default);

            //Assert
            fakeWebhookPayloadHandler
                .Received(1)
                .CanHandle(Arg.Any<WebhookPayload>());

            await fakeWebhookPayloadHandler
                .Received(1)
                .HandleAsync(Arg.Is<WebhookPayloadContext>(args =>
                    args.PullRequest != null));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task PullDogWebhook_PullRequestNumberGivenViaPullRequest_FetchesPullRequestFromPullRequestNumber()
        {
            //Arrange
            var context = new WebhookPayloadContext(
                new WebhookPayload()
                {
                    Repository = new RepositoryPayload()
                    {
                        Id = 1337
                    },
                    PullRequest = new PullRequestPayload()
                    {
                        Number = 1339
                    },
                    Installation = new InstallationPayload()
                    {
                        Id = 1338
                    }
                },
                null!,
                null!,
                null!);

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<EnsurePullDogPullRequestCommand>(args =>
                    args.PullRequestHandle == "1339"))
                .Returns(new TestPullDogPullRequestBuilder().Build());

            fakeMediator
                .Send(Arg.Is<GetRepositoryByHandleQuery>(args => args.RepositoryHandle == "1337"))
                .Returns(new TestPullDogRepositoryBuilder()
                    .WithGitHubInstallationId(1338));

            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            var fakeWebhookPayloadHandler = Substitute.For<IWebhookPayloadHandler>();
            fakeWebhookPayloadHandler.Event.Returns("some-event");
            fakeWebhookPayloadHandler
                .CanHandle(Arg.Any<WebhookPayload>())
                .Returns(true);

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeGitHubClient);
                    services.AddSingleton(fakeMediator);
                    services.AddSingleton(fakeWebhookPayloadHandler);

                    services.AddSingleton(Substitute.For<IConfigurationPayloadHandler>());
                    services.AddSingleton(GenerateTestGitHubOptions());
                }
            });

            var webhooksController = environment.ServiceProvider.GetRequiredService<WebhooksController>();
            FakeOutAuthenticResponse(webhooksController);

            //Act
            await webhooksController.PullDogWebhook(context.Payload, default);

            //Assert
            fakeWebhookPayloadHandler
                .Received(1)
                .CanHandle(Arg.Any<WebhookPayload>());

            await fakeWebhookPayloadHandler
                .Received(1)
                .HandleAsync(Arg.Is<WebhookPayloadContext>(args =>
                    args.PullRequest != null));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task PullDogWebhook_AuthenticPayload_ReturnsNoContent()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(Substitute.For<IGitHubClient>());
                }
            });

            environment.Configuration["GitHub:PullDog:WebhookSecret"] = "some-webhook-secret";
            environment.Configuration["GitHub:PullDog:PrivateKeyPath"] = "some-private-key-path";

            using var httpClient = new HttpClient();

            var stringContent = new StringContent("{\"action\":\"edited\",\"changes\":{\"body\":{\"from\":\"Mangler test af:\\r\\n\\r\\n- [x] `RegisterInstanceAsProvisioned` - der skal ikke laves en betaling hvis instansen har et pull dog pull request attached.\\r\\n- [x] `DeleteInstanceByName` - subscriptions skal ikke slettes hvis det er en pull dog instans.\\r\\n- [ ] `ChangePlan` på controller.\\r\\n- [ ] `CreateStripeSubscriptionCommand`\\r\\n- [ ] `ChangePullDogPlanCommand`\\r\\n- [ ] `UpdateUserSubscriptionCommand`\\r\\n- [ ] `GetSupportedPlansQuery`\\r\\n- [ ] `GetPullDogPlanFromSettings`\\r\\n- [ ] `GetSupportedPullDogPlans`\\r\\n- [ ] `DeleteAllPullDogInstancesForUser`\"}},\"issue\":{\"url\":\"https://api.github.com/repos/ffMathy/dogger.io/issues/238\",\"repository_url\":\"https://api.github.com/repos/ffMathy/dogger.io\",\"labels_url\":\"https://api.github.com/repos/ffMathy/dogger.io/issues/238/labels{/name}\",\"comments_url\":\"https://api.github.com/repos/ffMathy/dogger.io/issues/238/comments\",\"events_url\":\"https://api.github.com/repos/ffMathy/dogger.io/issues/238/events\",\"html_url\":\"https://github.com/ffMathy/dogger.io/issues/238\",\"id\":603478783,\"node_id\":\"MDU6SXNzdWU2MDM0Nzg3ODM=\",\"number\":238,\"title\":\"Pull Dog\",\"user\":{\"login\":\"ffMathy\",\"id\":2824010,\"node_id\":\"MDQ6VXNlcjI4MjQwMTA=\",\"avatar_url\":\"https://avatars3.githubusercontent.com/u/2824010?v=4\",\"gravatar_id\":\"\",\"url\":\"https://api.github.com/users/ffMathy\",\"html_url\":\"https://github.com/ffMathy\",\"followers_url\":\"https://api.github.com/users/ffMathy/followers\",\"following_url\":\"https://api.github.com/users/ffMathy/following{/other_user}\",\"gists_url\":\"https://api.github.com/users/ffMathy/gists{/gist_id}\",\"starred_url\":\"https://api.github.com/users/ffMathy/starred{/owner}{/repo}\",\"subscriptions_url\":\"https://api.github.com/users/ffMathy/subscriptions\",\"organizations_url\":\"https://api.github.com/users/ffMathy/orgs\",\"repos_url\":\"https://api.github.com/users/ffMathy/repos\",\"events_url\":\"https://api.github.com/users/ffMathy/events{/privacy}\",\"received_events_url\":\"https://api.github.com/users/ffMathy/received_events\",\"type\":\"User\",\"site_admin\":false},\"labels\":[],\"state\":\"open\",\"locked\":false,\"assignee\":null,\"assignees\":[],\"milestone\":null,\"comments\":10,\"created_at\":\"2020-04-20T19:28:45Z\",\"updated_at\":\"2020-05-30T22:07:12Z\",\"closed_at\":null,\"author_association\":\"OWNER\",\"body\":\"Se indiehackers svar.\"},\"comment\":{\"url\":\"https://api.github.com/repos/ffMathy/dogger.io/issues/comments/633209000\",\"html_url\":\"https://github.com/ffMathy/dogger.io/issues/238#issuecomment-633209000\",\"issue_url\":\"https://api.github.com/repos/ffMathy/dogger.io/issues/238\",\"id\":633209000,\"node_id\":\"MDEyOklzc3VlQ29tbWVudDYzMzIwOTAwMA==\",\"user\":{\"login\":\"ffMathy\",\"id\":2824010,\"node_id\":\"MDQ6VXNlcjI4MjQwMTA=\",\"avatar_url\":\"https://avatars3.githubusercontent.com/u/2824010?v=4\",\"gravatar_id\":\"\",\"url\":\"https://api.github.com/users/ffMathy\",\"html_url\":\"https://github.com/ffMathy\",\"followers_url\":\"https://api.github.com/users/ffMathy/followers\",\"following_url\":\"https://api.github.com/users/ffMathy/following{/other_user}\",\"gists_url\":\"https://api.github.com/users/ffMathy/gists{/gist_id}\",\"starred_url\":\"https://api.github.com/users/ffMathy/starred{/owner}{/repo}\",\"subscriptions_url\":\"https://api.github.com/users/ffMathy/subscriptions\",\"organizations_url\":\"https://api.github.com/users/ffMathy/orgs\",\"repos_url\":\"https://api.github.com/users/ffMathy/repos\",\"events_url\":\"https://api.github.com/users/ffMathy/events{/privacy}\",\"received_events_url\":\"https://api.github.com/users/ffMathy/received_events\",\"type\":\"User\",\"site_admin\":false},\"created_at\":\"2020-05-24T10:16:58Z\",\"updated_at\":\"2020-05-30T22:07:12Z\",\"author_association\":\"OWNER\",\"body\":\"Mangler test af:\\r\\n\\r\\n- [x] `RegisterInstanceAsProvisioned` - der skal ikke laves en betaling hvis instansen har et pull dog pull request attached.\\r\\n- [x] `DeleteInstanceByName` - subscriptions skal ikke slettes hvis det er en pull dog instans.\\r\\n- [x] `ChangePlan` på controller.\\r\\n- [ ] `CreateStripeSubscriptionCommand`\\r\\n- [ ] `ChangePullDogPlanCommand`\\r\\n- [ ] `UpdateUserSubscriptionCommand`\\r\\n- [ ] `GetSupportedPlansQuery`\\r\\n- [ ] `GetPullDogPlanFromSettings`\\r\\n- [ ] `GetSupportedPullDogPlans`\\r\\n- [ ] `DeleteAllPullDogInstancesForUser`\"},\"repository\":{\"id\":237241523,\"node_id\":\"MDEwOlJlcG9zaXRvcnkyMzcyNDE1MjM=\",\"name\":\"dogger.io\",\"full_name\":\"ffMathy/dogger.io\",\"private\":true,\"owner\":{\"login\":\"ffMathy\",\"id\":2824010,\"node_id\":\"MDQ6VXNlcjI4MjQwMTA=\",\"avatar_url\":\"https://avatars3.githubusercontent.com/u/2824010?v=4\",\"gravatar_id\":\"\",\"url\":\"https://api.github.com/users/ffMathy\",\"html_url\":\"https://github.com/ffMathy\",\"followers_url\":\"https://api.github.com/users/ffMathy/followers\",\"following_url\":\"https://api.github.com/users/ffMathy/following{/other_user}\",\"gists_url\":\"https://api.github.com/users/ffMathy/gists{/gist_id}\",\"starred_url\":\"https://api.github.com/users/ffMathy/starred{/owner}{/repo}\",\"subscriptions_url\":\"https://api.github.com/users/ffMathy/subscriptions\",\"organizations_url\":\"https://api.github.com/users/ffMathy/orgs\",\"repos_url\":\"https://api.github.com/users/ffMathy/repos\",\"events_url\":\"https://api.github.com/users/ffMathy/events{/privacy}\",\"received_events_url\":\"https://api.github.com/users/ffMathy/received_events\",\"type\":\"User\",\"site_admin\":false},\"html_url\":\"https://github.com/ffMathy/dogger.io\",\"description\":null,\"fork\":false,\"url\":\"https://api.github.com/repos/ffMathy/dogger.io\",\"forks_url\":\"https://api.github.com/repos/ffMathy/dogger.io/forks\",\"keys_url\":\"https://api.github.com/repos/ffMathy/dogger.io/keys{/key_id}\",\"collaborators_url\":\"https://api.github.com/repos/ffMathy/dogger.io/collaborators{/collaborator}\",\"teams_url\":\"https://api.github.com/repos/ffMathy/dogger.io/teams\",\"hooks_url\":\"https://api.github.com/repos/ffMathy/dogger.io/hooks\",\"issue_events_url\":\"https://api.github.com/repos/ffMathy/dogger.io/issues/events{/number}\",\"events_url\":\"https://api.github.com/repos/ffMathy/dogger.io/events\",\"assignees_url\":\"https://api.github.com/repos/ffMathy/dogger.io/assignees{/user}\",\"branches_url\":\"https://api.github.com/repos/ffMathy/dogger.io/branches{/branch}\",\"tags_url\":\"https://api.github.com/repos/ffMathy/dogger.io/tags\",\"blobs_url\":\"https://api.github.com/repos/ffMathy/dogger.io/git/blobs{/sha}\",\"git_tags_url\":\"https://api.github.com/repos/ffMathy/dogger.io/git/tags{/sha}\",\"git_refs_url\":\"https://api.github.com/repos/ffMathy/dogger.io/git/refs{/sha}\",\"trees_url\":\"https://api.github.com/repos/ffMathy/dogger.io/git/trees{/sha}\",\"statuses_url\":\"https://api.github.com/repos/ffMathy/dogger.io/statuses/{sha}\",\"languages_url\":\"https://api.github.com/repos/ffMathy/dogger.io/languages\",\"stargazers_url\":\"https://api.github.com/repos/ffMathy/dogger.io/stargazers\",\"contributors_url\":\"https://api.github.com/repos/ffMathy/dogger.io/contributors\",\"subscribers_url\":\"https://api.github.com/repos/ffMathy/dogger.io/subscribers\",\"subscription_url\":\"https://api.github.com/repos/ffMathy/dogger.io/subscription\",\"commits_url\":\"https://api.github.com/repos/ffMathy/dogger.io/commits{/sha}\",\"git_commits_url\":\"https://api.github.com/repos/ffMathy/dogger.io/git/commits{/sha}\",\"comments_url\":\"https://api.github.com/repos/ffMathy/dogger.io/comments{/number}\",\"issue_comment_url\":\"https://api.github.com/repos/ffMathy/dogger.io/issues/comments{/number}\",\"contents_url\":\"https://api.github.com/repos/ffMathy/dogger.io/contents/{+path}\",\"compare_url\":\"https://api.github.com/repos/ffMathy/dogger.io/compare/{base}...{head}\",\"merges_url\":\"https://api.github.com/repos/ffMathy/dogger.io/merges\",\"archive_url\":\"https://api.github.com/repos/ffMathy/dogger.io/{archive_format}{/ref}\",\"downloads_url\":\"https://api.github.com/repos/ffMathy/dogger.io/downloads\",\"issues_url\":\"https://api.github.com/repos/ffMathy/dogger.io/issues{/number}\",\"pulls_url\":\"https://api.github.com/repos/ffMathy/dogger.io/pulls{/number}\",\"milestones_url\":\"https://api.github.com/repos/ffMathy/dogger.io/milestones{/number}\",\"notifications_url\":\"https://api.github.com/repos/ffMathy/dogger.io/notifications{?since,all,participating}\",\"labels_url\":\"https://api.github.com/repos/ffMathy/dogger.io/labels{/name}\",\"releases_url\":\"https://api.github.com/repos/ffMathy/dogger.io/releases{/id}\",\"deployments_url\":\"https://api.github.com/repos/ffMathy/dogger.io/deployments\",\"created_at\":\"2020-01-30T15:21:24Z\",\"updated_at\":\"2020-05-30T21:24:42Z\",\"pushed_at\":\"2020-05-30T21:24:40Z\",\"git_url\":\"git://github.com/ffMathy/dogger.io.git\",\"ssh_url\":\"git@github.com:ffMathy/dogger.io.git\",\"clone_url\":\"https://github.com/ffMathy/dogger.io.git\",\"svn_url\":\"https://github.com/ffMathy/dogger.io\",\"homepage\":null,\"size\":2728,\"stargazers_count\":0,\"watchers_count\":0,\"language\":\"C#\",\"has_issues\":true,\"has_projects\":true,\"has_downloads\":true,\"has_wiki\":false,\"has_pages\":false,\"forks_count\":0,\"mirror_url\":null,\"archived\":false,\"disabled\":false,\"open_issues_count\":43,\"license\":null,\"forks\":0,\"open_issues\":43,\"watchers\":0,\"default_branch\":\"master\"},\"sender\":{\"login\":\"ffMathy\",\"id\":2824010,\"node_id\":\"MDQ6VXNlcjI4MjQwMTA=\",\"avatar_url\":\"https://avatars3.githubusercontent.com/u/2824010?v=4\",\"gravatar_id\":\"\",\"url\":\"https://api.github.com/users/ffMathy\",\"html_url\":\"https://github.com/ffMathy\",\"followers_url\":\"https://api.github.com/users/ffMathy/followers\",\"following_url\":\"https://api.github.com/users/ffMathy/following{/other_user}\",\"gists_url\":\"https://api.github.com/users/ffMathy/gists{/gist_id}\",\"starred_url\":\"https://api.github.com/users/ffMathy/starred{/owner}{/repo}\",\"subscriptions_url\":\"https://api.github.com/users/ffMathy/subscriptions\",\"organizations_url\":\"https://api.github.com/users/ffMathy/orgs\",\"repos_url\":\"https://api.github.com/users/ffMathy/repos\",\"events_url\":\"https://api.github.com/users/ffMathy/events{/privacy}\",\"received_events_url\":\"https://api.github.com/users/ffMathy/received_events\",\"type\":\"User\",\"site_admin\":false},\"installation\":{\"id\":9252450,\"node_id\":\"MDIzOkludGVncmF0aW9uSW5zdGFsbGF0aW9uOTI1MjQ1MA==\"}}");
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            stringContent.Headers.Add("X-Hub-Signature", "sha1=d89914e0f80b9f723ea96b0b9eac5ce245625f10");
            stringContent.Headers.Add("X-GitHub-Delivery", "foobar");

            //Act
            var response = await httpClient.PostAsync(
                "http://localhost:14568/api/webhooks/github/pull-dog",
                stringContent);

            //Assert
            Assert.IsNotNull(response);

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.IsNotNull(responseString);

            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        private static void FakeOutAuthenticResponse(WebhooksController webhooksController)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Add("X-Hub-Signature", "sha1=b2c5a0cc23f36c7d7031e6c8d544c22ca8f9fc6a");
            httpContext.Request.Headers.Add("X-GitHub-Delivery", "foobar");
            httpContext.Request.Headers.Add("X-GitHub-Event", "some-event");
            httpContext.Request.Body = new MemoryStream(
                Encoding.UTF8.GetBytes("{}"));

            webhooksController.ControllerContext = new ControllerContext(
                new ActionContext(
                    httpContext,
                    new RouteData(),
                    new ControllerActionDescriptor()));
        }

        private static IOptionsMonitor<GitHubOptions> GenerateTestGitHubOptions()
        {
            var fakeGitHubOptions = Substitute.For<IOptionsMonitor<GitHubOptions>>();
            fakeGitHubOptions
                .CurrentValue
                .Returns(new GitHubOptions()
                {
                    PullDog = new GitHubPullDogOptions()
                    {
                        WebhookSecret = "some-webhook-secret"
                    }
                });
            return fakeGitHubOptions;
        }
    }
}

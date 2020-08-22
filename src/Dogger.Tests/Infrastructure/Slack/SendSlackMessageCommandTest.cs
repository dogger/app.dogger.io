using System.Diagnostics;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ioc;
using Dogger.Infrastructure.Slack;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Slack.Webhooks;

namespace Dogger.Tests.Infrastructure.Slack
{
    [TestClass]
    public class SendSlackMessageCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_SlackNotConfigured_DoesNothing()
        {
            //Arrange
            var fakeOptionalSlackService = Substitute.For<IOptionalService<ISlackClient>>();
            fakeOptionalSlackService.Value.Returns((ISlackClient)null);
            
            var handler = new SendSlackMessageCommandHandler(
                fakeOptionalSlackService);

            //Act
            await handler.Handle(
                new SendSlackMessageCommand("dummy"),
                default);

            //Assert
            Debug.WriteLine("No assert.");
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_SlackConfigured_SendsMessage()
        {
            //Arrange
            var fakeOptionalSlackService = Substitute.For<IOptionalService<ISlackClient>>();
            
            var handler = new SendSlackMessageCommandHandler(
                fakeOptionalSlackService);

            //Act
            await handler.Handle(
                new SendSlackMessageCommand("dummy"),
                default);

            //Assert
            await fakeOptionalSlackService
                .Value
                .Received(1)
                .PostAsync(Arg.Any<SlackMessage>());
        }
    }
}


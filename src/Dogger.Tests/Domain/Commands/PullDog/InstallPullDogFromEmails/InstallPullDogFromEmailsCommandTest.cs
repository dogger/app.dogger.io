using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.InstallPullDogFromEmails;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Commands.PullDog.InstallPullDogFromEmails
{
    [TestClass]
    public class InstallPullDogFromEmailsCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_SomeIntegrationCondition_SomeOutcome()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            //Act
            var result = await environment.Mediator.Send(
                new InstallPullDogFromEmailsCommand());

            //Assert
            Assert.Fail("Not implemented.");
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_SomeUnitCondition_SomeOutcome()
        {
            //Arrange
            var handler = new InstallPullDogFromEmailsCommandHandler();

            //Act
            var result = await handler.Handle(
                new InstallPullDogFromEmailsCommand(),
                default);

            //Assert
            Assert.Fail("Not implemented.");
        }
    }
}


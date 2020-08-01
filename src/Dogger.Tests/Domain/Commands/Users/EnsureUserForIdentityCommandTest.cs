using System.Security.Claims;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Queries.Users.GetUserByIdentityName;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Commands.Users
{
    [TestClass]
    public class EnsureUserForIdentityCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_IdentityNameNull_ExceptionIsThrown()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();
            
            //Act
            var exception = await Assert.ThrowsExceptionAsync<IdentityNameNotProvidedException>(async () => 
                await environment.Mediator.Send(
                    new EnsureUserForIdentityCommand(
                        new ClaimsPrincipal())));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingIdentityNameGiven_ExistingUserIsReturned()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var existingUserIdentityName = "identity-name";
            var existingUser = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName(existingUserIdentityName)));
            
            //Act
            var returnedUser = await environment.Mediator.Send(
                new EnsureUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName(existingUserIdentityName)));

            //Assert
            Assert.IsNotNull(returnedUser);
            Assert.AreNotEqual(default, existingUser.Id);
            Assert.AreEqual(existingUser.Id, returnedUser.Id);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NewIdentityNameGiven_NewUserIsCreated()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var identityName = "identity-name";
            var existingUser = await environment.Mediator.Send(
                new GetUserByIdentityNameQuery(identityName));
            
            //Act
            await environment.Mediator.Send(
                new EnsureUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName(identityName)));

            //Assert
            var createdUser = await environment.Mediator.Send(
                new EnsureUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName(identityName)));

            Assert.IsNull(existingUser);
            Assert.IsNotNull(createdUser);
        }
    }
}

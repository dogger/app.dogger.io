using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Queries.Users.GetUserByIdentityName;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Queries.Users
{
    [TestClass]
    public class GetUserByIdentityNameQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_IdentityNameNull_ExceptionIsThrown()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();
            
            //Act
            var exception = await Assert.ThrowsExceptionAsync<IdentityNameNotProvidedException>(async () => 
                await environment.Mediator.Send(
                    new GetUserByIdentityNameQuery(
                        identityName: null)));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_MultipleUsersExist_MatchingUserIsReturned()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var existingUserIdentityName = "identity-name";

            await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-dummy-user")));

            var existingUser = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName(existingUserIdentityName)));

            await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-other-dummy-user")));

            //Act
            var returnedUser = await environment.Mediator.Send(
                new GetUserByIdentityNameQuery(existingUserIdentityName));

            //Assert
            Assert.IsNotNull(returnedUser);
            Assert.AreNotEqual(default, existingUser.Id);
            Assert.AreEqual(existingUser.Id, returnedUser.Id);
            Assert.AreEqual(existingUser.Identities.Single().Name, returnedUser.Identities.Single().Name);
        }
    }
}

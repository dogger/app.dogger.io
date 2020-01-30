using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Queries.Users.GetUserById;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Queries.Users
{
    [TestClass]
    public class GetUserByIdQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_UserNotFound_ReturnsNull()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            //Act
            var returnedUser = await environment.Mediator.Send(
                new GetUserByIdQuery(Guid.NewGuid()));

            //Assert
            Assert.IsNull(returnedUser);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_UserFound_ReturnsUser()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var existingUser = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-identity-name")));
            
            //Act
            var returnedUser = await environment.Mediator.Send(
                new GetUserByIdQuery(existingUser.Id));

            //Assert
            Assert.IsNotNull(returnedUser);
            Assert.AreNotEqual(default, existingUser.Id);
            Assert.AreEqual(existingUser.Id, returnedUser.Id);
        }
    }
}

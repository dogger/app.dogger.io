using System.Threading.Tasks;
using Dogger.Domain.Models.Builders;
using Dogger.Domain.Queries.Amazon.Identity.GetAmazonUserByName;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Queries.Amazon.Identity
{
    [TestClass]
    public class GetAmazonUserByNameQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_UserNotFound_ReturnsNull()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            //Act
            var user = await environment.Mediator.Send(new GetAmazonUserByNameQuery("some-name"));

            //Assert
            Assert.IsNull(user);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_SeveralUsers_ReturnsFoundUser()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.AmazonUsers.AddAsync(new TestAmazonUserBuilder()
                    .WithName("user-1"));
                await dataContext.AmazonUsers.AddAsync(new TestAmazonUserBuilder()
                    .WithName("user-2"));
                await dataContext.AmazonUsers.AddAsync(new TestAmazonUserBuilder()
                    .WithName("user-3"));
            });

            //Act
            var user = await environment.Mediator.Send(new GetAmazonUserByNameQuery("user-2"));

            //Assert
            Assert.IsNotNull(user);
            Assert.AreEqual("user-2", user.Name);
        }
    }
}

using System;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Amazon.Identity.GetAmazonUserByName;
using Dogger.Tests.TestHelpers;
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
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

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
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.AmazonUsers.AddAsync(new AmazonUser()
                {
                    Name = "user-1",
                    EncryptedSecretAccessKey = Array.Empty<byte>(),
                    EncryptedAccessKeyId = Array.Empty<byte>()
                });
                await dataContext.AmazonUsers.AddAsync(new AmazonUser()
                {
                    Name = "user-2",
                    EncryptedSecretAccessKey = Array.Empty<byte>(),
                    EncryptedAccessKeyId = Array.Empty<byte>()
                });
                await dataContext.AmazonUsers.AddAsync(new AmazonUser()
                {
                    Name = "user-3",
                    EncryptedSecretAccessKey = Array.Empty<byte>(),
                    EncryptedAccessKeyId = Array.Empty<byte>()
                });
            });

            //Act
            var user = await environment.Mediator.Send(new GetAmazonUserByNameQuery("user-2"));

            //Assert
            Assert.IsNotNull(user);
            Assert.AreEqual("user-2", user.Name);
        }
    }
}

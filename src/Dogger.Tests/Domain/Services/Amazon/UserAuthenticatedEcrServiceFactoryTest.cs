using System.Threading.Tasks;
using Dogger.Domain.Queries.Amazon.Identity.GetAmazonUserByName;
using Dogger.Domain.Services.Amazon.Identity;
using Dogger.Infrastructure.Encryption;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Services.Amazon
{
    [TestClass]
    public class UserAuthenticatedEcrServiceFactoryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Create_CredentialsAndRegionGiven_SetsCredentialsAndRegionOnNewEcrService()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetAmazonUserByNameQuery>(args => args.Name == "some-amazon-user-name"))
                .Returns(new TestAmazonUserBuilder()

                    .WithName("some-amazon-user-name"));

            var serviceFactory = new UserAuthenticatedEcrServiceFactory(
                fakeMediator,
                Substitute.For<IAesEncryptionHelper>());

            //Act
            var service = await serviceFactory.CreateAsync("some-amazon-user-name");

            //Assert
            Assert.IsNotNull(service);
        }
    }
}

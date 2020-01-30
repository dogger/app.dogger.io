using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Dogger.Domain.Queries.Amazon.Identity.GetAmazonGroupByName;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Dogger.Tests.Domain.Queries.Amazon.Identity
{
    [TestClass]
    public class GetAmazonGroupByNameQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_GroupNotFound_ReturnsNull()
        {
            //Arrange
            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .GetGroupAsync(Arg.Is<GetGroupRequest>(args => args.GroupName == "some-group-name"))
                .Throws(new NoSuchEntityException("dummy"));

            var handler = new GetAmazonGroupByNameQueryHandler(
                fakeAmazonIdentityManagementService);

            //Act
            var group = await handler.Handle(new GetAmazonGroupByNameQuery("some-group-name"), default);

            //Assert
            Assert.IsNull(group);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_GroupFound_ReturnsFoundGroup()
        {
            //Arrange
            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .GetGroupAsync(Arg.Is<GetGroupRequest>(args => args.GroupName == "some-group-name"))
                .Returns(new GetGroupResponse()
                {
                    Group = new Group()
                });

            var handler = new GetAmazonGroupByNameQueryHandler(
                fakeAmazonIdentityManagementService);

            //Act
            var group = await handler.Handle(new GetAmazonGroupByNameQuery("some-group-name"), default);

            //Assert
            Assert.IsNotNull(group);
        }
    }
}

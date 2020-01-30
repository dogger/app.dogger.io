using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Dogger.Domain.Commands.Amazon.Identity.EnsureAmazonGroupWithName;
using Dogger.Domain.Queries.Amazon.Identity.GetAmazonGroupByName;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Commands.Amazon.Identity
{
    [TestClass]
    public class EnsureAmazonGroupWithNameCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingGroupFound_ExistingGroupReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetAmazonGroupByNameQuery>(args => args.Name == "some-group-name"))
                .Returns(new Group()
                {
                    GroupId = "existing-group"
                });

            var handler = new EnsureAmazonGroupWithNameCommandHandler(
                Substitute.For<IAmazonIdentityManagementService>(),
                fakeMediator);

            //Act
            var group = await handler.Handle(new EnsureAmazonGroupWithNameCommand("some-group-name"), default);

            //Assert
            Assert.AreEqual("existing-group", group.GroupId);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoGroupFound_NewGroupCreatedAndReturned()
        {
            //Arrange
            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .CreateGroupAsync(Arg.Is<CreateGroupRequest>(args => args.GroupName == "some-group-name"))
                .Returns(new CreateGroupResponse()
                {
                    Group = new Group()
                    {
                        GroupId = "new-group"
                    }
                });

            var handler = new EnsureAmazonGroupWithNameCommandHandler(
                fakeAmazonIdentityManagementService,
                Substitute.For<IMediator>());

            //Act
            var group = await handler.Handle(new EnsureAmazonGroupWithNameCommand("some-group-name"), default);

            //Assert
            Assert.AreEqual("new-group", group.GroupId);
        }
    }
}

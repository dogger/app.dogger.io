using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Dogger.Domain.Queries.Amazon.Identity.GetUserPolicy;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Dogger.Tests.Domain.Queries.Amazon.Identity
{
    [TestClass]
    public class GetUserPolicyQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_UserPolicyNotFound_ReturnsNull()
        {
            //Arrange
            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .GetUserPolicyAsync(Arg.Is<GetUserPolicyRequest>(args =>
                    args.UserName == "some-user-name" &&
                    args.PolicyName == "some-policy-name"))
                .Throws(new NoSuchEntityException("dummy"));

            var handler = new GetUserPolicyQueryHandler(fakeAmazonIdentityManagementService);

            //Act
            var policy = await handler.Handle(
                new GetUserPolicyQuery(
                    "some-user-name", 
                    "some-policy-name"), 
                default);

            //Assert
            Assert.IsNull(policy);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_UserPolicyFound_ReturnsUserPolicy()
        {
            //Arrange
            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .GetUserPolicyAsync(Arg.Is<GetUserPolicyRequest>(args =>
                    args.UserName == "some-user-name" &&
                    args.PolicyName == "some-policy-name"))
                .Returns(new GetUserPolicyResponse());

            var handler = new GetUserPolicyQueryHandler(fakeAmazonIdentityManagementService);

            //Act
            var policy = await handler.Handle(
                new GetUserPolicyQuery(
                    "some-user-name",
                    "some-policy-name"),
                default);

            //Assert
            Assert.IsNotNull(policy);
        }
    }
}

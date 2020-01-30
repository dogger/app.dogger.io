using System;
using Dogger.Infrastructure.Amazon;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Infrastructure.Amazon
{
    [TestClass]
    public class AmazonPathHelperTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void GetUserPath_NullUserId_ReturnsSlash()
        {
            //Arrange
            var userId = (Guid?)null;

            //Act
            var path = AmazonPathHelper.GetUserPath(userId);

            //Assert
            Assert.AreEqual("/", path);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void GetUserPath_UserIdGiven_ReturnsProperCustomerPath()
        {
            //Arrange
            var userId = Guid.NewGuid();

            //Act
            var path = AmazonPathHelper.GetUserPath(userId);

            //Assert
            Assert.AreEqual($"/customers/{userId}/", path);
        }
    }
}

using Dogger.Domain.Helpers;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Helpers
{
    [TestClass]
    public class StringHelperTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void ToHexadecimal_StringGiven_ConvertsToHexadecimal()
        {
            //Arrange
            var stringToHex = "hello world";

            //Act
            var hexedString = StringHelper.ToHexadecimal(stringToHex);

            //Assert
            Assert.AreEqual("68656c6c6f20776f726c64", hexedString);
        }
    }
}

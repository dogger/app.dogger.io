using AutoMapper;
using Dogger.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests
{
    [TestClass]
    public class AutoMapperFactoryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public void GetMapperFromServiceProvider_NoValidationExceptionsThrown()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();

            //Act
            var mapper = serviceProvider.GetRequiredService<IMapper>();

            //Assert
            Assert.IsNotNull(mapper);
        }
    }
}

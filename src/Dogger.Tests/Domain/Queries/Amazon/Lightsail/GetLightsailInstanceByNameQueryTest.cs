using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Dogger.Tests.Domain.Queries.Amazon.Lightsail
{
    [TestClass]
    public class GetLightsailInstanceByNameQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InstanceFound_LightsailInstanceReturned()
        {
            //Arrange
            var existingInstance = new Instance();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetInstanceAsync(
                    Arg.Is<GetInstanceRequest>(
                        arg => arg.InstanceName == "some-instance-name"),
                    default)
                .Returns(new GetInstanceResponse()
                {
                    Instance = existingInstance
                });

            var handler = new GetLightsailInstanceByNameQueryHandler(
                fakeAmazonLightsail);

            //Act
            var instance = await handler.Handle(
                new GetLightsailInstanceByNameQuery("some-instance-name"),
                default);

            //Assert
            Assert.IsNotNull(instance);
            Assert.AreSame(existingInstance, instance);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InstanceNotFoundExceptionThrown_ReturnsNull()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetInstanceAsync(
                    Arg.Any<GetInstanceRequest>(),
                    default)
                .Throws(new NotFoundException("Not found"));

            var handler = new GetLightsailInstanceByNameQueryHandler(
                fakeAmazonLightsail);

            //Act
            var instance = await handler.Handle(
                new GetLightsailInstanceByNameQuery("some-instance-name"),
                default);

            //Assert
            Assert.IsNull(instance);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NullLightsailResponseReturned_ReturnsNull()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetInstanceAsync(
                    Arg.Is<GetInstanceRequest>(
                        arg => arg.InstanceName == "some-instance-name"),
                    default)
                .Returns((GetInstanceResponse)null);

            var handler = new GetLightsailInstanceByNameQueryHandler(
                fakeAmazonLightsail);

            //Act
            var instance = await handler.Handle(
                new GetLightsailInstanceByNameQuery("some-instance-name"),
                default);

            //Assert
            Assert.IsNull(instance);
        }
    }
}

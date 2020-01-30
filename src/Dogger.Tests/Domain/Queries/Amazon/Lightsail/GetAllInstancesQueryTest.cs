using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Amazon.Lightsail.GetAllInstances;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Amazon.Lightsail
{
    [TestClass]
    public class GetAllInstancesQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_OneInstanceInAws_LightsailInstanceReturned()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetInstancesAsync(
                    Arg.Any<GetInstancesRequest>(),
                    default)
                .Returns(new GetInstancesResponse()
                {
                    Instances = new List<Instance>()
                    {
                        new Instance()
                        {
                            BundleId = "some-bundle-id"
                        }
                    }
                });

            var handler = new GetAllInstancesQueryHandler(
                fakeAmazonLightsail);

            //Act
            var instances = await handler.Handle(
                new GetAllInstancesQuery(),
                default);

            //Assert
            Assert.IsNotNull(instances);
            Assert.AreEqual(1, instances.Count);
            Assert.AreEqual("some-bundle-id", instances.Single().BundleId);
        }
    }
}

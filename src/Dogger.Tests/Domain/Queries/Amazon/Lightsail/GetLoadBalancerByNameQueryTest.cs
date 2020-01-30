using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLoadBalancerByName;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Dogger.Tests.Domain.Queries.Amazon.Lightsail
{
    [TestClass]
    public class GetLoadBalancerByNameQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_LoadBalancerFound_LightsailLoadBalancerReturned()
        {
            //Arrange
            var existingLoadBalancer = new LoadBalancer();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetLoadBalancerAsync(
                    Arg.Is<GetLoadBalancerRequest>(
                        arg => arg.LoadBalancerName == "some-load-balancer"),
                    default)
                .Returns(new GetLoadBalancerResponse()
                {
                    LoadBalancer = existingLoadBalancer
                });

            var handler = new GetLoadBalancerByNameQueryHandler(
                fakeAmazonLightsail);

            //Act
            var loadBalancer = await handler.Handle(
                new GetLoadBalancerByNameQuery("some-load-balancer"),
                default);

            //Assert
            Assert.IsNotNull(loadBalancer);
            Assert.AreSame(existingLoadBalancer, loadBalancer);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_LoadBalancerNotFound_ReturnsNull()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .GetLoadBalancerAsync(
                    Arg.Is<GetLoadBalancerRequest>(
                        arg => arg.LoadBalancerName == "some-load-balancer"),
                    default)
                .Throws(new NotFoundException("Not found"));

            var handler = new GetLoadBalancerByNameQueryHandler(
                fakeAmazonLightsail);

            //Act
            var loadBalancer = await handler.Handle(
                new GetLoadBalancerByNameQuery("some-load-balancer"),
                default);

            //Assert
            Assert.IsNull(loadBalancer);
        }
    }
}

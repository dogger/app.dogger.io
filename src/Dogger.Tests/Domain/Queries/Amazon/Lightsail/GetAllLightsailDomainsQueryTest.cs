using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Amazon.Lightsail.GetAllLightsailDomains;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Amazon.Lightsail
{
    [TestClass]
    public class GetAllLightsailDomainsQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_OneDomainInAws_LightsailDomainReturned()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsailDomain>();
            fakeAmazonLightsail
                .GetDomainsAsync(
                    Arg.Any<GetDomainsRequest>(),
                    default)
                .Returns(new GetDomainsResponse()
                {
                    Domains = new List<global::Amazon.Lightsail.Model.Domain>()
                    {
                        new global::Amazon.Lightsail.Model.Domain()
                        {
                            Name = "some-domain-name"
                        }
                    }
                });

            var handler = new GetAllLightsailDomainsQueryHandler(
                fakeAmazonLightsail);

            //Act
            var domains = await handler.Handle(
                new GetAllLightsailDomainsQuery(),
                default);

            //Assert
            Assert.IsNotNull(domains);
            Assert.AreEqual(1, domains.Count);
            Assert.AreEqual("some-domain-name", domains.Single().Name);
        }
    }
}

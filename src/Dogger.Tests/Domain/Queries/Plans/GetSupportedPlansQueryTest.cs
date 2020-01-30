using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Plans
{
    [TestClass]
    public class GetSupportedPlansQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InactiveAndActivePlansPresent_ActivePlansReturned()
        {
            //Arrange
            var fakeLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeLightsailClient
                .GetBundlesAsync(Arg.Any<GetBundlesRequest>())
                .Returns(new GetBundlesResponse()
                {
                    Bundles = new List<Bundle>()
                    {
                        new Bundle()
                        {
                            IsActive = false,
                            Name = "incorrect",
                            SupportedPlatforms = new List<string>()
                            {
                                "LINUX_UNIX"
                            }
                        },
                        new Bundle()
                        {
                            IsActive = true,
                            Name = "correct",
                            SupportedPlatforms = new List<string>()
                            {
                                "LINUX_UNIX"
                            }
                        }
                    }
                });

            var handler = new GetSupportedPlansQueryHandler(
                fakeLightsailClient);

            //Act
            var bundles = await handler.Handle(new GetSupportedPlansQuery(), default);

            //Assert
            Assert.AreEqual(1, bundles.Count);
            Assert.AreEqual("correct", bundles.Single().Bundle.Name);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_VariousPlatformsPresent_UnixLinuxPlatformsReturned()
        {
            //Arrange
            var fakeLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeLightsailClient
                .GetBundlesAsync(Arg.Any<GetBundlesRequest>())
                .Returns(new GetBundlesResponse()
                {
                    Bundles = new List<Bundle>()
                    {
                        new Bundle()
                        {
                            IsActive = true,
                            Name = "incorrect",
                            SupportedPlatforms = new List<string>()
                            {
                                "WINDOWS"
                            }
                        },
                        new Bundle()
                        {
                            IsActive = true,
                            Name = "correct",
                            SupportedPlatforms = new List<string>()
                            {
                                "LINUX_UNIX"
                            }
                        }
                    }
                });

            var handler = new GetSupportedPlansQueryHandler(
                fakeLightsailClient);

            //Act
            var bundles = await handler.Handle(new GetSupportedPlansQuery(), default);

            //Assert
            Assert.AreEqual(1, bundles.Count);
            Assert.AreEqual("correct", bundles.Single().Bundle.Name);
        }
    }
}

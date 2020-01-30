using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Amazon.Lightsail.GetAllLightsailDomains;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Queries.Clusters.GetConnectionDetails;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Clusters
{
    [TestClass]
    public class GetConnectionDetailsQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_DomainsPresent_InstanceWithDomainHostNameReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Is<GetLightsailInstanceByNameQuery>(arg =>
                        arg.Name == "some-instance-name"),
                    default)
                .Returns(RandomObjectFactory
                    .Create<Instance>(instance =>
                        instance.PublicIpAddress = "127.0.0.1"));

            fakeMediator
                .Send(
                    Arg.Any<GetAllLightsailDomainsQuery>(),
                    default)
                .Returns(new[] {
                    RandomObjectFactory.Create<global::Amazon.Lightsail.Model.Domain>(domain =>
                        domain.DomainEntries = new List<DomainEntry>()
                        {
                            new DomainEntry()
                            {
                                Target = "127.0.0.1",
                                Name = "some-hostname"
                            }
                        })
                });

            var handler = new GetConnectionDetailsQueryHandler(fakeMediator);

            //Act
            var response = await handler.Handle(new GetConnectionDetailsQuery("some-instance-name"), default);

            //Assert
            Assert.IsNotNull(response);
            Assert.AreEqual("some-hostname", response.HostName);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_SshPortPresentOnInstance_InstanceWithNoPortsReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Is<GetLightsailInstanceByNameQuery>(arg =>
                        arg.Name == "some-instance-name"),
                    default)
                .Returns(RandomObjectFactory
                    .Create<Instance>(instance =>
                        instance.Networking.Ports = new List<InstancePortInfo>()
                        {
                            new InstancePortInfo()
                            {
                                FromPort = 22,
                                ToPort = 22,
                                Protocol = NetworkProtocol.Tcp
                            }
                        }));

            var handler = new GetConnectionDetailsQueryHandler(fakeMediator);

            //Act
            var response = await handler.Handle(new GetConnectionDetailsQuery("some-instance-name"), default);

            //Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Ports.Count());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NonSslPortsPresentOnInstance_InstanceWithPortsReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Is<GetLightsailInstanceByNameQuery>(arg =>
                        arg.Name == "some-instance-name"),
                    default)
                .Returns(RandomObjectFactory
                    .Create<Instance>(instance =>
                        instance.Networking.Ports = new List<InstancePortInfo>()
                        {
                            new InstancePortInfo()
                            {
                                FromPort = 23,
                                ToPort = 24,
                                Protocol = NetworkProtocol.Tcp
                            },
                            new InstancePortInfo()
                            {
                                FromPort = 25,
                                ToPort = 26,
                                Protocol = NetworkProtocol.Udp
                            }
                        }));

            var handler = new GetConnectionDetailsQueryHandler(fakeMediator);

            //Act
            var response = await handler.Handle(new GetConnectionDetailsQuery("some-instance-name"), default);

            //Assert
            Assert.IsNotNull(response);

            var allPorts = response.Ports.ToArray();
            Assert.AreEqual(4, allPorts.Length);

            Assert.IsTrue(allPorts.Any(x => x.Port == 23 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(allPorts.Any(x => x.Port == 24 && x.Protocol == SocketProtocol.Tcp));

            Assert.IsTrue(allPorts.Any(x => x.Port == 25 && x.Protocol == SocketProtocol.Udp));
            Assert.IsTrue(allPorts.Any(x => x.Port == 26 && x.Protocol == SocketProtocol.Udp));
        }
    }
}

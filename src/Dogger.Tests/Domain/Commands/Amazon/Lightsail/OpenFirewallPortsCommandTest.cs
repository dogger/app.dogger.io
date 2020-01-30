using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Amazon.Lightsail.OpenFirewallPorts;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Commands.Amazon.Lightsail
{
    [TestClass]
    public class OpenFirewallPortsCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ProperArgumentsGiven_OperationIsAwaited()
        {
            //Arrange
            var fakeOperation = new Operation() {Id = "some-operation-id"};

            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .PutInstancePublicPortsAsync(
                    Arg.Any<PutInstancePublicPortsRequest>(),
                    default)
                .Returns(new PutInstancePublicPortsResponse()
                {
                    Operation = fakeOperation
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            var handler = new OpenFirewallPortsCommandHandler(
                fakeAmazonLightsailClient,
                fakeLightsailOperationService);

            //Act
            await handler.Handle(
                new OpenFirewallPortsCommand(
                    "some-instance-name",
                    new List<ExposedPortRange>()),
                default);

            //Assert
            await fakeLightsailOperationService
                .Received(1)
                .WaitForOperationsAsync(fakeOperation);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_TcpPortGiven_TcpPortSet()
        {
            //Arrange
            var fakeOperation = new Operation() { Id = "some-operation-id" };

            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .PutInstancePublicPortsAsync(
                    Arg.Any<PutInstancePublicPortsRequest>(),
                    default)
                .Returns(new PutInstancePublicPortsResponse()
                {
                    Operation = fakeOperation
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            var handler = new OpenFirewallPortsCommandHandler(
                fakeAmazonLightsailClient,
                fakeLightsailOperationService);

            //Act
            await handler.Handle(
                new OpenFirewallPortsCommand(
                    "some-instance-name",
                    new List<ExposedPortRange>()
                    {
                        new ExposedPort()
                        {
                            Port = 1337,
                            Protocol = SocketProtocol.Tcp
                        }
                    }),
                default);

            //Assert
            await fakeAmazonLightsailClient
                .Received(1)
                .PutInstancePublicPortsAsync(
                    Arg.Is<PutInstancePublicPortsRequest>(request => request
                        .PortInfos
                        .Any(port =>
                            port.Protocol == NetworkProtocol.Tcp &&
                            port.FromPort == 1337 &&
                            port.FromPort == 1337)));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_UdpPortGiven_UdpPortSet()
        {
            //Arrange
            var fakeOperation = new Operation() { Id = "some-operation-id" };

            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .PutInstancePublicPortsAsync(
                    Arg.Any<PutInstancePublicPortsRequest>(),
                    default)
                .Returns(new PutInstancePublicPortsResponse()
                {
                    Operation = fakeOperation
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            var handler = new OpenFirewallPortsCommandHandler(
                fakeAmazonLightsailClient,
                fakeLightsailOperationService);

            //Act
            await handler.Handle(
                new OpenFirewallPortsCommand(
                    "some-instance-name",
                    new List<ExposedPortRange>()
                    {
                        new ExposedPort()
                        {
                            Port = 1337,
                            Protocol = SocketProtocol.Udp
                        }
                    }),
                default);

            //Assert
            await fakeAmazonLightsailClient
                .Received(1)
                .PutInstancePublicPortsAsync(
                    Arg.Is<PutInstancePublicPortsRequest>(request => request
                        .PortInfos
                        .Any(port =>
                            port.Protocol == NetworkProtocol.Udp &&
                            port.FromPort == 1337 &&
                            port.FromPort == 1337)));
        }
    }
}

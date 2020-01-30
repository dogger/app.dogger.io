using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using Dogger.Domain.Queries.Instances.GetNecessaryInstanceFirewallPorts;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Instances
{
    [TestClass]
    public class GetNecessaryInstanceFirewallPortsQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoArguments_IncludesSshPort()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new GetNecessaryInstanceFirewallPortsQueryHandler(fakeMediator);

            //Act
            var ports = await handler.Handle(new GetNecessaryInstanceFirewallPortsQuery("some-instance-name"), default);

            //Assert
            Assert.IsNotNull(ports);

            Assert.AreEqual(1, ports.Count);

            Assert.AreEqual(22, ports.Single().FromPort);
            Assert.AreEqual(22, ports.Single().ToPort);

            Assert.AreEqual(SocketProtocol.Tcp, ports.Single().Protocol);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_KubernetesControlPlaneInstanceGiven_IncludesSshPort()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetInstanceByNameQuery>(args => args.Name == "some-instance-name"))
                .Returns(new Instance()
                {
                    Type = InstanceType.KubernetesControlPlane
                });

            var handler = new GetNecessaryInstanceFirewallPortsQueryHandler(fakeMediator);

            //Act
            var ports = await handler.Handle(new GetNecessaryInstanceFirewallPortsQuery("some-instance-name"), default);

            //Assert
            Assert.IsNotNull(ports);

            Assert.AreEqual(4, ports.Count);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_KubernetesWorkerInstanceGiven_IncludesSshPort()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetInstanceByNameQuery>(args => args.Name == "some-instance-name"))
                .Returns(new Instance()
                {
                    Type = InstanceType.KubernetesWorker
                });

            var handler = new GetNecessaryInstanceFirewallPortsQueryHandler(fakeMediator);

            //Act
            var ports = await handler.Handle(new GetNecessaryInstanceFirewallPortsQuery("some-instance-name"), default);

            //Assert
            Assert.IsNotNull(ports);

            Assert.AreEqual(3, ports.Count);
        }
    }
}

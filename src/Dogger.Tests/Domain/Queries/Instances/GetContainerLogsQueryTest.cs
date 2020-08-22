using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Queries.Instances.GetContainerLogs;
using Dogger.Infrastructure.Docker.Engine;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Instances
{
    [TestClass]
    public class GetContainerLogsQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoContainersPresent_NoLogResponsesReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Any<GetLightsailInstanceByNameQuery>(),
                    default)
                .Returns(new Instance()
                {
                    PublicIpAddress = "133.7.133.7"
                });

            var fakeDockerEngineClientFactory = Substitute.For<IDockerEngineClientFactory>();

            var handler = new GetContainerLogsQueryHandler(
                fakeMediator,
                fakeDockerEngineClientFactory);

            //Act
            var response = await handler.Handle(
                new GetContainerLogsQuery("some-instance-name"),
                default);

            //Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.Count);
        }
        
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_MultipleContainersPresent_ContainerLogsAreFetchedFromEveryContainer()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Is<GetLightsailInstanceByNameQuery>(
                        arg => arg.Name == "some-instance-name"),
                    default)
                .Returns(new Instance()
                {
                    PublicIpAddress = "133.7.133.7"
                });

            var fakeDockerEngineClient = Substitute.For<IDockerEngineClient>();
            fakeDockerEngineClient
                .GetContainersAsync()
                .Returns(new[]
                {
                    new ContainerResponse(
                        "some-container-1",
                        "dummy",
                        Array.Empty<string>()),
                    new ContainerResponse(
                        "some-container-2",
                        "dummy",
                        Array.Empty<string>())
                });

            var fakeDockerEngineClientFactory = Substitute.For<IDockerEngineClientFactory>();
            fakeDockerEngineClientFactory
                .CreateFromIpAddressAsync("133.7.133.7")
                .Returns(fakeDockerEngineClient);

            var handler = new GetContainerLogsQueryHandler(
                fakeMediator,
                fakeDockerEngineClientFactory);

            //Act
            var response = await handler.Handle(
                new GetContainerLogsQuery("some-instance-name")
                {
                    LinesToReturnPerContainer = 1337
                },
                default);

            //Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Count);

            Assert.IsTrue(response.Any(x => x.Container.Id == "some-container-1"));
            Assert.IsTrue(response.Any(x => x.Container.Id == "some-container-2"));

            await fakeDockerEngineClient
                .Received(1)
                .GetContainerLogsAsync("some-container-1", 1337);

            await fakeDockerEngineClient
                .Received(1)
                .GetContainerLogsAsync("some-container-2", 1337);
        }
    }
}

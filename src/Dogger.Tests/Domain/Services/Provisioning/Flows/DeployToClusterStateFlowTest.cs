using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Domain.Services.Provisioning.Stages.RunDockerComposeOnInstance;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Services.Provisioning.Flows
{
    [TestClass]
    public class DeployToClusterStateFlowTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetInitialState_ValuesGiven_TransfersValuesToInitialState()
        {
            //Arrange
            var buildArguments = new Dictionary<string, string>();

            var flow = new DeployToClusterStateFlow(
                "127.0.0.1",
                new[]
                {
                    "some-docker-compose-contents"
                })
            {
                BuildArguments = buildArguments
            };

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetLightsailInstanceByNameQuery>())
                .Returns(new Instance()
                {
                    PublicIpAddress = "127.0.0.1"
                });

            //Act
            var state = await flow.GetInitialStateAsync(new InitialStateContext(
                fakeMediator,
                stateFactory)) as IRunDockerComposeOnInstanceStage;

            //Assert
            Assert.IsNotNull(state);

            Assert.AreEqual("127.0.0.1", state.IpAddress);
            Assert.AreEqual("some-docker-compose-contents", state.DockerComposeYmlContents?.Single());

            Assert.AreSame(buildArguments, state.BuildArguments);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_FromRunDockerComposeOnInstanceState_ReturnsNull()
        {
            //Arrange
            var flow = new DeployToClusterStateFlow(
                "127.0.0.1",
                new[]
                {
                    "some-docker-compose-contents"
                });

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeState = Substitute.For<IRunDockerComposeOnInstanceStage>();
            var fakeMediator = Substitute.For<IMediator>();

            //Act
            var state = await flow.GetNextStateAsync(new NextStateContext(
                fakeMediator,
                stateFactory,
                fakeState));

            //Assert
            Assert.IsNull(state);
        }
    }
}

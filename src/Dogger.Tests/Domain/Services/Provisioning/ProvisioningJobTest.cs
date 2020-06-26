using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Services.Provisioning
{
    [TestClass]
    public class ProvisioningJobTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Initialize_FlowGiven_SetsInitialStateFromFlow()
        {
            //Arrange
            var fakeInitialState = Substitute.For<IProvisioningStage>();

            var fakeFlow = Substitute.For<IProvisioningStageFlow>();
            fakeFlow
                .GetInitialStateAsync(Arg.Any<InitialStateContext>())
                .Returns(fakeInitialState);

            var provisioningJob = new ProvisioningJob(
                fakeFlow,
                Substitute.For<IServiceScope>());

            //Act
            await provisioningJob.InitializeAsync();

            //Assert
            Assert.AreSame(provisioningJob.CurrentStage, fakeInitialState);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Initialize_FlowGiven_CallsInitializeOnInitialState()
        {
            //Arrange
            var fakeInitialState = Substitute.For<IProvisioningStage>();

            var fakeFlow = Substitute.For<IProvisioningStageFlow>();
            fakeFlow
                .GetInitialStateAsync(Arg.Any<InitialStateContext>())
                .Returns(fakeInitialState);

            var provisioningJob = new ProvisioningJob(
                fakeFlow,
                Substitute.For<IServiceScope>());

            //Act
            await provisioningJob.InitializeAsync();

            //Assert
            await fakeInitialState
                .Received(1)
                .InitializeAsync();
        }
    }
}

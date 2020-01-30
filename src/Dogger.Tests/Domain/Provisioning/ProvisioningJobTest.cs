using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Provisioning
{
    [TestClass]
    public class ProvisioningJobTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Initialize_FlowGiven_SetsInitialStateFromFlow()
        {
            //Arrange
            var fakeInitialState = Substitute.For<IProvisioningState>();

            var fakeFlow = Substitute.For<IProvisioningStateFlow>();
            fakeFlow
                .GetInitialStateAsync(Arg.Any<InitialStateContext>())
                .Returns(fakeInitialState);

            var provisioningJob = new ProvisioningJob(
                fakeFlow,
                Substitute.For<IServiceScope>());

            //Act
            await provisioningJob.InitializeAsync();

            //Assert
            Assert.AreSame(provisioningJob.CurrentState, fakeInitialState);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Initialize_FlowGiven_CallsInitializeOnInitialState()
        {
            //Arrange
            var fakeInitialState = Substitute.For<IProvisioningState>();

            var fakeFlow = Substitute.For<IProvisioningStateFlow>();
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

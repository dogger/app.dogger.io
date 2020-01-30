using System;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Provisioning.Flows
{
    [TestClass]
    public class AggregateProvisioningStateFlowTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetInitialState_CurrentFlowIsFirstFlow_ReturnsInitialStateOfFirstFlow()
        {
            //Arrange
            var context = new InitialStateContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>());

            var fakeProvisioningStateFlow = Substitute.For<IProvisioningStateFlow>();

            var fakeInitialState = await fakeProvisioningStateFlow.GetInitialStateAsync(context);

            var flow = new AggregateProvisioningStateFlow(fakeProvisioningStateFlow);

            //Act
            var initialState = await flow.GetInitialStateAsync(context);

            //Assert
            Assert.AreSame(fakeInitialState, initialState);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetInitialState_CurrentFlowIsSecondFlow_ThrowsException()
        {
            //Arrange
            var initialStateContext = new InitialStateContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>());

            var nextStateContext = new NextStateContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>(),
                Substitute.For<IProvisioningState>());

            var fakeProvisioningStateFlow1 = Substitute.For<IProvisioningStateFlow>();
            fakeProvisioningStateFlow1
                .GetNextStateAsync(nextStateContext)
                .Returns((IProvisioningState)null);

            var fakeProvisioningStateFlow2 = Substitute.For<IProvisioningStateFlow>();

            var flow = new AggregateProvisioningStateFlow(
                fakeProvisioningStateFlow1,
                fakeProvisioningStateFlow2);

            await flow.GetNextStateAsync(nextStateContext);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => 
                await flow.GetInitialStateAsync(initialStateContext));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_CurrentFlowIsFirstFlowAndNotFinished_ReturnsNextStateOfFirstFlow()
        {
            //Arrange
            var nextStateContext = new NextStateContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>(),
                Substitute.For<IProvisioningState>());

            var fakeProvisioningStateFlow1 = Substitute.For<IProvisioningStateFlow>();
            var fakeProvisioningStateFlow2 = Substitute.For<IProvisioningStateFlow>();

            var fakeNextState = await fakeProvisioningStateFlow1.GetNextStateAsync(nextStateContext);

            var flow = new AggregateProvisioningStateFlow(
                fakeProvisioningStateFlow1,
                fakeProvisioningStateFlow2);

            //Act
            var nextState = await flow.GetNextStateAsync(nextStateContext);

            //Assert
            Assert.AreSame(fakeNextState, nextState);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_CurrentFlowIsFirstFlowAndFinished_ReturnsInitialStateOfSecondFlow()
        {
            //Arrange
            var nextStateContext = new NextStateContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>(),
                Substitute.For<IProvisioningState>());

            var fakeProvisioningStateFlow1 = Substitute.For<IProvisioningStateFlow>();
            fakeProvisioningStateFlow1
                .GetNextStateAsync(nextStateContext)
                .Returns((IProvisioningState)null);

            var fakeProvisioningStateFlow2 = Substitute.For<IProvisioningStateFlow>();

            var fakeInitialSecondFlowState = Substitute.For<IProvisioningState>();
            fakeProvisioningStateFlow2
                .GetInitialStateAsync(Arg.Any<InitialStateContext>())
                .Returns(fakeInitialSecondFlowState);

            var flow = new AggregateProvisioningStateFlow(
                fakeProvisioningStateFlow1,
                fakeProvisioningStateFlow2);

            //Act
            var nextState = await flow.GetNextStateAsync(nextStateContext);

            //Assert
            Assert.AreSame(fakeInitialSecondFlowState, nextState);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_CurrentFlowIsFinalFlowAndFinished_ReturnsNull()
        {
            //Arrange
            var nextStateContext = new NextStateContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>(),
                Substitute.For<IProvisioningState>());

            var fakeProvisioningStateFlow1 = Substitute.For<IProvisioningStateFlow>();
            fakeProvisioningStateFlow1
                .GetNextStateAsync(nextStateContext)
                .Returns((IProvisioningState)null);

            var flow = new AggregateProvisioningStateFlow(
                fakeProvisioningStateFlow1);

            //Act
            var nextState = await flow.GetNextStateAsync(nextStateContext);

            //Assert
            Assert.IsNull(nextState);
        }
    }
}

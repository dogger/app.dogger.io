using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
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
    public class ProvisioningServiceTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetCompletedJob_Always_ReturnsCompletedJob()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            //Act
            var job = provisioningService.GetCompletedJob();

            //Assert
            Assert.IsNotNull(job);

            Assert.IsTrue(job.IsEnded);
            Assert.IsTrue(job.IsSucceeded);

            Assert.IsFalse(job.IsFailed);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetJobById_JobExists_JobIsReturned()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var createdJob = await provisioningService.ScheduleJob(Substitute.For<IProvisioningStageFlow>());

            //Act
            var job = await provisioningService.GetJobById(createdJob.Id);

            //Assert
            Assert.IsNotNull(job);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetJobById_JobDoesNotExists_ReturnsNull()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            //Act
            var job = await provisioningService.GetJobById("non-existing-id");

            //Assert
            Assert.IsNull(job);
        }
        
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetJobById_JobExists_JobHasInitialStateOfFlow()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var fakeInitialState = Substitute.For<IProvisioningStage>();

            var fakeFlow = Substitute.For<IProvisioningStageFlow>();
            fakeFlow
                .GetInitialState(Arg.Any<InitialStateContext>())
                .Returns(fakeInitialState);

            var createdJob = await provisioningService.ScheduleJob(fakeFlow);

            //Act
            var job = await provisioningService.GetJobById(createdJob.Id);

            //Assert
            var state = job.CurrentStage;
            Assert.AreSame(fakeInitialState, state);
        }
        
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task StartAsync_CancellationTokenFired_RespectsCancellation()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            //Act
            await provisioningService.StartAsync(new CancellationToken(true));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task StopAsync_CancellationTokenFired_RespectsCancellation()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            //Act
            await provisioningService.StopAsync(new CancellationToken(true));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task StartAsync_JobAvailableWithNoState_ThrowsException()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var createdJob = await provisioningService.ScheduleJob(Substitute.For<IProvisioningStageFlow>());
            createdJob.CurrentStage = null;

            //Act & assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => 
                await provisioningService.StartAsync(new CancellationToken(true)));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task StartAsync_UpdatingThrowsInvalidOperationException_SetsStateUpdateExceptionOnJob()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var createdJob = await provisioningService.ScheduleJob(Substitute.For<IProvisioningStageFlow>());

            var fakeJobState = Substitute.For<IProvisioningStage>();
            fakeJobState
                .UpdateAsync()
                .Throws(new InvalidOperationException("Test error"));

            createdJob.CurrentStage = fakeJobState;

            //Act
            await provisioningService.StartAsync(new CancellationToken(true));

            //Assert
            Assert.IsInstanceOfType(createdJob.Exception, typeof(StageUpdateException));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task StartAsync_UpdatingThrowsStateUpdateException_SetsExceptionOnJob()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var createdJob = await provisioningService.ScheduleJob(Substitute.For<IProvisioningStageFlow>());

            var fakeJobState = Substitute.For<IProvisioningStage>();
            fakeJobState
                .UpdateAsync()
                .Throws(new StageUpdateException("Test error"));

            createdJob.CurrentStage = fakeJobState;

            //Act
            await provisioningService.StartAsync(new CancellationToken(true));

            //Assert
            Assert.IsInstanceOfType(createdJob.Exception, typeof(StageUpdateException));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task StartAsync_JobAvailableWithFinishedState_UpdatesJobStateToCompletedState()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var fakeJobState = Substitute.For<IProvisioningStage>();
            fakeJobState
                .UpdateAsync()
                .Returns(ProvisioningStateUpdateResult.Succeeded);

            var fakeFlow = Substitute.For<IProvisioningStageFlow>();
            fakeFlow
                .GetInitialState(Arg.Any<InitialStateContext>())
                .Returns(fakeJobState);

            fakeFlow
                .GetNextState(Arg.Any<NextStageContext>())
                .Returns((IProvisioningStage)null);

            var createdJob = await provisioningService.ScheduleJob(fakeFlow);

            //Act
            await provisioningService.StartAsync(new CancellationToken(true));

            //Assert
            Assert.IsTrue(createdJob.IsSucceeded);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task StartAsync_InitialJobAvailable_InitializesInitialJob()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var startCancellationTokenSource = new CancellationTokenSource();

            var fakeInitialState = Substitute.For<IProvisioningStage>();
            fakeInitialState
                .UpdateAsync()
                .Returns(call =>
                {
                    startCancellationTokenSource.Cancel();
                    return ProvisioningStateUpdateResult.Succeeded;
                });

            var fakeFlow = Substitute.For<IProvisioningStageFlow>();
            fakeFlow
                .GetInitialState(Arg.Any<InitialStateContext>())
                .Returns(fakeInitialState);

            fakeFlow
                .GetNextState(Arg.Is<NextStageContext>(args =>
                    args.CurrentStage == fakeInitialState))
                .Returns((IProvisioningStage)null);

            await provisioningService.ScheduleJob(fakeFlow);

            //Act
            await provisioningService.StartAsync(startCancellationTokenSource.Token);

            //Assert
            await fakeInitialState
                .Received(1)
                .InitializeAsync();
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task StartAsync_JobAvailableWithCompletedState_InitializesNextJob()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var startCancellationTokenSource = new CancellationTokenSource();

            var fakeInitialState = Substitute.For<IProvisioningStage>();
            fakeInitialState
                .UpdateAsync()
                .Returns(ProvisioningStateUpdateResult.Succeeded);

            var fakeNextState = Substitute.For<IProvisioningStage>();
            fakeNextState
                .UpdateAsync()
                .Returns(call =>
                {
                    startCancellationTokenSource.Cancel();
                    return ProvisioningStateUpdateResult.Succeeded;
                });

            var fakeFlow = Substitute.For<IProvisioningStageFlow>();
            fakeFlow
                .GetInitialState(Arg.Any<InitialStateContext>())
                .Returns(fakeInitialState);

            fakeFlow
                .GetNextState(Arg.Is<NextStageContext>(args => 
                    args.CurrentStage == fakeInitialState))
                .Returns(
                    fakeNextState);

            fakeFlow
                .GetNextState(Arg.Is<NextStageContext>(args =>
                    args.CurrentStage == fakeNextState))
                .Returns((IProvisioningStage)null);

            await provisioningService.ScheduleJob(fakeFlow);

            //Act
            await provisioningService.StartAsync(startCancellationTokenSource.Token);

            //Assert
            await fakeNextState
                .Received(1)
                .InitializeAsync();
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task StartAsync_JobAvailableWithInProgressState_ReschedulesJob()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var updateCallCount = 0;
            var startCancellationTokenSource = new CancellationTokenSource();

            var fakeJobState = Substitute.For<IProvisioningStage>();
            fakeJobState
                .UpdateAsync()
                .Returns(callInfo => {
                    var updateResults = new[]
                    {
                        ProvisioningStateUpdateResult.InProgress,
                        ProvisioningStateUpdateResult.Succeeded,
                        ProvisioningStateUpdateResult.Succeeded
                    };

                    var updateResult = updateResults[updateCallCount++];
                    if (updateResult == ProvisioningStateUpdateResult.Succeeded)
                        startCancellationTokenSource.Cancel();

                    return updateResult;
                });

            var fakeFlow = Substitute.For<IProvisioningStageFlow>();
            fakeFlow
                .GetInitialState(Arg.Any<InitialStateContext>())
                .Returns(fakeJobState);

            fakeFlow
                .GetNextState(Arg.Any<NextStageContext>())
                .Returns(
                    fakeJobState,
                    (IProvisioningStage)null);

            var createdJob = await provisioningService.ScheduleJob(fakeFlow);

            //Act
            await provisioningService.StartAsync(startCancellationTokenSource.Token);

            //Assert
            Assert.IsTrue(createdJob.IsSucceeded);
        }

        private static void ConfigureFakeLightsailClient(IServiceProvider serviceProvider)
        {
            var fakeLightsailClient = serviceProvider.GetRequiredService<IAmazonLightsail>();
            fakeLightsailClient
                .CreateInstancesAsync(
                    Arg.Is<CreateInstancesRequest>(x => x.BundleId == "some-plan-id"))
                .Returns(new CreateInstancesResponse()
                {
                    Operations = new List<Operation>()
                    {
                        new Operation()
                        {
                            Id = "fake-job-id"
                        }
                    }
                });
        }
    }
}

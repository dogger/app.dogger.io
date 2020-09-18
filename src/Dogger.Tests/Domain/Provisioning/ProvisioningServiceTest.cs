using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Services.Provisioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Tests.TestHelpers;
using NSubstitute.ExceptionExtensions;

namespace Dogger.Tests.Domain.Provisioning
{
    [TestClass]
    public class ProvisioningServiceTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task IsProtectedResourceName_ProtectedNameWithLeadingWhitespace_ReturnsTrue()
        {
            //Arrange
            var name = " main-";

            //Act
            var result = ProvisioningService.IsProtectedResourceName(name);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task IsProtectedResourceName_ProtectedNameWithWhitespaceInside_ReturnsTrue()
        {
            //Arrange
            var name = "mai n-lol";

            //Act
            var result = ProvisioningService.IsProtectedResourceName(name);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task IsProtectedResourceName_ProtectedNameWithTrailingWhitespace_ReturnsTrue()
        {
            //Arrange
            var name = "main- ";

            //Act
            var result = ProvisioningService.IsProtectedResourceName(name);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task IsProtectedResourceName_ProtectedNameGiven_ReturnsTrue()
        {
            //Arrange
            var name = "main-lol";

            //Act
            var result = ProvisioningService.IsProtectedResourceName(name);

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task IsProtectedResourceName_JustProtectedNamePrefixGiven_ReturnsTrue()
        {
            //Arrange
            var name = "main-";

            //Act
            var result = ProvisioningService.IsProtectedResourceName(name);

            //Assert
            Assert.IsTrue(result);
        }

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

            var createdJob = await provisioningService.ScheduleJobAsync(Substitute.For<IProvisioningStateFlow>());

            //Act
            var job = await provisioningService.GetJobByIdAsync(createdJob.Id);

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
            var job = await provisioningService.GetJobByIdAsync("non-existing-id");

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

            var fakeInitialState = Substitute.For<IProvisioningState>();

            var fakeFlow = Substitute.For<IProvisioningStateFlow>();
            fakeFlow
                .GetInitialStateAsync(Arg.Any<InitialStateContext>())
                .Returns(fakeInitialState);

            var createdJob = await provisioningService.ScheduleJobAsync(fakeFlow);

            //Act
            var job = await provisioningService.GetJobByIdAsync(createdJob.Id);

            //Assert
            var state = job.CurrentState;
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
        public async Task StartAsync_UpdatingThrowsInvalidOperationException_SetsStateUpdateExceptionOnJob()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var createdJob = await provisioningService.ScheduleJobAsync(Substitute.For<IProvisioningStateFlow>());

            var fakeJobState = Substitute.For<IProvisioningState>();
            fakeJobState
                .UpdateAsync()
                .Throws(new InvalidOperationException("Test error"));

            createdJob.CurrentState = fakeJobState;

            //Act
            await provisioningService.StartAsync(new CancellationToken(true));

            //Assert
            Assert.IsInstanceOfType(createdJob.Exception, typeof(StateUpdateException));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task StartAsync_UpdatingThrowsStateUpdateException_SetsExceptionOnJob()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var createdJob = await provisioningService.ScheduleJobAsync(Substitute.For<IProvisioningStateFlow>());

            var fakeJobState = Substitute.For<IProvisioningState>();
            fakeJobState
                .UpdateAsync()
                .Throws(new StateUpdateException("Test error"));

            createdJob.CurrentState = fakeJobState;

            //Act
            await provisioningService.StartAsync(new CancellationToken(true));

            //Assert
            Assert.IsInstanceOfType(createdJob.Exception, typeof(StateUpdateException));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task StartAsync_JobAvailableWithFinishedState_UpdatesJobStateToCompletedState()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            ConfigureFakeLightsailClient(serviceProvider);

            var provisioningService = serviceProvider.GetRequiredService<IProvisioningService>();

            var fakeJobState = Substitute.For<IProvisioningState>();
            fakeJobState
                .UpdateAsync()
                .Returns(ProvisioningStateUpdateResult.Succeeded);

            var fakeFlow = Substitute.For<IProvisioningStateFlow>();
            fakeFlow
                .GetInitialStateAsync(Arg.Any<InitialStateContext>())
                .Returns(fakeJobState);

            fakeFlow
                .GetNextStateAsync(Arg.Any<NextStateContext>())
                .Returns((IProvisioningState)null);

            var createdJob = await provisioningService.ScheduleJobAsync(fakeFlow);

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

            var fakeInitialState = Substitute.For<IProvisioningState>();
            fakeInitialState
                .UpdateAsync()
                .Returns(call =>
                {
                    startCancellationTokenSource.Cancel();
                    return ProvisioningStateUpdateResult.Succeeded;
                });

            var fakeFlow = Substitute.For<IProvisioningStateFlow>();
            fakeFlow
                .GetInitialStateAsync(Arg.Any<InitialStateContext>())
                .Returns(fakeInitialState);

            fakeFlow
                .GetNextStateAsync(Arg.Is<NextStateContext>(args =>
                    args.CurrentState == fakeInitialState))
                .Returns((IProvisioningState)null);

            await provisioningService.ScheduleJobAsync(fakeFlow);

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

            var fakeInitialState = Substitute.For<IProvisioningState>();
            fakeInitialState
                .UpdateAsync()
                .Returns(ProvisioningStateUpdateResult.Succeeded);

            var fakeNextState = Substitute.For<IProvisioningState>();
            fakeNextState
                .UpdateAsync()
                .Returns(call =>
                {
                    startCancellationTokenSource.Cancel();
                    return ProvisioningStateUpdateResult.Succeeded;
                });

            var fakeFlow = Substitute.For<IProvisioningStateFlow>();
            fakeFlow
                .GetInitialStateAsync(Arg.Any<InitialStateContext>())
                .Returns(fakeInitialState);

            fakeFlow
                .GetNextStateAsync(Arg.Is<NextStateContext>(args => 
                    args.CurrentState == fakeInitialState))
                .Returns(
                    fakeNextState);

            fakeFlow
                .GetNextStateAsync(Arg.Is<NextStateContext>(args =>
                    args.CurrentState == fakeNextState))
                .Returns((IProvisioningState)null);

            await provisioningService.ScheduleJobAsync(fakeFlow);

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

            var fakeJobState = Substitute.For<IProvisioningState>();
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

            var fakeFlow = Substitute.For<IProvisioningStateFlow>();
            fakeFlow
                .GetInitialStateAsync(Arg.Any<InitialStateContext>())
                .Returns(fakeJobState);

            fakeFlow
                .GetNextStateAsync(Arg.Any<NextStateContext>())
                .Returns(
                    fakeJobState,
                    (IProvisioningState)null);

            var createdJob = await provisioningService.ScheduleJobAsync(fakeFlow);

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

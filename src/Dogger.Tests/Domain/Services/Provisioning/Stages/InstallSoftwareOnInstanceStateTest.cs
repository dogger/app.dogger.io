using System;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Domain.Services.Provisioning.Stages.CompleteInstanceSetup;
using Dogger.Domain.Services.Provisioning.Stages.InstallSoftwareOnInstance;
using Dogger.Infrastructure.Ssh;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Services.Provisioning.Stages
{
    [TestClass]
    public class InstallSoftwareOnInstanceStateTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task UpdateAsync_NoIpAddressSet_ThrowsException()
        {
            //Arrange
            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            
            var state = serviceProvider.GetRequiredService<InstallSoftwareOnInstanceStage>();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await state.UpdateAsync());

            //Assert
            Assert.IsNotNull(exception);
        }
        
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task UpdateAsync_IpAddressProvided_CreatesProperSshClient()
        {
            //Arrange
            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();

            var fakeSshClient = Substitute.For<ISshClient>();
            fakeSshClientFactory
                .CreateForLightsailInstanceAsync("ip-address")
                .Returns(fakeSshClient);

            var fakeMediator = Substitute.For<IMediator>();
            
            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStageFactory>();
            fakeProvisioningStateFactory
                .Create<CompleteInstanceSetupStage>()
                .Returns(new CompleteInstanceSetupStage(
                    fakeSshClientFactory,
                    fakeMediator));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services => {
                services.AddSingleton(fakeSshClientFactory);
            });
            
            var state = serviceProvider.GetRequiredService<InstallSoftwareOnInstanceStage>();
            state.IpAddress = "ip-address";

            await state.InitializeAsync();

            //Act
            await state.UpdateAsync();

            //Assert
            await fakeSshClient
                .Received()
                .ExecuteCommandAsync(
                    Arg.Any<SshRetryPolicy>(),
                    Arg.Any<string>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task UpdateAsync_IpAddressProvided_ReturnsCompleted()
        {
            //Arrange
            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();

            var fakeSshClient = Substitute.For<ISshClient>();
            fakeSshClientFactory
                .CreateForLightsailInstanceAsync("ip-address")
                .Returns(fakeSshClient);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStageFactory>();
            fakeProvisioningStateFactory
                .Create<CompleteInstanceSetupStage>()
                .Returns(new CompleteInstanceSetupStage(
                    fakeSshClientFactory,
                    fakeMediator));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services => {
                services.AddSingleton(fakeSshClientFactory);
            });

            var state = serviceProvider.GetRequiredService<InstallSoftwareOnInstanceStage>();
            state.IpAddress = "ip-address";

            await state.InitializeAsync();

            //Act
            var newState = await state.UpdateAsync();

            //Assert
            Assert.AreEqual(ProvisioningStateUpdateResult.Succeeded, newState);
        }
    }
}

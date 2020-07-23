using Dogger.Infrastructure.Ssh;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Domain.Services.Provisioning.States.CompleteInstanceSetup;
using Dogger.Domain.Services.Provisioning.States.InstallSoftwareOnInstance;
using Dogger.Tests.TestHelpers;
using MediatR;

namespace Dogger.Tests.Domain.Provisioning.States
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
            
            var state = serviceProvider.GetRequiredService<InstallSoftwareOnInstanceState>();

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
            
            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStateFactory>();
            fakeProvisioningStateFactory
                .Create<CompleteInstanceSetupState>()
                .Returns(new CompleteInstanceSetupState(
                    fakeSshClientFactory,
                    fakeMediator));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services => {
                services.AddSingleton(fakeSshClientFactory);
            });
            
            var state = serviceProvider.GetRequiredService<InstallSoftwareOnInstanceState>();
            state.IpAddress = "ip-address";

            await state.InitializeAsync();

            //Act
            await state.UpdateAsync();

            //Assert
            await fakeSshClient
                .Received()
                .ExecuteCommandAsync(
                    Arg.Any<SshRetryPolicy>(),
                    Arg.Any<SshResponseSensitivity>(),
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

            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStateFactory>();
            fakeProvisioningStateFactory
                .Create<CompleteInstanceSetupState>()
                .Returns(new CompleteInstanceSetupState(
                    fakeSshClientFactory,
                    fakeMediator));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services => {
                services.AddSingleton(fakeSshClientFactory);
            });

            var state = serviceProvider.GetRequiredService<InstallSoftwareOnInstanceState>();
            state.IpAddress = "ip-address";

            await state.InitializeAsync();

            //Act
            var newState = await state.UpdateAsync();

            //Assert
            Assert.AreEqual(ProvisioningStateUpdateResult.Succeeded, newState);
        }
    }
}

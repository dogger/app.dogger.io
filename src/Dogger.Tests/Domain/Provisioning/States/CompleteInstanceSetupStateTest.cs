using System.Threading.Tasks;
using Amazon.Lightsail;
using Dogger.Domain.Commands.Instances.RegisterInstanceAsProvisioned;
using Dogger.Domain.Commands.Instances.RegisterInstanceAsProvisioning;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Domain.Services.Provisioning.States.CompleteInstanceSetup;
using Dogger.Infrastructure.Ssh;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Renci.SshNet.Common;

namespace Dogger.Tests.Domain.Provisioning.States
{
    [TestClass]
    public class CompleteInstanceSetupStateTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Initialize_ConnectionCantBeMadeToSsh_ThrowsStateUpdateException()
        {
            //Arrange
            var fakeLightsailClient = Substitute.For<IAmazonLightsail>();

            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();
            fakeSshClientFactory
                .CreateForLightsailInstanceAsync("some-ip-address")
                .Throws(new SshConnectionException());

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeLightsailClient);
                services.AddSingleton(fakeSshClientFactory);
            });

            var state = serviceProvider.GetRequiredService<CompleteInstanceSetupState>();
            state.IpAddress = "some-ip-address";

            //Act
            var exception = await Assert.ThrowsExceptionAsync<StateUpdateException>(async () =>
                await state.InitializeAsync());

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Update_StateCompleted_PublishesServerProvisionedEvent()
        {
            //Arrange
            var fakeLightsailClient = Substitute.For<IAmazonLightsail>();
            var fakeSshClientFactory = Substitute.For<ISshClientFactory>();
            var fakeMediator = Substitute.For<IMediator>();

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeMediator);
                services.AddSingleton(fakeLightsailClient);
                services.AddSingleton(fakeSshClientFactory);
            });

            var state = serviceProvider.GetRequiredService<CompleteInstanceSetupState>();
            state.IpAddress = "some-ip-address";
            state.InstanceName = "some-instance-name";

            await state.InitializeAsync();

            //Act
            await state.UpdateAsync();

            //Assert
            await fakeMediator
                .Received(1)
                .Send(
                    Arg.Is<RegisterInstanceAsProvisionedCommand>(arg => 
                        arg.InstanceName == "some-instance-name"),
                    default);
        }

    }
}

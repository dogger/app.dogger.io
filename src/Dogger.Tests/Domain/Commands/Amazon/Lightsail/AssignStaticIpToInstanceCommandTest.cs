using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Amazon.Lightsail.AssignStaticIpToInstance;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Commands.Amazon.Lightsail
{
    [TestClass]
    public class AssignStaticIpToInstanceCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ProperArgumentsGiven_OperationIsAwaited()
        {
            //Arrange
            var fakeOperations = new List<Operation>
            {
                new Operation() {Id = "some-operation-id-1"},
                new Operation() {Id = "some-operation-id-2"}
            };

            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .AttachStaticIpAsync(
                    Arg.Any<AttachStaticIpRequest>(),
                    default)
                .Returns(new AttachStaticIpResponse()
                {
                    Operations = fakeOperations
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            var handler = new AssignStaticIpToInstanceCommandHandler(
                fakeAmazonLightsailClient,
                fakeLightsailOperationService);

            //Act
            await handler.Handle(
                new AssignStaticIpToInstanceCommand(
                    "some-instance-name",
                    "static-ip-name"),
                default);

            //Assert
            await fakeLightsailOperationService
                .Received(1)
                .WaitForOperationsAsync(fakeOperations);
        }
    }
}

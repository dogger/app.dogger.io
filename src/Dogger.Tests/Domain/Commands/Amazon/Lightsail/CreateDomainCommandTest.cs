using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Amazon.Lightsail.CreateDomain;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Commands.Amazon.Lightsail
{
    [TestClass]
    public class CreateDomainCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ProperArgumentsGiven_OperationIsAwaited()
        {
            //Arrange
            var fakeOperation = new Operation() {Id = "some-operation-id-1"};

            var fakeAmazonLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsailClient
                .CreateDomainAsync(
                    Arg.Any<CreateDomainRequest>(),
                    default)
                .Returns(new CreateDomainResponse()
                {
                    Operation = fakeOperation
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();

            var handler = new CreateDomainCommandHandler(
                fakeAmazonLightsailClient,
                fakeLightsailOperationService);

            //Act
            await handler.Handle(
                new CreateDomainCommand("some-host-name"),
                default);

            //Assert
            await fakeLightsailOperationService
                .Received(1)
                .WaitForOperationsAsync(fakeOperation);
        }
    }
}

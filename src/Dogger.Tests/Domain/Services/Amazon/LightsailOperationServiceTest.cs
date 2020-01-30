using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Infrastructure.Time;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Services.Amazon
{
    [TestClass]
    public class LightsailOperationServiceTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task WaitForOperations_OneOperationGiven_WaitsUntilCompletion()
        {
            //Arrange
            var fakeLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeLightsailClient
                .GetOperationAsync(Arg.Is<GetOperationRequest>(arg =>
                    arg.OperationId == "some-operation-id"))
                .Returns(
                    GetOperationResponseWithStatus(OperationStatus.Started),
                    GetOperationResponseWithStatus(OperationStatus.Succeeded));

            var fakeTime = Substitute.For<ITime>();

            var lightsailOperationService = new LightsailOperationService(
                fakeLightsailClient,
                fakeTime);

            //Act
            await lightsailOperationService.WaitForOperationsAsync(new List<Operation>()
            {
                GetOperationWithStatus(OperationStatus.NotStarted)
            });

            //Assert
            await fakeTime
                .Received(2)
                .WaitAsync(Arg.Any<int>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task WaitForOperations_NoOperationGiven_ReturnsInstantly()
        {
            //Arrange
            var fakeLightsailClient = Substitute.For<IAmazonLightsail>();

            var fakeTime = Substitute.For<ITime>();

            var lightsailOperationService = new LightsailOperationService(
                fakeLightsailClient,
                fakeTime);

            //Act
            await lightsailOperationService.WaitForOperationsAsync();

            //Assert
            await fakeTime
                .DidNotReceive()
                .WaitAsync(Arg.Any<int>());

            await fakeLightsailClient
                .DidNotReceive()
                .GetOperationAsync(
                    Arg.Any<GetOperationRequest>(),
                    default);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task WaitForOperations_OperationEventuallyFails_ThrowsLightsailOperationsException()
        {
            //Arrange
            var fakeLightsailClient = Substitute.For<IAmazonLightsail>();
            fakeLightsailClient
                .GetOperationAsync(Arg.Is<GetOperationRequest>(arg =>
                    arg.OperationId == "some-operation-id"))
                .Returns(
                    GetOperationResponseWithStatus(OperationStatus.Started),
                    GetOperationResponseWithStatus(OperationStatus.Failed));

            var fakeTime = Substitute.For<ITime>();

            var lightsailOperationService = new LightsailOperationService(
                fakeLightsailClient,
                fakeTime);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<LightsailOperationsException>(async () => 
                await lightsailOperationService.WaitForOperationsAsync(new List<Operation>()
                {
                    GetOperationWithStatus(OperationStatus.NotStarted)
                }));

            //Assert
            Assert.IsNotNull(exception);
        }

        private static GetOperationResponse GetOperationResponseWithStatus(OperationStatus status)
        {
            return new GetOperationResponse()
            {
                Operation = GetOperationWithStatus(status)
            };
        }

        private static Operation GetOperationWithStatus(OperationStatus status)
        {
            return new Operation()
            {
                Id = "some-operation-id",
                Status = status
            };
        }
    }
}

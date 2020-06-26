using System;
using System.Threading.Tasks;
using Dogger.Controllers.Jobs;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Infrastructure;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Controllers
{
    [TestClass]
    public class JobsControllerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Status_JobExists_ReturnsJobStatus()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var fakeServiceScope = Substitute.For<IServiceScope>();

            var fakeServiceProvider = Substitute.For<IServiceProvider>();
            fakeServiceProvider
                .GetService(typeof(IMediator))
                .Returns(fakeMediator);

            fakeServiceScope.ServiceProvider.Returns(fakeServiceProvider);

            var fakeProvisioningStateFlow = Substitute.For<IProvisioningStateFlow>();

            var provisioningJob = new ProvisioningJob(
                fakeProvisioningStateFlow,
                fakeServiceScope);
            await provisioningJob.InitializeAsync();

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            fakeProvisioningService
                .GetJobByIdAsync("some-job-id")
                .Returns(provisioningJob);

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new JobsController(
                mapper,
                fakeProvisioningService);

            //Act
            var status = await controller.Status("some-job-id");

            //Assert
            Assert.IsNotNull(status);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Status_JobDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            fakeProvisioningService
                .GetJobByIdAsync(Arg.Any<string>())
                .Returns((ProvisioningJob)null);

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new JobsController(
                mapper,
                fakeProvisioningService);

            //Act
            var status = await controller.Status("some-job-id");

            //Assert
            Assert.IsNotNull(status);
            Assert.AreEqual(404, status.GetStatusCode());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Status_JobWithStatusResult_ReturnsStatusResult()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var fakeServiceScope = Substitute.For<IServiceScope>();

            var fakeServiceProvider = Substitute.For<IServiceProvider>();
            fakeServiceProvider
                .GetService(typeof(IMediator))
                .Returns(fakeMediator);

            fakeServiceScope.ServiceProvider.Returns(fakeServiceProvider);

            var fakeProvisioningStateFlow = Substitute.For<IProvisioningStateFlow>();

            var provisioningJob = new ProvisioningJob(
                fakeProvisioningStateFlow,
                fakeServiceScope)
            {
                Exception = new StateUpdateException("dummy", new ConflictResult())
            };
            await provisioningJob.InitializeAsync();

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            fakeProvisioningService
                .GetJobByIdAsync("some-job-id")
                .Returns(provisioningJob);

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new JobsController(
                mapper,
                fakeProvisioningService);

            //Act
            var status = await controller.Status("some-job-id");

            //Assert
            Assert.IsNotNull(status);
            Assert.AreEqual(StatusCodes.Status409Conflict, status.GetStatusCode());
        }
    }
}

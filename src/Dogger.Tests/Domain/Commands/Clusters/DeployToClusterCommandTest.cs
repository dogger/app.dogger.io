using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.DeployToCluster;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Clusters.GetClusterById;
using Dogger.Domain.Queries.Clusters.GetClusterForUser;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Commands.Clusters
{
    [TestClass]
    public class DeployToClusterCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NotAuthenticatedAndClusterFound_DeploysToCluster()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetClusterByIdQuery>())
                .Returns(new TestClusterBuilder()
                    .WithInstances(new Instance()
                    {
                        Name = "some-instance-name"
                    })
                    .Build());

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var handler = new DeployToClusterCommandHandler(
                fakeProvisioningService,
                fakeMediator);

            //Assert
            await handler.Handle(new DeployToClusterCommand(Array.Empty<string>())
            {
                UserId = null,
                ClusterId = Guid.NewGuid()
            }, default);

            //Assert
            await fakeProvisioningService
                .Received(1)
                .ScheduleJobAsync(Arg.Is<DeployToClusterStateFlow>(args =>
                    args.InstanceName == "some-instance-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_AuthenticatedAndDifferentAuthenticatedClusterFound_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetClusterByIdQuery>())
                .Returns(new TestClusterBuilder()
                    .WithInstances(new Instance()
                    {
                        Name = "some-instance-name"
                    })
                    .Build());

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var handler = new DeployToClusterCommandHandler(
                fakeProvisioningService,
                fakeMediator);

            //Assert
            var exception = await Assert.ThrowsExceptionAsync<NotAuthorizedToAccessClusterException>(async () =>
                await handler.Handle(new DeployToClusterCommand(Array.Empty<string>())
                {
                    UserId = Guid.NewGuid(),
                    ClusterId = Guid.NewGuid()
                }, default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_AuthenticatedAndAnonymousClusterFound_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetClusterByIdQuery>())
                .Returns(new TestClusterBuilder()
                    .WithInstances(new Instance()
                    {
                        Name = "some-instance-name"
                    })
                    .Build());

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var handler = new DeployToClusterCommandHandler(
                fakeProvisioningService,
                fakeMediator);

            //Assert
            var exception = await Assert.ThrowsExceptionAsync<NotAuthorizedToAccessClusterException>(async () =>
                await handler.Handle(new DeployToClusterCommand(Array.Empty<string>())
                {
                    UserId = Guid.NewGuid(),
                    ClusterId = Guid.NewGuid()
                }, default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NotAuthenticatedAndAuthenticatedClusterFound_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetClusterByIdQuery>())
                .Returns(new TestClusterBuilder()
                    .WithUser(Guid.NewGuid())
                    .WithInstances(new Instance()
                    {
                        Name = "some-instance-name"
                    })
                    .Build());

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var handler = new DeployToClusterCommandHandler(
                fakeProvisioningService,
                fakeMediator);

            //Assert
            var exception = await Assert.ThrowsExceptionAsync<NotAuthorizedToAccessClusterException>(async () =>
                await handler.Handle(new DeployToClusterCommand(Array.Empty<string>())
                {
                    UserId = null,
                    ClusterId = Guid.NewGuid()
                }, default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NotAuthenticatedAndNoDemoClusterFound_ThrowsClusterNotFoundException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureClusterWithIdCommand>())
                .Returns((Cluster)null);



            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var handler = new DeployToClusterCommandHandler(
                fakeProvisioningService,
                fakeMediator);

            //Assert
            var exception = await Assert.ThrowsExceptionAsync<ClusterNotFoundException>(async () =>
                await handler.Handle(new DeployToClusterCommand(Array.Empty<string>())
                {
                    UserId = null,
                    ClusterId = Guid.NewGuid()
                }, default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_AuthenticatedAndClusterFound_DeploysToDemoCluster()
        {
            //Arrange
            var fakeUserId = Guid.NewGuid();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetClusterForUserQuery>())
                .Returns(new TestClusterBuilder()
                    .WithUser(fakeUserId)
                    .WithInstances(new Instance()
                    {
                        Name = "some-instance-name"
                    })
                    .Build());

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var handler = new DeployToClusterCommandHandler(
                fakeProvisioningService,
                fakeMediator);

            //Assert
            await handler.Handle(new DeployToClusterCommand(Array.Empty<string>())
            {
                UserId = fakeUserId
            }, default);

            //Assert
            await fakeProvisioningService
                .Received(1)
                .ScheduleJobAsync(Arg.Is<DeployToClusterStateFlow>(args =>
                    args.InstanceName == "some-instance-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_AuthenticatedAndNoClusterFound_ThrowsClusterNotFoundException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetClusterForUserQuery>())
                .Returns((Cluster)null);

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var handler = new DeployToClusterCommandHandler(
                fakeProvisioningService,
                fakeMediator);

            //Assert
            var exception = await Assert.ThrowsExceptionAsync<ClusterNotFoundException>(async () =>
                await handler.Handle(new DeployToClusterCommand(Array.Empty<string>())
                {
                    UserId = Guid.NewGuid()
                }, default));

            //Assert
            Assert.IsNotNull(exception);
        }
    }
}

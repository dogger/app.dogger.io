using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Dogger.Controllers.Clusters;
using Dogger.Domain.Commands.Amazon.ElasticContainerRegistry.EnsureRepositoryWithName;
using Dogger.Domain.Commands.Clusters.DeployToCluster;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryLoginByRepositoryName;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Queries.Clusters.GetClusterForUser;
using Dogger.Domain.Queries.Clusters.GetConnectionDetails;
using Dogger.Domain.Queries.Instances.GetContainerLogs;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using Dogger.Domain.Queries.Instances.GetProvisionedClustersWithInstancesForUser;
using Dogger.Infrastructure;
using Dogger.Infrastructure.Docker.Engine;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ConnectionDetailsResponse = Dogger.Controllers.Clusters.ConnectionDetailsResponse;
using Instance = Amazon.Lightsail.Model.Instance;

namespace Dogger.Tests.Controllers
{
    [TestClass]
    public class ClustersControllerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Get_OneInstanceFound_InstanceReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Is<EnsureUserForIdentityCommand>(
                        args => args.IdentityName == "some-identity-name"), 
                    default)
                .Returns(new User());

            fakeMediator
                .Send(Arg.Any<GetProvisionedClustersWithInstancesForUserQuery>(), default)
                .Returns(new[]
                {
                    RandomObjectFactory.Create<UserClusterResponse>()
                });

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.Get();

            //Assert
            var clusters = result.ToArray();
            Assert.AreEqual(1, clusters.Length);
            Assert.AreEqual(1, clusters.Single().Instances.Count());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task DemoConnectionDetails_DemoInstanceAvailable_DemoInstanceConnectionDetailsReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Is<GetConnectionDetailsQuery>(arg =>
                        arg.ClusterId == "demo"),
                    default)
                .Returns(new Dogger.Domain.Queries.Clusters.GetConnectionDetails.ConnectionDetailsResponse(
                    "some-ip-address",
                    "some-host-name",
                    Array.Empty<ExposedPort>()));

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);

            //Act
            var result = await controller.DemoConnectionDetails();

            //Assert
            var response = result.ToObject<ConnectionDetailsResponse>();
            Assert.IsNotNull(response);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task DeployDemo_ClusterIsNull_ClusterNotFoundValidationError()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<DeployToClusterCommand>())
                .Throws(new ClusterNotFoundException());

            fakeMediator
                .Send(Arg.Any<GetRepositoryLoginForUserQuery>())
                .Returns(new RepositoryLoginResponse(
                    "dummy",
                    "dummy"));

            fakeMediator
                .Send(Arg.Any<EnsureRepositoryWithNameCommand>())
                .Returns(new RepositoryResponse(
                    "dummy",
                    "dummy",
                    new AmazonUser(),
                    new AmazonUser()));

            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new User());

            fakeMediator
                .Send(Arg.Any<EnsureClusterWithIdCommand>())
                .Returns(new Cluster());

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);

            //Act
            var result = await controller.DeployDemo(new DeployToClusterRequest()
            {
                DockerComposeYmlContents = new[] { "some-docker-compose-contents" }
            });

            //Assert
            var response = result.GetValidationProblemDetails();
            Assert.IsNotNull(response);

            Assert.AreEqual("CLUSTER_NOT_FOUND", response.Type);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task DeployDemo_DeployingYieldsNotAuthorizedException_NotAuthorizedValidationError()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<DeployToClusterCommand>())
                .Throws(new NotAuthorizedToAccessClusterException());

            fakeMediator
                .Send(Arg.Any<GetRepositoryLoginForUserQuery>())
                .Returns(new RepositoryLoginResponse(
                    "dummy",
                    "dummy"));

            fakeMediator
                .Send(Arg.Any<EnsureRepositoryWithNameCommand>())
                .Returns(new RepositoryResponse(
                    "dummy",
                    "dummy",
                    new AmazonUser(), 
                    new AmazonUser()));

            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new User());

            fakeMediator
                .Send(Arg.Any<EnsureClusterWithIdCommand>())
                .Returns(new Cluster());

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);

            //Act
            var result = await controller.DeployDemo(new DeployToClusterRequest()
            {
                DockerComposeYmlContents = new[] { "some-docker-compose-contents" }
            });

            //Assert
            var response = result.GetValidationProblemDetails();
            Assert.IsNotNull(response);

            Assert.AreEqual("NOT_AUTHORIZED", response.Type);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Destroy_NoClusterIdGiven_DeleteInstanceByNameFiredWithClusterForUser()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new User());

            fakeMediator
                .Send(Arg.Is<GetClusterForUserQuery>(args =>
                    args.ClusterId == default))
                .Returns(new Cluster()
                {
                    Instances = new List<Dogger.Domain.Models.Instance>()
                    {
                        new Dogger.Domain.Models.Instance()
                        {
                            Name = "some-instance-name"
                        }
                    }
                });

            var fakeMapper = Substitute.For<IMapper>();

            var controller = new ClustersController(
                fakeMediator,
                fakeMapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.Destroy();

            //Assert
            Assert.IsNotNull(result);

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeleteInstanceByNameCommand>(args =>
                    args.Name == "some-instance-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Destroy_ClusterIdGiven_DeleteInstanceByNameFiredWithClusterForUserAndClusterId()
        {
            //Arrange
            var fakeClusterId = Guid.NewGuid();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new User());

            fakeMediator
                .Send(Arg.Is<GetClusterForUserQuery>(args =>
                    args.ClusterId == fakeClusterId))
                .Returns(new Cluster()
                {
                    Instances = new List<Dogger.Domain.Models.Instance>()
                    {
                        new Dogger.Domain.Models.Instance()
                        {
                            Name = "some-instance-name"
                        }
                    }
                });

            var fakeMapper = Substitute.For<IMapper>();

            var controller = new ClustersController(
                fakeMediator,
                fakeMapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.Destroy(fakeClusterId);

            //Assert
            Assert.IsNotNull(result);

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeleteInstanceByNameCommand>(args =>
                    args.Name == "some-instance-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Destroy_ClusterNotFound_ClusterNotFoundValidationErrorReturned()
        {
            //Arrange
            var fakeClusterId = Guid.NewGuid();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetClusterForUserQuery>(args =>
                    args.ClusterId == fakeClusterId))
                .Returns((Cluster)null);

            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new User());

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.Destroy(fakeClusterId);

            //Assert
            var response = result.GetValidationProblemDetails();
            Assert.IsNotNull(response);

            Assert.AreEqual("CLUSTER_NOT_FOUND", response.Type);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Deploy_NoClusterIdGiven_DeployToClusterCommandFiredNoClusterId()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new User());

            fakeMediator
                .Send(Arg.Any<GetRepositoryLoginForUserQuery>())
                .Returns(new RepositoryLoginResponse(
                    "dummy",
                    "dummy"));

            fakeMediator
                .Send(Arg.Any<EnsureRepositoryWithNameCommand>())
                .Returns(new RepositoryResponse(
                    "dummy",
                    "dummy",
                    new AmazonUser(),
                    new AmazonUser()));

            var fakeMapper = Substitute.For<IMapper>();

            var controller = new ClustersController(
                fakeMediator,
                fakeMapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.Deploy(new DeployToClusterRequest()
            {
                DockerComposeYmlContents = new[] { "some-docker-compose-contents" }
            });

            //Assert
            Assert.IsNotNull(result);

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeployToClusterCommand>(args =>
                    args.ClusterId == default));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Deploy_UserIdGiven_DeployToClusterCommandFiredWithAuthorizationFromRepository()
        {
            //Arrange
            var readUser = new AmazonUser();
            var writeUser = new AmazonUser();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new User());

            fakeMediator
                .Send(Arg.Is<GetRepositoryLoginForUserQuery>(arg => 
                    arg.AmazonUser == readUser))
                .Returns(new RepositoryLoginResponse(
                    "some-username",
                    "some-password"));

            fakeMediator
                .Send(Arg.Is<EnsureRepositoryWithNameCommand>(arg => arg.UserId != null))
                .Returns(new RepositoryResponse(
                    "dummy",
                    "some-host-name",
                    readUser,
                    writeUser));

            var fakeMapper = Substitute.For<IMapper>();

            var controller = new ClustersController(
                fakeMediator,
                fakeMapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.Deploy(new DeployToClusterRequest()
            {
                DockerComposeYmlContents = new[] { "some-docker-compose-contents" }
            });

            //Assert
            Assert.IsNotNull(result);

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeployToClusterCommand>(args =>
                    args.Authentication.Single().Username == "some-username" &&
                    args.Authentication.Single().Password == "some-password" &&
                    args.Authentication.Single().RegistryHostName == "some-host-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Deploy_ClusterNotFoundExceptionThrown_ClusterNotFoundValidationErrorReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<DeployToClusterCommand>())
                .Throws(new ClusterNotFoundException());

            fakeMediator
                .Send(Arg.Any<GetRepositoryLoginForUserQuery>())
                .Returns(new RepositoryLoginResponse(
                    "dummy",
                    "dummy"));

            fakeMediator
                .Send(Arg.Any<EnsureRepositoryWithNameCommand>())
                .Returns(new RepositoryResponse(
                    "dummy",
                    "dummy",
                    new AmazonUser(),
                    new AmazonUser()));

            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new User());

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.Deploy(new DeployToClusterRequest()
            {
                DockerComposeYmlContents = new[] { "some-docker-compose-contents" }
            });

            //Assert
            var response = result.GetValidationProblemDetails();
            Assert.IsNotNull(response);

            Assert.AreEqual("CLUSTER_NOT_FOUND", response.Type);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Deploy_ClusterQueryTooBroadExceptionThrown_TooBroadValidationErrorReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<DeployToClusterCommand>())
                .Throws(new ClusterQueryTooBroadException("dummy"));

            fakeMediator
                .Send(Arg.Any<GetRepositoryLoginForUserQuery>())
                .Returns(new RepositoryLoginResponse(
                    "dummy",
                    "dummy"));

            fakeMediator
                .Send(Arg.Any<EnsureRepositoryWithNameCommand>())
                .Returns(new RepositoryResponse(
                    "dummy",
                    "dummy",
                    new AmazonUser(),
                    new AmazonUser()));

            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new User());

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.Deploy(new DeployToClusterRequest()
            {
                DockerComposeYmlContents = new[] { "some-docker-compose-contents" }
            });

            //Assert
            var response = result.GetValidationProblemDetails();
            Assert.IsNotNull(response);

            Assert.AreEqual("TOO_BROAD", response.Type);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ConnectionDetails_InstanceWithProtectedName_UnauthorizedReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Is<GetLightsailInstanceByNameQuery>(arg =>
                        arg.Name == "main-some-instance-name"),
                    default)
                .Returns((Instance)null);

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("main-some-identity-name");

            //Act
            var result = await controller.ConnectionDetails("main-some-instance-name");

            //Assert
            var response = result as UnauthorizedObjectResult;
            Assert.IsNotNull(response);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ConnectionDetails_InstanceNotAvailable_NotFoundReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Is<GetLightsailInstanceByNameQuery>(arg =>
                        arg.Name == "some-instance-name"), 
                    default)
                .Returns((Instance)null);

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.ConnectionDetails("some-instance-name");

            //Assert
            var response = result as NotFoundObjectResult;
            Assert.IsNotNull(response);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ConnectionDetails_ConnectionDetailsPresentOnInstance_ConnectionDetailsReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Is<GetConnectionDetailsQuery>(arg =>
                        arg.ClusterId == "some-instance-name"),
                    default)
                .Returns(new Dogger.Domain.Queries.Clusters.GetConnectionDetails.ConnectionDetailsResponse(
                    "some-ip-address",
                    "some-host-name",
                    new []
                    {
                        new ExposedPort()
                        {
                            Port = 23,
                            Protocol = SocketProtocol.Tcp
                        },
                        new ExposedPort()
                        {
                            Port = 25,
                            Protocol = SocketProtocol.Udp
                        }
                    }));

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.ConnectionDetails("some-instance-name");

            //Assert
            var response = result.ToObject<ConnectionDetailsResponse>();
            Assert.IsNotNull(response);

            var allPorts = response.Ports.ToArray();
            Assert.AreEqual(2, allPorts.Length);

            Assert.IsTrue(allPorts.Any(x => x.Port == 23 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(allPorts.Any(x => x.Port == 25 && x.Protocol == SocketProtocol.Udp));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task DemoLogs_DemoInstanceAvailable_LogsForDemoInstanceReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetContainerLogsQuery>(arg => arg.InstanceName == "demo"))
                .Returns(new[]
                {
                    new ContainerLogsResponse(
                        new ContainerResponse()
                        {
                            Id = "some-id",
                            Names = new[]
                            {
                                "some-name"
                            },
                            Image = "some-image"
                        },
                        "some-logs")
                });

            fakeMediator
                .Send(
                    Arg.Is<GetInstanceByNameQuery>(arg =>
                        arg.Name == "demo"),
                    default)
                .Returns(new Dogger.Domain.Models.Instance());

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);

            //Act
            var result = await controller.DemoLogs();

            //Assert
            var response = result.ToObject<LogsResponse[]>();
            Assert.IsNotNull(response);

            Assert.AreEqual(1, response.Length);

            var logsResponse = response.Single();
            Assert.AreEqual("some-id", logsResponse.ContainerId);
            Assert.AreEqual("some-image", logsResponse.ContainerImage);
            Assert.AreEqual("some-logs", logsResponse.Logs);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Logs_InstanceAvailable_LogsForInstanceReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetContainerLogsQuery>(arg => arg.InstanceName == "some-instance-name"))
                .Returns(new[]
                {
                    new ContainerLogsResponse(
                        new ContainerResponse()
                        {
                            Id = "some-id",
                            Names = new[]
                            {
                                "some-name"
                            },
                            Image = "some-image"
                        },
                        "some-logs")
                });

            fakeMediator
                .Send(
                    Arg.Is<GetInstanceByNameQuery>(arg =>
                        arg.Name == "some-instance-name"),
                    default)
                .Returns(new Dogger.Domain.Models.Instance());

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);

            //Act
            var result = await controller.Logs("some-instance-name");

            //Assert
            var response = result.ToObject<LogsResponse[]>();
            Assert.IsNotNull(response);

            Assert.AreEqual(1, response.Length);

            var logsResponse = response.Single();
            Assert.AreEqual("some-id", logsResponse.ContainerId);
            Assert.AreEqual("some-image", logsResponse.ContainerImage);
            Assert.AreEqual("some-logs", logsResponse.Logs);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Logs_InstanceNotAvailable_NotFoundReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Any<GetInstanceByNameQuery>(),
                    default)
                .Returns((Dogger.Domain.Models.Instance)null);

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);

            //Act
            var result = await controller.Logs("some-instance-name");

            //Assert
            var response = result as NotFoundObjectResult;
            Assert.IsNotNull(response);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Logs_ProtectedInstanceProvided_UnauthorizedReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new ClustersController(
                fakeMediator,
                mapper);

            //Act
            var result = await controller.Logs("main-protected");

            //Assert
            var response = result as UnauthorizedObjectResult;
            Assert.IsNotNull(response);
        }
    }
}

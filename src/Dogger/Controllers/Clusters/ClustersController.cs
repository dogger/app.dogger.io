using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Dogger.Domain.Commands.Amazon.ElasticContainerRegistry.EnsureRepositoryWithName;
using Dogger.Domain.Commands.Clusters.DeployToCluster;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryLoginByRepositoryName;
using Dogger.Domain.Queries.Clusters.GetClusterForUser;
using Dogger.Domain.Queries.Clusters.GetConnectionDetails;
using Dogger.Domain.Queries.Instances.GetContainerLogs;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using Dogger.Domain.Queries.Instances.GetProvisionedClustersWithInstancesForUser;
using Dogger.Domain.Services.Dogfeeding;
using Dogger.Domain.Services.Provisioning.Arguments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Controllers.Clusters
{
    [ApiController]
    [Route("api/clusters")]
    public class ClustersController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;

        public ClustersController(
            IMediator mediator,
            IMapper mapper)
        {
            this.mediator = mediator;
            this.mapper = mapper;
        }

        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<ClusterResponse>> Get()
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(User));
            var instances = await this.mediator.Send(new GetProvisionedClustersWithInstancesForUserQuery(user.Id));
            
            return this.mapper
                .ProjectTo<ClusterResponse>(instances
                    .AsQueryable())
                .ToArray();
        }

        [HttpPost]
        [Route("demo/deploy")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(DeployToClusterResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeployDemo([FromBody] DeployToClusterRequest request)
        {
            var cluster = await this.mediator.Send(new EnsureClusterWithIdCommand(DataContext.DemoClusterId));

            var authenticatedUser = User?.Identity?.Name != null ?
                await this.mediator.Send(new EnsureUserForIdentityCommand(User)) :
                null;

            return await HandleDeploymentAsync(
                request, 
                authenticatedUser?.Id, 
                clusterId: cluster.Id);
        }

        [HttpPost]
        [Route("deploy")]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(DeployToClusterResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Deploy([FromBody] DeployToClusterRequest request)
        {
            return await Deploy(clusterId: null, request);
        }

        [HttpPost]
        [Route("{clusterId}/deploy")]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(DeployToClusterResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Deploy(Guid? clusterId, [FromBody] DeployToClusterRequest request)
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(User));
            return await HandleDeploymentAsync(request, user.Id, clusterId);
        }

        [HttpPost]
        [Route("destroy")]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Destroy()
        {
            return await Destroy(clusterId: null);
        }

        [HttpPost]
        [Route("{clusterId}/destroy")]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Destroy(Guid? clusterId)
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(User));

            var cluster = await this.mediator.Send(new GetClusterForUserQuery(user.Id)
            {
                ClusterId = clusterId
            });
            if (cluster == null)
            {
                return ValidationProblem(new ValidationProblemDetails()
                {
                    Type = "CLUSTER_NOT_FOUND"
                });
            }

            await this.mediator.Send(new DeleteInstanceByNameCommand(cluster.Instances.Single().Name));

            return Ok();
        }

        private async Task<IActionResult> HandleDeploymentAsync(
            DeployToClusterRequest request, 
            Guid? userId, 
            Guid? clusterId)
        {
            try
            {
                var repositoryName = userId == null ? "demo" : userId.Value.ToString();
                var repositoryResponse = await this.mediator.Send(new EnsureRepositoryWithNameCommand(repositoryName)
                {
                    UserId = userId
                });
                var login = await this.mediator.Send(new GetRepositoryLoginForUserQuery(repositoryResponse.ReadUser));

                var job = await this.mediator.Send(new DeployToClusterCommand(
                    request.DockerComposeYmlFilePaths)
                {
                    ClusterId = clusterId,
                    UserId = userId,
                    Files = request.Files,
                    Authentication = new []
                    {
                        new DockerAuthenticationArguments(
                            login.Username,
                            login.Password)
                        {
                            RegistryHostName = repositoryResponse.HostName
                        }
                    }
                });

                return Ok(this.mapper.Map<DeployToClusterResponse>(job));
            }
            catch (NotAuthorizedToAccessClusterException)
            {
                return ValidationProblem(new ValidationProblemDetails()
                {
                    Type = "NOT_AUTHORIZED"
                });
            }
            catch (ClusterNotFoundException)
            {
                return ValidationProblem(new ValidationProblemDetails()
                {
                    Type = "CLUSTER_NOT_FOUND"
                });
            }
            catch (ClusterQueryTooBroadException)
            {
                return ValidationProblem(new ValidationProblemDetails()
                {
                    Type = "TOO_BROAD"
                });
            }
        }

        [HttpGet]
        [Route("demo/connection-details")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ConnectionDetailsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> DemoConnectionDetails()
        {
            return await ConnectionDetails("demo");
        }

        [HttpGet]
        [Route("{clusterId}/connection-details")]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ConnectionDetailsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConnectionDetails(string clusterId)
        {
            if(DogfeedService.IsProtectedResourceName(clusterId))
                return Unauthorized("Can't get connection details for reserved instances.");

            var connectionDetails = await this.mediator.Send(new GetConnectionDetailsQuery(clusterId));
            if (connectionDetails == null)
                return NotFound("Instance not found.");

            return Ok(mapper.Map<ConnectionDetailsResponse>(connectionDetails));
        }

        [HttpGet]
        [Route("demo/logs")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(LogsResponse[]), StatusCodes.Status200OK)]
        public async Task<IActionResult> DemoLogs()
        {
            return await Logs("demo");
        }

        [HttpGet]
        [Route("{clusterId}/logs")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(LogsResponse[]), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logs(string clusterId)
        {
            if(DogfeedService.IsProtectedResourceName(clusterId))
                return Unauthorized("Can't get logs for reserved instances.");

            var instance = await this.mediator.Send(new GetInstanceByNameQuery(clusterId));
            if (instance == null)
                return NotFound("Instance not found.");

            var logResponses = await this.mediator.Send(new GetContainerLogsQuery(clusterId) {
                LinesToReturnPerContainer = 8
            });

            var responses = this.mapper
                .ProjectTo<LogsResponse>(
                    logResponses.AsQueryable())
                .ToArray();
            return Ok(responses);
        }
    }

}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using Dogger.Infrastructure.Docker.Yml;
using MediatR;

namespace Dogger.Domain.Queries.Instances.GetNecessaryInstanceFirewallPorts
{
    public class GetNecessaryInstanceFirewallPortsQueryHandler : IRequestHandler<GetNecessaryInstanceFirewallPortsQuery, IReadOnlyCollection<ExposedPortRange>>
    {
        private readonly IMediator mediator;

        public GetNecessaryInstanceFirewallPortsQueryHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<IReadOnlyCollection<ExposedPortRange>> Handle(GetNecessaryInstanceFirewallPortsQuery request, CancellationToken cancellationToken)
        {
            var necessaryPorts = new HashSet<ExposedPortRange>();
            AddPorts(necessaryPorts, GetSshPort());

            var instance = await this.mediator.Send(
                new GetInstanceByNameQuery(request.InstanceName),
                cancellationToken);

            if (instance?.Type == InstanceType.KubernetesControlPlane)
                AddPorts(necessaryPorts, GetKubernetesControlPlanePorts());

            if (instance?.Type == InstanceType.KubernetesWorker)
                AddPorts(necessaryPorts, GetKubernetesWorkerNodePorts());

            return necessaryPorts;
        }

        private static void AddPorts(ICollection<ExposedPortRange> collection, params ExposedPortRange[] ports)
        {
            foreach (var port in ports)
                collection.Add(port);
        }

        /// <summary>
        /// If we don't include the SSH port, we can't control the instance.
        /// </summary>
        private static ExposedPort GetSshPort()
        {
            return new ExposedPort()
            {
                Port = 22,
                Protocol = SocketProtocol.Tcp
            };
        }

        /// <summary>
        /// Found here: https://kubernetes.io/docs/setup/production-environment/tools/kubeadm/install-kubeadm/
        /// </summary>
        private static ExposedPortRange[] GetKubernetesControlPlanePorts()
        {
            return new[]
            {
                new ExposedPort()
                {
                    Port = 6443,
                    Protocol = SocketProtocol.Tcp
                },
                new ExposedPortRange()
                {
                    FromPort = 2379,
                    ToPort = 2380,
                    Protocol = SocketProtocol.Tcp
                },
                new ExposedPortRange()
                {
                    FromPort = 10250,
                    ToPort = 10252,
                    Protocol = SocketProtocol.Tcp
                }
            };
        }

        /// <summary>
        /// Found here: https://kubernetes.io/docs/setup/production-environment/tools/kubeadm/install-kubeadm/
        /// </summary>
        private static ExposedPortRange[] GetKubernetesWorkerNodePorts()
        {
            return new[]
            {
                new ExposedPort()
                {
                    Port = 10250,
                    Protocol = SocketProtocol.Tcp
                },
                new ExposedPortRange()
                {
                    FromPort = 30000,
                    ToPort = 32767,
                    Protocol = SocketProtocol.Tcp
                }
            };
        }
    }
}

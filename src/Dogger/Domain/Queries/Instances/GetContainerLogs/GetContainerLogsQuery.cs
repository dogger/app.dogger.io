using System.Collections.Generic;
using MediatR;

namespace Dogger.Domain.Queries.Instances.GetContainerLogs
{
    public class GetContainerLogsQuery : IRequest<ICollection<ContainerLogsResponse>>
    {
        public string InstanceName { get; }

        public int? LinesToReturnPerContainer { get; set; }

        public GetContainerLogsQuery(
            string instanceName)
        {
            InstanceName = instanceName;
        }
    }
}

using MediatR;

namespace Dogger.Domain.Queries.Clusters.GetConnectionDetails
{

    public class GetConnectionDetailsQuery : IRequest<ConnectionDetailsResponse?>
    {
        public string ClusterId { get; }

        public GetConnectionDetailsQuery(
            string clusterId)
        {
            this.ClusterId = clusterId;
        }
    }
}

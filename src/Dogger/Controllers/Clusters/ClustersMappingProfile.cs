using System.Linq;
using AutoMapper;
using Dogger.Domain.Queries.Instances.GetContainerLogs;
using Dogger.Domain.Queries.Instances.GetProvisionedClustersWithInstancesForUser;
using Dogger.Domain.Services.Provisioning;

namespace Dogger.Controllers.Clusters
{
    public class ClustersMappingProfile : Profile
    {
        public ClustersMappingProfile()
        {
            CreateMap<UserClusterResponse, ClusterResponse>()
                .ForMember(x => x.Id, x => x.MapFrom(y => y.Id))
                .ForMember(x => x.Instances, x => x.MapFrom(y => y.Instances));

            CreateMap<ProvisioningJob, DeployToClusterResponse>()
                .ForMember(x => x.JobId, x => x.MapFrom(y => y.Id));

            CreateMap<Domain.Queries.Clusters.GetConnectionDetails.ConnectionDetailsResponse, ConnectionDetailsResponse>();

            CreateMap<UserClusterInstanceResponse, InstanceResponse>()
                .ForMember(x => x.CpuCount, x => x.MapFrom(y => y.AmazonModel.Hardware.CpuCount))
                .ForMember(x => x.DiskSizeInGigabytes, x => x.MapFrom(y => y.AmazonModel.Hardware.Disks.Single().SizeInGb))
                .ForMember(x => x.PrivateIpAddress, x => x.MapFrom(y => y.AmazonModel.PrivateIpAddress))
                .ForMember(x => x.PublicIpAddressV4, x => x.MapFrom(y => y.AmazonModel.PublicIpAddress))
                .ForMember(x => x.PublicIpAddressV6, x => x.MapFrom(y => y.AmazonModel.Ipv6Address))
                .ForMember(x => x.RamSizeInMegabytes, x => x.MapFrom(y => (int)(y.AmazonModel.Hardware.RamSizeInGb * 1024)))
                .ForMember(x => x.TransferPerMonthInGigabytes, x => x.MapFrom(y => y.AmazonModel.Networking.MonthlyTransfer.GbPerMonthAllocated))
                .ForMember(x => x.Id, x => x.MapFrom(y => y.DatabaseModel.Id));

            CreateMap<ContainerLogsResponse, LogsResponse>()
                .ForMember(x => x.ContainerId, x => x.MapFrom(y => y.Container.Id))
                .ForMember(x => x.ContainerImage, x => x.MapFrom(y => y.Container.Image))
                .ForMember(x => x.Logs, x => x.MapFrom(y => y.Logs));
        }
    }
}

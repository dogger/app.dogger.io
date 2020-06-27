using System;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;
using Dogger.Domain.Services.Provisioning.Instructions.Models;

namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class Blueprint
    {
        public Version Version { get; set; } = null!;

        [NotLogged]
        public AmazonUser AmazonUser { get; set; } = null!;
        public Guid AmazonUserId { get; set; }

        [NotLogged] 
        public Instance? Instance { get; set; }
        public Guid? InstanceId { get; set; }

        [NotLogged]
        public PullDogSettings? PullDogSettings { get; set; }
        public Guid? PullDogSettingsId { get; set; }

        [NotLogged] 
        public InstructionGroup[] InstructionGroups { get; set; } = null!;
    }
}

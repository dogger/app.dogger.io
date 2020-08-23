using System;
using System.Collections.Generic;

namespace Dogger.Domain.Models.Builders
{
    public class ClusterBuilder : IModelBuilder<Cluster>
    {
        private Guid id;

        private string? name;

        private EntityReference<User>? user;

        private Instance[] instances;

        public ClusterBuilder()
        {
            this.instances = Array.Empty<Instance>();
        }

        public ClusterBuilder WithId(Guid id)
        {
            this.id = id;
            return this;
        }

        public ClusterBuilder WithName(string? name)
        {
            this.name = name;
            return this;
        }

        public ClusterBuilder WithUser(User user)
        {
            this.user = user;
            return this;
        }

        public ClusterBuilder WithUser(Guid? user)
        {
            this.user = new EntityReference<User>(user);
            return this;
        }

        public ClusterBuilder WithInstances(params Instance[] instances)
        {
            this.instances = instances;
            return this;
        }

        public Cluster Build()
        {
            var cluster = new Cluster()
            {
                Id = this.id,
                Name = this.name,
                User = this.user?.Reference,
                UserId = this.user?.Id
            };

            cluster.Instances.AddRange(this.instances);

            return cluster;
        }
    }
}

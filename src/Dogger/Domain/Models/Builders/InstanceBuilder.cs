using System;

namespace Dogger.Domain.Models.Builders
{
    public class InstanceBuilder : IModelBuilder<Instance>
    {
        private Guid id;

        private DateTime createdAtUtc;
        private DateTime? expiresAtUtc;

        private bool isProvisioned;

        private string? name;
        private string? planId;

        private EntityReference<Cluster>? cluster;
        private EntityReference<PullDogPullRequest>? pullDogPullRequest;

        public InstanceBuilder WithId(Guid value)
        {
            this.id = value;
            return this;
        }

        public InstanceBuilder WithCreatedDate(DateTime value)
        {
            this.createdAtUtc = value;
            return this;
        }

        public InstanceBuilder WithExpiredDate(DateTime? value)
        {
            this.expiresAtUtc = value;
            return this;
        }

        public InstanceBuilder WithProvisionedStatus(bool value)
        {
            this.isProvisioned = value;
            return this;
        }

        public InstanceBuilder WithName(string value)
        {
            this.name = value;
            return this;
        }

        public InstanceBuilder WithPlanId(string value)
        {
            this.planId = value;
            return this;
        }

        public InstanceBuilder WithCluster(Cluster value)
        {
            this.cluster = value;
            return this;
        }

        public InstanceBuilder WithPullDogPullRequest(PullDogPullRequest value)
        {
            this.pullDogPullRequest = value;
            return this;
        }

        public Instance Build()
        {
            if (cluster == null)
                throw new InvalidOperationException("Cluster not specified.");

            return new Instance()
            {
                Cluster = this.cluster.Reference!,
                ClusterId = this.cluster.Id!.Value,
                CreatedAtUtc = this.createdAtUtc,
                ExpiresAtUtc = this.expiresAtUtc,
                Id = this.id,
                IsProvisioned = this.isProvisioned,
                Name = this.name ?? throw new InvalidOperationException("Name not specified."),
                PlanId = this.planId ?? throw new InvalidOperationException("No plan ID specified."),
                PullDogPullRequest = this.pullDogPullRequest?.Reference
            };
        }
    }
}

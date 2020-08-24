using System;
using System.Collections.Generic;
using System.Linq;

namespace Dogger.Domain.Models.Builders
{
    public class UserBuilder : ModelBuilder<User>
    {
        private Guid id;

        private string? stripeCustomerId;
        private string? stripeSubscriptionId;

        private PullDogSettings? pullDogSettings;

        private Identity[] identities;
        private Cluster[] clusters;
        private AmazonUser[] amazonUsers;

        public UserBuilder()
        {
            this.identities = Array.Empty<Identity>();
            this.clusters = Array.Empty<Cluster>();
            this.amazonUsers = Array.Empty<AmazonUser>();
        }

        public UserBuilder WithIdentities(params Identity[] identities)
        {
            this.identities = identities;
            return this;
        }

        public UserBuilder WithClusters(params Cluster[] clusters)
        {
            this.clusters = clusters;
            return this;
        }

        public UserBuilder WithAmazonUsers(params AmazonUser[] amazonUsers)
        {
            this.amazonUsers = amazonUsers;
            return this;
        }

        public UserBuilder WithPullDogSettings(PullDogSettings? pullDogSettings)
        {
            this.pullDogSettings = pullDogSettings;
            return this;
        }

        public UserBuilder WithStripeCustomerId(
            string? stripeCustomerId)
        {
            this.stripeCustomerId = stripeCustomerId;
            return this;
        }

        public UserBuilder WithStripeSubscriptionId(
            string? stripeSubscriptionId)
        {
            this.stripeSubscriptionId = stripeSubscriptionId;
            return this;
        }

        public UserBuilder WithId(Guid id)
        {
            this.id = id;
            return this;
        }

        public override User Build()
        {
            var user = new User()
            {
                Id = this.id,
                PullDogSettings = this.pullDogSettings,
                StripeSubscriptionId = this.stripeSubscriptionId,
                StripeCustomerId = 
                    this.stripeCustomerId ?? 
                    throw new InvalidOperationException("Stripe customer ID not specified.")
            };

            user.Identities.AddRange(this.identities);
            user.Clusters.AddRange(this.clusters);
            user.AmazonUsers.AddRange(this.amazonUsers);

            return user;
        }
    }
}

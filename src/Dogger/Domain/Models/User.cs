﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

#pragma warning disable 8618

namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class User
    {
        public Guid Id { get; set; }

        [NotLogged]
        public List<Identity> Identities { get; set; }

        [NotLogged]
        public List<Cluster> Clusters { get; set; }

        [NotLogged]
        public List<License> Licenses { get; set; }

        [NotLogged]
        public List<AmazonUser> AmazonUsers { get; set; }

        [NotLogged]
        public PullDogSettings? PullDogSettings { get; set; }

        public string StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }

        public User()
        {
            Identities = new List<Identity>();
            Clusters = new List<Cluster>();
            Licenses = new List<License>();
            AmazonUsers = new List<AmazonUser>();
        }
    }

}

using System;
using Dogger.Domain.Models.Builders;

namespace Dogger.Tests.Domain.Models
{
    public class TestClusterBuilder : ClusterBuilder
    {
        public TestClusterBuilder()
        {
            WithId(Guid.NewGuid());
            WithName(Guid.NewGuid().ToString());
        }

        public TestClusterBuilder WithUser()
        {
            WithUser(new TestUserBuilder());
            return this;
        }
    }
}
using System;
using Dogger.Domain.Models.Builders;

public class TestClusterBuilder : ClusterBuilder
{
    public TestClusterBuilder()
    {
        WithId(Guid.NewGuid());
        WithName(Guid.NewGuid().ToString());
    }
}
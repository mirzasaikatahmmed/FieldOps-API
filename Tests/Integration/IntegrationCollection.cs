using Xunit;

namespace FieldOps.Tests.Integration;

[CollectionDefinition("Integration", DisableParallelization = true)]
public class IntegrationCollection : ICollectionFixture<FieldOpsApiFactory>
{
}

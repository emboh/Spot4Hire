using Xunit;

namespace Spot4Hire.Backend.Tests.Infrastructure;

// One shared container across all tests in this collection, run sequentially.
[CollectionDefinition(nameof(DatabaseCollection))]
public sealed class DatabaseCollection : ICollectionFixture<SqlServerFixture>
{
}
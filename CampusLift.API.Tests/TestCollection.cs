using Xunit;

namespace CampusLift.API.Tests;

[CollectionDefinition("Sequential")]
public class SequentialCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}
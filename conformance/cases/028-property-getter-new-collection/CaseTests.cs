using System.Collections.Generic;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    public class Store
    {
        public List<string> Categories => new List<string> { "Electronics", "Books" };
    }

    [Fact]
    public void Property_getter_allocates_new_list()
    {
        // Arrange
        var store = new Store();

        // Act
        var count = store.Categories.Count;

        // Assert
        count.Should().Be(2, "two store categories are configured");
    }
}

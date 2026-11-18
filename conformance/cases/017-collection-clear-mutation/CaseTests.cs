using System.Collections.Generic;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Clear_mutation_empties_collection_in_place()
    {
        // Arrange
        var items = new List<string> { "item1", "item2" };

        // Act
        items.Clear();

        // Assert
        items.Count.Should().Be(0, "collection is empty after clear");
    }
}

using System.Collections.Generic;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Remove_mutation_deletes_item_in_place()
    {
        // Arrange
        var items = new List<string> { "item1", "item2" };

        // Act
        items.Remove("item1");

        // Assert
        items.Count.Should().Be(1, "one item remains after removal");
    }
}

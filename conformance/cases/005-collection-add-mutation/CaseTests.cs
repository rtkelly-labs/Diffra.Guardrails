using System.Collections.Generic;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Add_mutation_appends_item_in_place()
    {
        // Arrange
        var items = new List<string> { "item1" };

        // Act
        items.Add("item2");

        // Assert
        items.Count.Should().Be(2, "two items should be present after addition");
    }
}

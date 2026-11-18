using System.Collections.Generic;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void In_place_sort_mutates_collection()
    {
        // Arrange
        var scores = new List<int> { 30, 10, 20 };

        // Act
        scores.Sort();

        // Assert
        scores[0].Should().Be(10, "lowest score is first after in-place sort");
    }
}

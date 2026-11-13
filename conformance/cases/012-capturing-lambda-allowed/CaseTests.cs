using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Lambda_capturing_outer_threshold_is_allowed()
    {
        // Arrange
        var threshold = 15;
        var numbers = new List<int> { 10, 20, 30 };

        // Act
        var filtered = numbers.Where(x => x > threshold).ToList();

        // Assert
        filtered.Count.Should().Be(2, "two items exceed the dynamic threshold");
    }
}

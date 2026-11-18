using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Filter_uses_non_static_pure_lambda()
    {
        // Arrange
        var numbers = new List<int> { 1, 2, 3, 4 };

        // Act
        var evens = numbers.Where(x => x % 2 == 0).ToList();

        // Assert
        evens.Count.Should().Be(2, "two even numbers should be matched");
    }
}

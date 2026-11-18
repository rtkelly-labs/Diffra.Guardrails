using System.Linq;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Local_function_without_static_captures_nothing()
    {
        // Arrange
        var numbers = new[] { 2, 4, 6 };
        int Halve(int x) => x / 2;

        // Act
        var results = numbers.Select(Halve).ToArray();

        // Assert
        results.Should().Equal([1, 2, 3], "halving even numbers produces one, two, three");
    }
}

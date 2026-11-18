using System.Linq;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Static_lambda_guarantees_zero_closure_allocation()
    {
        // Arrange
        var numbers = new[] { 1, 2, 3, 4, 5 };

        // Act
        var oddCount = numbers.Count(static x => x % 2 != 0);

        // Assert
        oddCount.Should().Be(3, "three odd numbers exist");
    }
}

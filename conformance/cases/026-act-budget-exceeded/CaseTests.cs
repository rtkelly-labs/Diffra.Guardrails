using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Act_block_has_two_statements()
    {
        // Arrange
        var basket = new Basket();

        // Act
        basket.ApplyDiscount(10);
        var total = basket.Total();

        // Assert
        total.Should().Be(90m, "ten percent discount is deducted");
    }
}

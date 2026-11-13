using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Assertion_placed_inside_act_block()
    {
        // Arrange
        var basket = new Basket(prices: [20m]);

        // Act
        var total = basket.Total();
        total.Should().Be(20m, "basket total equals item price");

        // Assert
    }
}

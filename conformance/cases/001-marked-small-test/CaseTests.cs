using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void A_percentage_discount_is_taken_off_the_total()
    {
        // Arrange
        var basket = new Basket(prices: [40m, 60m]);

        // Act
        var total = basket.Total(discountPercent: 15);

        // Assert
        total.Should().Be(85m, "the discount is taken off the price, not added to it");
    }
}

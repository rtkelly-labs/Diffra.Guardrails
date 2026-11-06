using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void A_discount_code_changes_the_total_and_the_label()
    {
        // Arrange
        var basket = new Basket(prices: [40m, 60m]);

        // Act
        var receipt = basket.Checkout(code: "SPRING15");

        // Assert
        receipt.Total.Should().Be(85m);
        receipt.Label.Should().Be("SPRING15 applied");
    }
}

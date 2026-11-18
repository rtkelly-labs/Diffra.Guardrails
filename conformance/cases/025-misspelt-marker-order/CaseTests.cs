using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Marker_misspelt_triggers_diagnostic()
    {
        // Arange
        var basket = new Basket(prices: [10m, 20m]);

        // Act
        var total = basket.Total();

        // Assert
        total.Should().Be(30m, "sum of prices is thirty");
    }
}

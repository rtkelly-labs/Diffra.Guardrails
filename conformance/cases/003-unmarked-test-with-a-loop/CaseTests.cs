using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void No_discount_makes_the_total_negative()
    {
        var basket = new Basket(prices: [40m, 60m]);
        foreach (var percent in new[] { 0, 50, 100, 150 })
        {
            var total = basket.Total(discountPercent: percent);
            total.Should().BeGreaterThanOrEqualTo(0m, "a discount can bring the total to zero but never below it");
        }
    }
}

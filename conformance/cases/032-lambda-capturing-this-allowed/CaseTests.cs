using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    public class PriceCalculator
    {
        private readonly decimal _discountRate = 0.1m;

        public IEnumerable<decimal> ApplyDiscount(IEnumerable<decimal> prices)
        {
            return prices.Select(p => p - (p * _discountRate));
        }
    }

    [Fact]
    public void Lambda_capturing_instance_state_is_allowed()
    {
        // Arrange
        var calculator = new PriceCalculator();
        var prices = new[] { 100m, 200m };

        // Act
        var discounted = calculator.ApplyDiscount(prices).ToList();

        // Assert
        discounted[0].Should().Be(90m, "first price has ten percent discount deducted");
    }
}

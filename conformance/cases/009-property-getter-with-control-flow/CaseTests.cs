using System.Collections.Generic;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    public class Basket
    {
        public List<decimal> Items { get; } = [10m, 25m];

        public decimal ComputedTotal
        {
            get
            {
                decimal sum = 0m;
                foreach (var price in Items)
                {
                    sum += price;
                }
                return sum;
            }
        }
    }

    [Fact]
    public void Property_getter_calculates_sum_via_loop()
    {
        // Arrange
        var basket = new Basket();

        // Act
        var total = basket.ComputedTotal;

        // Assert
        total.Should().Be(35m, "sum of items should equal thirty five");
    }
}

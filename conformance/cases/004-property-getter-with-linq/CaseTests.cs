using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    public class Order
    {
        public List<int> Items { get; } = [10, 20, 30];
        public List<int> DiscountedItems => Items.Where(x => x > 15).ToList();
    }

    [Fact]
    public void Property_getter_evaluates_active_items()
    {
        // Arrange
        var order = new Order();

        // Act
        var count = order.DiscountedItems.Count;

        // Assert
        count.Should().Be(2, "only items over fifteen qualify for discount");
    }
}

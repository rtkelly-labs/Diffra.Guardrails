using FluentAssertions;
using Xunit;

public enum OrderStatus
{
    Pending,
    Approved,
    Cancelled
}

public class OrderStatusTests
{
    [Fact]
    public void Switch_evaluates_discount_with_discard()
    {
        // Arrange
        var status = OrderStatus.Pending;

        // Act
        var discount = status switch
        {
            OrderStatus.Pending => 0.0m,
            OrderStatus.Approved => 0.05m,
            _ => 0.0m
        };

        // Assert
        discount.Should().Be(0.0m, "pending order receives zero discount");
    }
}

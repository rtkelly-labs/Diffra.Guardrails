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
    public void Switch_evaluates_discount_exhaustively()
    {
        // Arrange
        var status = OrderStatus.Approved;

        // Act
        var discount = status switch
        {
            OrderStatus.Pending => 0.0m,
            OrderStatus.Approved => 0.05m,
            OrderStatus.Cancelled => 0.0m
        };

        // Assert
        discount.Should().Be(0.05m, "approved order receives five percent discount");
    }
}

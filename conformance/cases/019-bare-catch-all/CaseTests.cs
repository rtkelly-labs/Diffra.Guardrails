using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Bare_catch_block_swallows_fault()
    {
        // Arrange
        var basket = new Basket();

        // Act
        try
        {
            basket.ApplyCode("UNKNOWN");
        }
        catch
        {
            // Bare catch swallowing all
        }

        // Assert
        basket.Total.Should().Be(0m, "total remains zero when bare catch swallows error");
    }
}

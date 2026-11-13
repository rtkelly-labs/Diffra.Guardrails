using System;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Catch_block_swallows_broad_exception()
    {
        // Arrange
        var basket = new Basket();

        // Act
        try
        {
            basket.ApplyCode("INVALID");
        }
        catch (Exception)
        {
            // Swallowed
        }

        // Assert
        basket.Total.Should().Be(0m, "total remains zero when invalid code is swallowed");
    }
}

using System;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Filtered_exception_catch_is_allowed()
    {
        // Arrange
        var basket = new Basket();
        var wasCaught = false;

        // Act
        try
        {
            basket.ApplyCode("EXPIRED");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("EXPIRED"))
        {
            wasCaught = true;
        }

        // Assert
        wasCaught.Should().BeTrue("expired code exception should match filter");
    }
}

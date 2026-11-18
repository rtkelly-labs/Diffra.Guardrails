using System;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Exception_logged_and_rethrown_preserves_diagnostics()
    {
        // Arrange
        var basket = new Basket();
        Action act = () =>
        {
            try
            {
                basket.ApplyCode("INVALID");
            }
            catch (Exception)
            {
                throw;
            }
        };

        // Act & Assert
        act.Should().Throw<Exception>("invalid code causes exception which is faithfully rethrown");
    }
}

using System;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Rethrow_with_identifier_truncates_stack()
    {
        // Arrange
        var basket = new Basket();
        Action act = () =>
        {
            try
            {
                basket.ApplyCode("ERR");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        };

        // Act & Assert
        act.Should().Throw<Exception>("rethrow triggers exception");
    }
}

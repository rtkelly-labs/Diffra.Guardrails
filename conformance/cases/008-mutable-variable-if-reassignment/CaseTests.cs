using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Status_assigned_across_conditional_branches()
    {
        // Arrange
        var discount = 20;

        // Act
        string tier;
        if (discount > 10)
        {
            tier = "Gold";
        }
        else
        {
            tier = "Standard";
        }

        // Assert
        tier.Should().Be("Gold", "twenty percent discount qualifies for gold tier");
    }
}

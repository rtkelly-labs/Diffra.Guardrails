using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Switch_expression_binds_single_assignment()
    {
        // Arrange
        var status = 2;

        // Act
        var label = status switch
        {
            1 => "Active",
            2 => "Paused",
            _ => "Closed"
        };

        // Assert
        label.Should().Be("Paused", "status two corresponds to paused");
    }
}

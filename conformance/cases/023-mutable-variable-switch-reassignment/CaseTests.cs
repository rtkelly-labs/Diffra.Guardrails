using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Switch_statement_mutates_procedural_variable()
    {
        // Arrange
        var status = 2;

        // Act
        string label;
        switch (status)
        {
            case 1:
                label = "Active";
                break;
            case 2:
                label = "Paused";
                break;
            default:
                label = "Closed";
                break;
        }

        // Assert
        label.Should().Be("Paused", "status two corresponds to paused");
    }
}

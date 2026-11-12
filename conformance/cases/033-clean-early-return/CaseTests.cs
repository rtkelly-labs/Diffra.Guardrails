using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    private static string ClassifyStatus(int code)
    {
        if (code < 0)
        {
            return "Invalid";
        }

        if (code == 0)
        {
            return "Pending";
        }

        return "Active";
    }

    [Fact]
    public void Early_return_flow_evaluates_purely()
    {
        // Arrange
        var code = 5;

        // Act
        var result = ClassifyStatus(code);

        // Assert
        result.Should().Be("Active", "positive code classifies as active");
    }
}

using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    public class Order
    {
        public int TierCode { get; init; } = 1;

        public string TierDescription
        {
            get
            {
                switch (TierCode)
                {
                    case 1:
                        return "Standard";
                    case 2:
                        return "Premium";
                    default:
                        return "Unknown";
                }
            }
        }
    }

    [Fact]
    public void Property_getter_executes_switch_statement()
    {
        // Arrange
        var order = new Order();

        // Act
        var desc = order.TierDescription;

        // Assert
        desc.Should().Be("Standard", "first tier is standard");
    }
}

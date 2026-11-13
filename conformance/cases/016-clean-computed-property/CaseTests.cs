using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    public class Rectangle
    {
        public int Width { get; init; } = 10;
        public int Height { get; init; } = 5;

        public int Area => Width * Height;
    }

    [Fact]
    public void Clean_computed_property_evaluates_simple_arithmetic()
    {
        // Arrange
        var rect = new Rectangle();

        // Act
        var area = rect.Area;

        // Assert
        area.Should().Be(50, "area of ten by five rectangle is fifty");
    }
}

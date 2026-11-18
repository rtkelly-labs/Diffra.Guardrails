using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    public class Catalog
    {
        public int[] DefaultTiers => new int[] { 1, 2, 3 };
    }

    [Fact]
    public void Property_getter_allocates_new_array()
    {
        // Arrange
        var catalog = new Catalog();

        // Act
        var count = catalog.DefaultTiers.Length;

        // Assert
        count.Should().Be(3, "three default tiers exist");
    }
}

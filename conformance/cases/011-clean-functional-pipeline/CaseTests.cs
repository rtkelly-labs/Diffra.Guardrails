using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    public record Order(ImmutableArray<int> Items)
    {
        public int ItemCount => Items.Length;
    }

    [Fact]
    public void Clean_functional_pipeline_executes_purely()
    {
        // Arrange
        var initial = new Order(Items: [10, 20]);
        var nextItem = 30;

        // Act
        var updated = initial with { Items = [.. initial.Items, nextItem] };

        // Assert
        updated.Items.Where(static x => x > 15).Count().Should().Be(2, "two items exceed fifteen in the pure pipeline");
    }
}

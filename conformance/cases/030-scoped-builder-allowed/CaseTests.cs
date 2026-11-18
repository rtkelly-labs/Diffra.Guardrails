using System.Collections.Immutable;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    private static ImmutableArray<int> BuildRange(int count)
    {
        var builder = ImmutableArray.CreateBuilder<int>(count);
        for (var i = 0; i < count; i++)
        {
            builder.Add(i);
        }
        return builder.MoveToImmutable();
    }

    [Fact]
    public void Scoped_builder_produces_immutable_array()
    {
        // Arrange
        var targetCount = 3;

        // Act
        var result = BuildRange(targetCount);

        // Assert
        result.Length.Should().Be(3, "three elements produced by scoped builder");
    }
}

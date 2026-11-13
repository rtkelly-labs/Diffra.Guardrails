using System.Collections.Generic;
using FluentAssertions;
using Pricing;
using Xunit;

public class BasketTests
{
    [Fact]
    public void Collection_expression_combines_purely()
    {
        // Arrange
        int[] first = [1, 2];
        int[] second = [3, 4];

        // Act
        int[] combined = [.. first, .. second];

        // Assert
        combined.Length.Should().Be(4, "combining two length two arrays produces length four array");
    }
}

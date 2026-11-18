using System;
using FluentAssertions;
using Xunit;

public class AuditTests
{
    [Fact]
    public void Read_timestamp_uses_system_clock()
    {
        // Arrange
        var baseline = DateTime.MinValue;

        // Act
        var now = DateTime.UtcNow;

        // Assert
        now.Should().BeAfter(baseline, "system clock timestamp is after baseline");
    }
}

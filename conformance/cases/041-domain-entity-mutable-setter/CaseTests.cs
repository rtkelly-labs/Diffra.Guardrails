using FluentAssertions;
using Xunit;

namespace Domain.Entities;

public class OrderEntity
{
    public string Id { get; init; } = "";
    public decimal Total { get; set; }
}

public class OrderEntityTests
{
    [Fact]
    public void Entity_mutates_total()
    {
        // Arrange
        var order = new OrderEntity { Id = "ORD-1", Total = 100m };

        // Act
        order.Total = 120m;

        // Assert
        order.Total.Should().Be(120m, "order total was mutated via setter");
    }
}

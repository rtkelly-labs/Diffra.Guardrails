using FluentAssertions;
using Xunit;

namespace Domain.Contracts;

public record OrderDto(int Id, decimal Price)
{
    public decimal CalculateTotal() => Price * 1.2m;
}

public class OrderContractTests
{
    [Fact]
    public void Contract_calculates_total()
    {
        // Arrange
        var dto = new OrderDto(1, 100m);

        // Act
        var total = dto.CalculateTotal();

        // Assert
        total.Should().Be(120m, "total includes standard tax");
    }
}

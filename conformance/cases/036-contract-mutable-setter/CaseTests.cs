using FluentAssertions;
using Xunit;

namespace Domain.Contracts;

public record InvoiceDto
{
    public int Id { get; init; }
    public decimal Amount { get; set; }
}

public class InvoiceContractTests
{
    [Fact]
    public void Contract_sets_amount()
    {
        // Arrange
        var invoice = new InvoiceDto { Id = 1, Amount = 50m };

        // Act
        invoice.Amount = 75m;

        // Assert
        invoice.Amount.Should().Be(75m, "amount was mutated via set");
    }
}

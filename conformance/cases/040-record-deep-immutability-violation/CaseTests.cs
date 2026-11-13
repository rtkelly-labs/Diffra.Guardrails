using FluentAssertions;
using Xunit;

namespace Domain.Aggregates;

public class MutableAddress
{
    public string Street { get; init; } = "";
}

public record Customer(string Id, MutableAddress Address);

public class CustomerTests
{
    [Fact]
    public void Record_reads_address_street()
    {
        // Arrange
        var customer = new Customer("C-1", new MutableAddress { Street = "Main St" });

        // Act
        var street = customer.Address.Street;

        // Assert
        street.Should().Be("Main St", "street matches customer address");
    }
}

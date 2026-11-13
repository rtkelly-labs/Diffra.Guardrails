using FluentAssertions;
using Xunit;

namespace Domain.Contracts;

public record CustomerDto(string FirstName, string LastName)
{
    public string FullName => $"{FirstName} {LastName}";
}

public class CustomerContractTests
{
    [Fact]
    public void Contract_formats_full_name()
    {
        // Arrange
        var customer = new CustomerDto("Ada", "Lovelace");

        // Act
        var name = customer.FullName;

        // Assert
        name.Should().Be("Ada Lovelace", "full name joins first and last");
    }
}

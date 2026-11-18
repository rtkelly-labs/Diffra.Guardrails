using FluentAssertions;
using Xunit;

namespace Domain.Contracts;

public class UserContract
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

public class UserContractTests
{
    [Fact]
    public void Contract_reads_name()
    {
        // Arrange
        var user = new UserContract { Id = 1, Name = "Alice" };

        // Act
        var name = user.Name;

        // Assert
        name.Should().Be("Alice", "user name matches initialized value");
    }
}

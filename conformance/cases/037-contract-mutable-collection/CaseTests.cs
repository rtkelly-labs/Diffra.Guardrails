using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Domain.Contracts;

public record TeamDto(int Id, List<string> Members);

public class TeamContractTests
{
    [Fact]
    public void Contract_reads_members()
    {
        // Arrange
        var team = new TeamDto(1, ["Alice", "Bob"]);

        // Act
        var count = team.Members.Count;

        // Assert
        count.Should().Be(2, "two members present in team");
    }
}

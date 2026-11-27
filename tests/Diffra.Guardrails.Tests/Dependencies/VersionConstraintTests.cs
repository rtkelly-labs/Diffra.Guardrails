using Diffra.Guardrails.Core.Dependencies;

namespace Diffra.Guardrails.Tests.Dependencies;

public class VersionConstraintTests
{
    [Theory]
    [InlineData("", "0.0.1", true)]
    [InlineData("1.2.3", "1.2.3", true)]
    [InlineData("= 1.2.3", "1.2.3.0", true)]
    [InlineData("= 1.2.3", "1.2.4", false)]
    [InlineData(">= 8.0", "8.0.0", true)]
    [InlineData(">= 8.0", "7.99.99", false)]
    [InlineData(">= 8.0", "8.0.0-rc.1", true)]
    [InlineData(">= 8.0.0-rc.2", "8.0.0-rc.1", false)]
    [InlineData("> 8.0", "8.0.0", false)]
    [InlineData("< 4.20.2", "4.20.2-beta", true)]
    [InlineData(">= 4.20.0 < 4.20.2", "4.20.1", true)]
    [InlineData(">= 4.20.0 < 4.20.2", "4.20.2", false)]
    [InlineData("<= 2.0", "2.0.0.1", false)]
    [InlineData("~> 1.2", "1.9.9", true)]
    [InlineData("~> 1.2", "2.0.0", false)]
    [InlineData("~> 1.2.3", "1.2.9", true)]
    [InlineData("~> 1.2.3", "1.3.0", false)]
    [InlineData("~> 1.2.3", "1.2.2", false)]
    [InlineData("= 1.0.0-beta.2", "1.0.0-BETA.2", true)]
    [InlineData("< 1.0.0-beta.10", "1.0.0-beta.9", true)]
    public void A_constraint_covers_the_versions_Paket_would_give_it(string constraint, string version, bool expected)
    {
        // Arrange
        var parsed = VersionConstraint.Parse(constraint.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        // Act
        var matches = parsed.Matches(PackageVersion.Parse(version));

        // Assert
        matches.Should().Be(expected, because: $"'{constraint}' decides whether {version} is banned");
    }

    [Theory]
    [InlineData("== 1.0", "conflict override")]
    [InlineData(">= 1.0 prerelease", "prerelease flags")]
    [InlineData("!= 1.0", "not a version operator")]
    [InlineData(">= 1.0 <", "needs a version")]
    [InlineData("= 1.0 < 2.0", "cannot be combined")]
    [InlineData(">= one", "not a NuGet version")]
    public void An_unsupported_constraint_is_rejected_with_the_reason(string constraint, string reason)
    {
        // Arrange
        var tokens = constraint.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Act
        var parse = () => VersionConstraint.Parse(tokens);

        // Assert
        parse.Should().Throw<FormatException>(because: "a ban that cannot be read must stop the run, never be skipped")
            .WithMessage($"*{reason}*");
    }
}

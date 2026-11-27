using Diffra.Guardrails.Core.Dependencies;
using Diffra.Guardrails.Tool;

namespace Diffra.Guardrails.Tests.Dependencies;

public class BanListTests
{
    [Fact]
    public void A_ban_line_keeps_its_package_reason_exemption_and_line()
    {
        // Arrange
        var text = "// licence bans\n\nnuget Verify >= 30.0 exempt-until: 2027-11-30 // small-revenue licence\n";

        // Act
        var ban = BanList.Parse(text, "banned.dependencies").Single();

        // Assert
        new { ban.PackageId, ban.Constraint.Text, ban.Reason, ban.ExemptUntil, ban.Line }.Should().BeEquivalentTo(
            new { PackageId = "Verify", Text = ">= 30.0", Reason = "small-revenue licence", ExemptUntil = (DateOnly?)new DateOnly(2027, 11, 30), Line = 3 },
            because: "findings quote the reason and point at the ban's line");
    }

    [Theory]
    [InlineData("nuget Moq >= 4.20.0", "needs a reason")]
    [InlineData("nuget Moq //   ", "needs a reason")]
    [InlineData("package Moq // reason", "expected 'nuget")]
    [InlineData("nuget // reason", "expected 'nuget")]
    [InlineData("nuget Moq exempt-until: 2027-13-01 // reason", "not a date")]
    [InlineData("nuget Moq exempt-until: 2027-01-01 >= 4.0 // reason", "must come last")]
    public void A_bad_ban_line_stops_the_run_and_names_the_line(string line, string reason)
    {
        // Arrange
        var text = "nuget Fine // ok\n" + line;

        // Act
        var parse = () => BanList.Parse(text, "banned.dependencies");

        // Assert
        parse.Should().Throw<BanFileException>(because: "a skipped ban line would let the package through silently")
            .WithMessage($"banned.dependencies:2: *{reason}*");
    }

    [Fact]
    public void A_root_with_no_dependency_graph_is_a_usage_error_not_a_pass()
    {
        // Arrange
        var root = Directory.CreateTempSubdirectory("diffra-deps-").FullName;
        File.WriteAllText(Path.Combine(root, "banned.dependencies"), "nuget Moq // reason\n");

        // Act
        var exit = DepsCommand.Run(["check", "--root", root], new StringWriter(), new StringWriter());

        // Assert
        exit.Should().Be(2, because: "an unrestored repository is not evidence of a clean one");
    }

    [Fact]
    public void An_excluded_directory_is_not_read()
    {
        // Arrange
        var root = Directory.CreateTempSubdirectory("diffra-deps-").FullName;
        File.WriteAllText(Path.Combine(root, "banned.dependencies"), "nuget Pricing.Licensing // proprietary licence\n");
        foreach (var folder in new[] { "src", "fixtures/bad" })
        {
            Directory.CreateDirectory(Path.Combine(root, folder));
        }

        File.WriteAllText(Path.Combine(root, "src", "paket.lock"), "NUGET\n    Pricing.Helpers (1.0)\n");
        File.WriteAllText(Path.Combine(root, "fixtures", "bad", "paket.lock"), "NUGET\n    Pricing.Licensing (1.0)\n");

        // Act
        var exit = DepsCommand.Run(["check", "--root", root, "--exclude", "fixtures"], new StringWriter(), new StringWriter());

        // Assert
        exit.Should().Be(0, because: "the only banned package is under the excluded fixtures directory");
    }
}

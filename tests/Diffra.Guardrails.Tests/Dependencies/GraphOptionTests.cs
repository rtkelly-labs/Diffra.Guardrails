using System.Text.Json;
using Diffra.Guardrails.Tool;

namespace Diffra.Guardrails.Tests.Dependencies;

public class GraphOptionTests
{
    [Fact]
    public void A_named_graph_is_the_only_one_read_and_keeps_its_path_from_the_root()
    {
        // Arrange
        var root = Directory.CreateTempSubdirectory("diffra-deps-").FullName;
        File.WriteAllText(Path.Combine(root, "banned.dependencies"), "nuget Pricing.Licensing // proprietary licence\n");
        foreach (var project in new[] { "Pricing", "Basket" })
        {
            Directory.CreateDirectory(Path.Combine(root, project));
            File.WriteAllText(Path.Combine(root, project, "paket.lock"), "NUGET\n    Pricing.Licensing (1.0)\n");
        }

        var output = new StringWriter();

        // Act
        DepsCommand.Run(
            ["check", "--root", root, "--graph", Path.Combine(root, "Pricing", "paket.lock"), "--format", "json"],
            output,
            new StringWriter());

        // Assert
        JsonDocument.Parse(output.ToString()).RootElement.EnumerateArray()
            .Select(f => f.GetProperty("path").GetString())
            .Should().Equal(["Pricing/paket.lock"], because: "only the named graph is read, and it is named from the root");
    }

    [Fact]
    public void A_named_graph_that_does_not_exist_is_a_usage_error_not_a_pass()
    {
        // Arrange
        var root = Directory.CreateTempSubdirectory("diffra-deps-").FullName;
        File.WriteAllText(Path.Combine(root, "banned.dependencies"), "nuget Moq // reason\n");

        // Act
        var exit = DepsCommand.Run(
            ["check", "--root", root, "--graph", "obj/project.assets.json"], new StringWriter(), new StringWriter());

        // Assert
        exit.Should().Be(2, because: "a graph that was asked for and is missing has not been checked");
    }
}

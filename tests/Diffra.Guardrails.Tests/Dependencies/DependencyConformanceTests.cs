using System.Text.Json;
using Diffra.Guardrails.Tool;

namespace Diffra.Guardrails.Tests.Dependencies;

public class DependencyConformanceTests
{
    public static TheoryData<string> Cases()
    {
        var data = new TheoryData<string>();
        foreach (var directory in Directory.GetDirectories(CasesDirectory()).Order(StringComparer.Ordinal))
        {
            data.Add(Path.GetFileName(directory));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Deps_check_reports_exactly_the_expected_findings(string name)
    {
        // Arrange
        var root = Path.Combine(CasesDirectory(), name);
        using var expectedJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "expected.json")));
        var expected = expectedJson.RootElement;
        var output = new StringWriter();

        // Act
        var exit = DepsCommand.Run(
            ["check", "--root", root, "--today", expected.GetProperty("today").GetString()!, "--format", "json"],
            output,
            new StringWriter());

        // Assert
        new { Exit = exit, Findings = Keys(JsonDocument.Parse(output.ToString()).RootElement) }.Should().BeEquivalentTo(
            new { Exit = expected.GetProperty("exit").GetInt32(), Findings = Keys(expected.GetProperty("findings")) },
            because: $"case {name} pins the exit code and the findings by rule, file, package, version and path");
    }

    private static List<string> Keys(JsonElement findings) =>
        findings.EnumerateArray()
            .Select(f => string.Join(
                " | ",
                f.GetProperty("rule").GetString(),
                f.GetProperty("path").GetString(),
                f.GetProperty("package").GetString(),
                f.GetProperty("version").GetString(),
                string.Join(" -> ", f.GetProperty("via").EnumerateArray().Select(v => v.GetString()))))
            .ToList();

    internal static string CasesDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Diffra.Guardrails.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ?? throw new InvalidOperationException("repository root not found"),
            "conformance", "dependencies", "cases");
    }
}

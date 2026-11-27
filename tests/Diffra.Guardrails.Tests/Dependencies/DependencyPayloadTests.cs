using System.Text.Json;
using Diffra.Guardrails.Tool;

namespace Diffra.Guardrails.Tests.Dependencies;

public class DependencyPayloadTests
{
    public static TheoryData<string> Cases() => DependencyConformanceTests.Cases();

    [Theory]
    [MemberData(nameof(Cases))]
    public void The_payload_counts_each_case_findings_by_file_and_rule(string name)
    {
        // Arrange
        var root = Path.Combine(DependencyConformanceTests.CasesDirectory(), name);
        using var expectedJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "expected.json")));
        var expected = expectedJson.RootElement;
        var outDirectory = Directory.CreateTempSubdirectory("diffra-deps-payload-").FullName;

        // Act
        DepsCommand.Run(
            ["payload", "--root", root, "--today", expected.GetProperty("today").GetString()!, "--out", outDirectory, "--scope", "example/pricing"],
            new StringWriter(),
            new StringWriter());

        // Assert
        new { Shape = ShapeErrors(Payload(outDirectory)), Counts = Counts(Payload(outDirectory)) }.Should().BeEquivalentTo(
            new { Shape = new List<string>(), Counts = ExpectedCounts(expected.GetProperty("findings")) },
            because: $"case {name}'s payload must be a valid Diffra metrics payload counting its expected findings");
    }

    [Fact]
    public void A_payload_without_an_output_directory_is_a_usage_error()
    {
        // Arrange
        var root = Path.Combine(DependencyConformanceTests.CasesDirectory(), "007-clean-graph");

        // Act
        var exit = DepsCommand.Run(["payload", "--root", root], new StringWriter(), new StringWriter());

        // Assert
        exit.Should().Be(2, because: "a payload with nowhere to go has not been written");
    }

    private static JsonElement Payload(string outDirectory) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(outDirectory, "dependency-policy-v1.json"))).RootElement;

    /// <summary>
    /// The payload rules from Diffra's published CLI documentation, as PLAN.md's payload check states
    /// them: string <c>scope</c> and <c>methodology</c>, nothing else but <c>metrics</c>, and every
    /// measurement <c>{value, unit}</c> with an integer value.
    /// </summary>
    private static List<string> ShapeErrors(JsonElement payload)
    {
        var errors = new List<string>();
        var names = payload.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToList();
        if (!names.SequenceEqual(["methodology", "metrics", "scope"]))
        {
            errors.Add($"top-level properties are {string.Join(", ", names)}");
        }

        foreach (var property in new[] { "scope", "methodology" })
        {
            if (!payload.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || value.GetString()!.Length == 0)
            {
                errors.Add($"{property} is not a non-empty string");
            }
        }

        foreach (var file in payload.GetProperty("metrics").GetProperty("files").EnumerateObject())
        {
            foreach (var measurement in file.Value.EnumerateObject())
            {
                var parts = measurement.Value.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToList();
                if (!parts.SequenceEqual(["unit", "value"])
                    || measurement.Value.GetProperty("unit").ValueKind != JsonValueKind.String
                    || !measurement.Value.GetProperty("value").TryGetInt64(out _))
                {
                    errors.Add($"{file.Name} {measurement.Name} is not {{value, unit}} with an integer value");
                }
            }
        }

        return errors;
    }

    private static Dictionary<string, long> Counts(JsonElement payload) =>
        payload.GetProperty("metrics").GetProperty("files").EnumerateObject()
            .SelectMany(file => file.Value.EnumerateObject().Select(m => (Key: $"{file.Name} {m.Name}", Value: m.Value.GetProperty("value").GetInt64())))
            .ToDictionary(m => m.Key, m => m.Value);

    private static Dictionary<string, long> ExpectedCounts(JsonElement findings) =>
        findings.EnumerateArray()
            .GroupBy(f => $"{f.GetProperty("path").GetString()} {f.GetProperty("rule").GetString()}")
            .ToDictionary(g => g.Key, g => (long)g.Count());
}

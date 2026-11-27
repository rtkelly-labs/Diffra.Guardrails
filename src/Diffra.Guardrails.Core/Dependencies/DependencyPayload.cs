using System.Text.Json.Nodes;

namespace Diffra.Guardrails.Core.Dependencies;

/// <summary>
/// Dependency-policy findings as a Diffra metrics payload, <c>{scope, methodology, metrics}</c>.
/// Per graph file, per rule: the number of banned package versions resolved. Counts only, lower is
/// better, sparse by file (a clean graph file is absent), as docs/adapters.md describes for the
/// test-structure payloads. One payload, so DFG601 and DFG602 share one budget.
/// </summary>
public static class DependencyPayload
{
    /// <summary>Changing what a measurement counts is breaking: bump the version.</summary>
    public const string Methodology = "dependency-policy-v1";

    public const string Unit = "violations";

    public static JsonObject Create(string scope, IEnumerable<DependencyFinding> findings)
    {
        var files = new JsonObject();
        foreach (var file in findings.GroupBy(f => f.Path, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var rules = new JsonObject();
            foreach (var rule in file.GroupBy(f => f.Rule, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                rules[rule.Key] = new JsonObject { ["value"] = rule.Count(), ["unit"] = Unit };
            }

            files[file.Key] = rules;
        }

        return new JsonObject
        {
            ["scope"] = scope,
            ["methodology"] = Methodology,
            ["metrics"] = new JsonObject { ["files"] = files },
        };
    }
}

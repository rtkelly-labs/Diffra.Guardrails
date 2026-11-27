using System.Text.Json;
using System.Text.RegularExpressions;

namespace Diffra.Guardrails.Core.Dependencies;

/// <summary>Reads resolved dependency graphs from the files restore and Paket write.</summary>
public static partial class GraphReaders
{
    public const string AssetsFile = "project.assets.json";
    public const string PaketLock = "paket.lock";
    public const string ToolManifest = "dotnet-tools.json";

    /// <summary>
    /// One graph per restore target. Targets are per framework and per runtime, so the same
    /// package appears several times; the check deduplicates findings by file, package and version.
    /// Roots are the project's direct package dependencies and its project references.
    /// </summary>
    public static IEnumerable<DependencyGraph> ReadAssets(string text, string path)
    {
        using var document = JsonDocument.Parse(text);
        var json = document.RootElement;
        var direct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (json.TryGetProperty("project", out var project) && project.TryGetProperty("frameworks", out var frameworks))
        {
            foreach (var framework in frameworks.EnumerateObject())
            {
                if (framework.Value.TryGetProperty("dependencies", out var dependencies))
                {
                    direct.UnionWith(dependencies.EnumerateObject().Select(d => d.Name));
                }
            }
        }

        if (!json.TryGetProperty("targets", out var targets))
        {
            throw new FormatException($"{path}: no 'targets' object; is this a project.assets.json file?");
        }

        var lines = text.Split('\n');
        foreach (var target in targets.EnumerateObject())
        {
            var graph = new DependencyGraph(path);
            foreach (var entry in target.Value.EnumerateObject())
            {
                var slash = entry.Name.IndexOf('/');
                if (slash <= 0)
                {
                    throw new FormatException($"{path}: target entry '{entry.Name}' is not 'Id/Version'.");
                }

                var id = entry.Name[..slash];
                graph.Add(new ResolvedPackage(id, entry.Name[(slash + 1)..], LineOf(lines, $"\"{entry.Name}\"")));
                if (entry.Value.TryGetProperty("type", out var type) && type.GetString() == "project")
                {
                    graph.AddRoot(id);
                }

                if (entry.Value.TryGetProperty("dependencies", out var dependencies))
                {
                    foreach (var dependency in dependencies.EnumerateObject())
                    {
                        graph.AddEdge(id, dependency.Name);
                    }
                }
            }

            foreach (var id in direct)
            {
                graph.AddRoot(id);
            }

            yield return graph;
        }
    }

    /// <summary>
    /// One graph per group in a <c>paket.lock</c>. Only <c>NUGET</c> sections hold packages:
    /// a package at four spaces of indent, its dependencies at six. Roots come from the
    /// <c>nuget</c> lines of the sibling <c>paket.dependencies</c>, when there is one.
    /// </summary>
    public static IEnumerable<DependencyGraph> ReadPaketLock(string text, string path, string? dependenciesText)
    {
        var roots = dependenciesText is null ? null : PaketRoots(dependenciesText);
        var graphs = new List<DependencyGraph>();
        DependencyGraph? graph = null;
        var group = "Main";
        var inNuget = false;
        string? current = null;
        var lines = text.Split('\n');

        DependencyGraph StartGroup(string name)
        {
            var g = new DependencyGraph(path) { RootsKnown = roots is not null };
            foreach (var root in roots?.GetValueOrDefault(name) ?? [])
            {
                g.AddRoot(root);
            }

            graphs.Add(g);
            return g;
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (line.Length > 0 && !char.IsWhiteSpace(line[0]))
            {
                if (line.StartsWith("GROUP ", StringComparison.Ordinal))
                {
                    group = line["GROUP ".Length..].Trim();
                    graph = null;
                }

                inNuget = line == "NUGET";
                current = null;
                continue;
            }

            if (!inNuget)
            {
                continue;
            }

            var package = LockPackage().Match(line);
            if (package.Success)
            {
                graph ??= StartGroup(group);
                current = package.Groups["id"].Value;
                graph.Add(new ResolvedPackage(current, package.Groups["version"].Value.Trim(), i + 1));
                continue;
            }

            var dependency = LockDependency().Match(line);
            if (dependency.Success && current is not null && graph is not null)
            {
                graph.AddEdge(current, dependency.Groups["id"].Value);
            }
        }

        return graphs;
    }

    /// <summary>Tools in a <c>dotnet-tools.json</c> manifest. Neither restore graph includes them.</summary>
    public static DependencyGraph ReadToolManifest(string text, string path)
    {
        using var document = JsonDocument.Parse(text);
        var graph = new DependencyGraph(path);
        if (!document.RootElement.TryGetProperty("tools", out var tools))
        {
            return graph;
        }

        var lines = text.Split('\n');
        foreach (var tool in tools.EnumerateObject())
        {
            var version = tool.Value.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";
            graph.Add(new ResolvedPackage(tool.Name, version, LineOf(lines, $"\"{tool.Name}\"")));
            graph.AddRoot(tool.Name);
        }

        return graph;
    }

    private static Dictionary<string, List<string>> PaketRoots(string text)
    {
        var roots = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var group = "Main";
        foreach (var raw in text.Split('\n'))
        {
            var tokens = raw.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 2 && tokens[0] == "group")
            {
                group = tokens[1];
            }
            else if (tokens.Length >= 2 && tokens[0] is "nuget" or "clitool")
            {
                if (!roots.TryGetValue(group, out var list))
                {
                    roots[group] = list = [];
                }

                list.Add(tokens[1]);
            }
        }

        return roots;
    }

    private static int LineOf(string[] lines, string needle)
    {
        var index = Array.FindIndex(lines, l => l.Contains(needle, StringComparison.Ordinal));
        return index >= 0 ? index + 1 : 1;
    }

    [GeneratedRegex(@"^ {4}(?<id>[^\s(]+) \((?<version>[^)]+)\)")]
    private static partial Regex LockPackage();

    [GeneratedRegex(@"^ {6}(?<id>[^\s(]+) ")]
    private static partial Regex LockDependency();
}

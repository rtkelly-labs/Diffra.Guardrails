namespace Diffra.Guardrails.Core.Dependencies;

/// <summary>The outcome of one check: findings, plus counts that show what was actually examined.</summary>
public sealed record DependencyCheckResult(
    IReadOnlyList<DependencyFinding> Findings,
    int GraphFiles,
    int PackageVersions,
    int Exempted);

/// <summary>
/// Checks every resolved dependency graph under a root against a ban list.
/// DFG601: a banned package version is resolved. DFG602: it is resolved under an exemption that has expired.
/// </summary>
public static class DependencyCheck
{
    public const string Banned = "DFG601";
    public const string ExemptionExpired = "DFG602";

    private static readonly string[] SkippedDirectories = [".git", "node_modules"];

    /// <summary>Graph files under <paramref name="root"/>, as paths relative to it with forward slashes.</summary>
    public static IReadOnlyList<string> Discover(string root, IReadOnlyCollection<string> excludes)
    {
        var found = new List<string>();
        var pending = new Stack<string>([root]);
        var options = new EnumerationOptions { AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = true };
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var sub in Directory.EnumerateDirectories(directory, "*", options))
            {
                var relative = Relative(root, sub);
                if (!SkippedDirectories.Contains(System.IO.Path.GetFileName(sub)) && !IsExcluded(relative, excludes))
                {
                    pending.Push(sub);
                }
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*", options))
            {
                var name = System.IO.Path.GetFileName(file);
                if (name is GraphReaders.AssetsFile or GraphReaders.PaketLock or GraphReaders.ToolManifest
                    && !IsExcluded(Relative(root, file), excludes))
                {
                    found.Add(Relative(root, file));
                }
            }
        }

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    public static IReadOnlyList<DependencyGraph> Read(string root, IEnumerable<string> relativePaths)
    {
        var graphs = new List<DependencyGraph>();
        foreach (var relative in relativePaths)
        {
            var full = System.IO.Path.Combine(root, relative);
            var text = File.ReadAllText(full);
            try
            {
                switch (System.IO.Path.GetFileName(relative))
                {
                    case GraphReaders.AssetsFile:
                        graphs.AddRange(GraphReaders.ReadAssets(text, relative));
                        break;
                    case GraphReaders.PaketLock:
                        var dependencies = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(full)!, "paket.dependencies");
                        graphs.AddRange(GraphReaders.ReadPaketLock(text, relative, File.Exists(dependencies) ? File.ReadAllText(dependencies) : null));
                        break;
                    default:
                        graphs.Add(GraphReaders.ReadToolManifest(text, relative));
                        break;
                }
            }
            catch (System.Text.Json.JsonException e)
            {
                throw new FormatException($"{relative}: not valid JSON ({e.Message})", e);
            }
        }

        return graphs;
    }

    public static DependencyCheckResult Evaluate(
        IReadOnlyList<DependencyGraph> graphs, IReadOnlyList<Ban> bans, DateOnly today, string banFile)
    {
        var findings = new Dictionary<(string, string, string), DependencyFinding>();
        var exempted = new HashSet<(string, string, string)>();
        var seen = new HashSet<(string, string, string)>();
        foreach (var graph in graphs)
        {
            foreach (var package in graph.Packages)
            {
                var key = (graph.Path, package.Id.ToLowerInvariant(), package.Version);
                seen.Add(key);
                var matching = bans
                    .Where(b => string.Equals(b.PackageId, package.Id, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (matching.Count == 0)
                {
                    continue;
                }

                if (!PackageVersion.TryParse(package.Version, out var version))
                {
                    throw new FormatException(
                        $"{graph.Path}:{package.Line}: '{package.Version}' for banned package {package.Id} is not a NuGet version.");
                }

                matching = matching.Where(b => b.Constraint.Matches(version)).ToList();
                var active = matching.FirstOrDefault(b => b.ExemptUntil is null);
                var expired = matching.FirstOrDefault(b => b.ExemptUntil < today);
                if (active is null && expired is null)
                {
                    if (matching.Count > 0)
                    {
                        exempted.Add(key);
                    }

                    continue;
                }

                var finding = active is not null
                    ? BannedFinding(graph, package, active, banFile)
                    : ExpiredFinding(graph, package, expired!, banFile);
                if (!findings.TryGetValue(key, out var existing) || IsShorter(finding.Via, existing.Via))
                {
                    findings[key] = finding;
                }
            }
        }

        var ordered = findings.Values
            .OrderBy(f => f.Path, StringComparer.Ordinal)
            .ThenBy(f => f.Line)
            .ThenBy(f => f.Package, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new DependencyCheckResult(
            ordered, graphs.Select(g => g.Path).Distinct().Count(), seen.Count, exempted.Count);
    }

    private static bool IsShorter(IReadOnlyList<string> candidate, IReadOnlyList<string> existing) =>
        candidate.Count > 0 && (existing.Count == 0 || candidate.Count < existing.Count);

    private static DependencyFinding BannedFinding(DependencyGraph graph, ResolvedPackage package, Ban ban, string banFile)
    {
        var via = graph.PathTo(package.Id);
        return new DependencyFinding(
            Banned,
            graph.Path,
            package.Line,
            package.Id,
            package.Version,
            via,
            $"{package.Id} {package.Version} is banned ({ban.Constraint.Text}): {Sentence(ban.Reason)}{Through(via)}",
            $"Remove the dependency that brings in {package.Id}{Root(via)}"
                + (ban.Constraint.IsAny ? ", or replace it" : $", or move to a version outside '{ban.Constraint.Text}'")
                + $". To keep it for now, add 'exempt-until: yyyy-MM-dd' to line {ban.Line} of {banFile}.");
    }

    private static DependencyFinding ExpiredFinding(DependencyGraph graph, ResolvedPackage package, Ban ban, string banFile)
    {
        var via = graph.PathTo(package.Id);
        return new DependencyFinding(
            ExemptionExpired,
            graph.Path,
            package.Line,
            package.Id,
            package.Version,
            via,
            $"{package.Id} {package.Version} is banned and its exemption expired on {ban.ExemptUntil:yyyy-MM-dd}: "
                + $"{Sentence(ban.Reason)}{Through(via)}",
            $"Remove the dependency that brings in {package.Id}{Root(via)}, or review the exemption again and "
                + $"move 'exempt-until' on line {ban.Line} of {banFile} to a new date.");
    }

    private static string Sentence(string reason) => reason.TrimEnd('.') + ".";

    private static string Through(IReadOnlyList<string> via) =>
        via.Count > 1 ? $" It is resolved through {string.Join(" -> ", via)}." : "";

    private static string Root(IReadOnlyList<string> via) => via.Count > 1 ? $" ({via[0]})" : "";

    private static string Relative(string root, string path) =>
        System.IO.Path.GetRelativePath(root, path).Replace('\\', '/');

    private static bool IsExcluded(string relative, IReadOnlyCollection<string> excludes) =>
        excludes.Any(e =>
        {
            var prefix = e.Replace('\\', '/').TrimEnd('/');
            return relative == prefix || relative.StartsWith(prefix + "/", StringComparison.Ordinal);
        });
}

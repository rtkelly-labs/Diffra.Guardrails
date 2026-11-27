using System.Globalization;
using System.Text.Json;
using Diffra.Guardrails.Core.Dependencies;

namespace Diffra.Guardrails.Tool;

/// <summary>
/// <c>deps check</c>: fail when a banned package is resolved anywhere under the root.
/// Exit 0 when clean, 1 when a gated finding remains, 2 for usage errors, an unreadable ban
/// file, or no dependency graph at all (an unrestored repository is not a clean one).
/// <c>deps payload</c>: run the same check and write the findings as a Diffra metrics payload.
/// It measures rather than gates: exit 0 once the payload is written, whatever it counts.
/// </summary>
public static class DepsCommand
{
    public const string DefaultBanFile = "banned.dependencies";

    private const string Common =
        "[--root <dir>] [--bans <file>] [--exclude <path>]... [--graph <file>]... [--today yyyy-MM-dd]";

    public const string Usage =
        "usage: diffra-guardrails deps check " + Common + " [--format agent|json]\n"
        + "       diffra-guardrails deps payload " + Common + " --out <dir> [--scope <name>]";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        if (args.Count == 0 || args[0] is not ("check" or "payload"))
        {
            error.WriteLine(Usage);
            return 2;
        }

        var verb = args[0];
        var name = $"deps {verb}";
        string root = ".", format = "agent";
        string? bans = null, outDirectory = null, scope = null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var excludes = new List<string>();
        var graphs = new List<string>();
        for (var i = 1; i < args.Count; i++)
        {
            if (i + 1 >= args.Count)
            {
                error.WriteLine($"{name}: '{args[i]}' needs a value.\n{Usage}");
                return 2;
            }

            var value = args[++i];
            switch (args[i - 1])
            {
                case "--root":
                    root = value;
                    break;
                case "--bans":
                    bans = value;
                    break;
                case "--exclude":
                    excludes.Add(value);
                    break;
                case "--graph":
                    graphs.Add(value);
                    break;
                case "--format" when verb == "check" && value is "agent" or "json":
                    format = value;
                    break;
                case "--out" when verb == "payload":
                    outDirectory = value;
                    break;
                case "--scope" when verb == "payload" && value.Trim().Length > 0:
                    scope = value;
                    break;
                case "--today" when DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date):
                    today = date;
                    break;
                default:
                    error.WriteLine($"{name}: unknown option or bad value '{args[i - 1]} {value}'.\n{Usage}");
                    return 2;
            }
        }

        if (verb == "payload" && outDirectory is null)
        {
            error.WriteLine($"{name}: --out <dir> is required.\n{Usage}");
            return 2;
        }

        root = Path.GetFullPath(root);
        if (!Directory.Exists(root))
        {
            error.WriteLine($"{name}: root '{root}' is not a directory.");
            return 2;
        }

        var banPath = Path.GetFullPath(bans ?? Path.Combine(root, DefaultBanFile));
        if (!File.Exists(banPath))
        {
            error.WriteLine($"{name}: no ban file at {banPath}. Pass --bans <file> or add {DefaultBanFile} to the root.");
            return 2;
        }

        var banName = Path.GetFileName(banPath);
        try
        {
            var banList = BanList.Parse(File.ReadAllText(banPath), banName);
            var files = graphs.Count == 0 ? DependencyCheck.Discover(root, excludes) : Named(root, graphs);
            if (files.Count == 0)
            {
                error.WriteLine(
                    $"{name}: no {GraphReaders.AssetsFile}, {GraphReaders.PaketLock} or {GraphReaders.ToolManifest} "
                    + "found under the root. Restore first (dotnet restore or paket install); an unrestored "
                    + "repository is not evidence of a clean one.");
                return 2;
            }

            var result = DependencyCheck.Evaluate(DependencyCheck.Read(root, files), banList, today, banName);
            var summary =
                $"{name}: {banList.Count} bans, {result.GraphFiles} graph files, {result.PackageVersions} "
                + $"package versions, {result.Findings.Count} findings, {result.Exempted} exempted.";
            if (verb == "payload")
            {
                var payloadDirectory = Path.GetFullPath(outDirectory!);
                Directory.CreateDirectory(payloadDirectory);
                var payloadPath = Path.Combine(payloadDirectory, DependencyPayload.Methodology + ".json");
                var payload = DependencyPayload.Create(scope ?? Path.GetFileName(root), result.Findings);
                File.WriteAllText(payloadPath, payload.ToJsonString(Json) + "\n");
                error.WriteLine($"{summary} Wrote {payloadPath}.");
                return 0;
            }

            if (format == "json")
            {
                output.WriteLine(JsonSerializer.Serialize(result.Findings, Json));
            }
            else
            {
                foreach (var finding in result.Findings)
                {
                    output.WriteLine($"{finding.Path}:{finding.Line} {finding.Rule} {finding.Message}");
                    output.WriteLine($"fix: {finding.Fix}");
                }
            }

            error.WriteLine(summary);
            return result.Findings.Any(f => f.Gated) ? 1 : 0;
        }
        catch (Exception e) when (e is BanFileException or FormatException or IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"{name}: {e.Message}");
            return 2;
        }
    }

    /// <summary>
    /// Graph files named with <c>--graph</c>, relative to the root so findings name them as discovery would.
    /// A named file that is missing, or is not a graph file, is an error: it must not pass as clean.
    /// </summary>
    private static List<string> Named(string root, IEnumerable<string> graphs) =>
        graphs.Select(graph =>
            {
                var full = Path.GetFullPath(graph, root);
                if (!File.Exists(full))
                {
                    throw new FileNotFoundException($"--graph {graph}: no such file.");
                }

                if (Path.GetFileName(full) is not (GraphReaders.AssetsFile or GraphReaders.PaketLock or GraphReaders.ToolManifest))
                {
                    throw new FormatException(
                        $"--graph {graph}: not a {GraphReaders.AssetsFile}, {GraphReaders.PaketLock} or {GraphReaders.ToolManifest}.");
                }

                return Path.GetRelativePath(root, full).Replace('\\', '/');
            })
            .Distinct(StringComparer.Ordinal)
            .ToList();
}

using System.Globalization;
using System.Text.Json;
using Diffra.Guardrails.Core.Dependencies;

namespace Diffra.Guardrails.Tool;

/// <summary>
/// <c>deps check</c>: fail when a banned package is resolved anywhere under the root.
/// Exit 0 when clean, 1 when a gated finding remains, 2 for usage errors, an unreadable ban
/// file, or no dependency graph at all (an unrestored repository is not a clean one).
/// </summary>
public static class DepsCommand
{
    public const string DefaultBanFile = "banned.dependencies";

    public const string Usage =
        "usage: diffra-guardrails deps check [--root <dir>] [--bans <file>] [--exclude <path>]... "
        + "[--format agent|json] [--today yyyy-MM-dd]";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        if (args.Count == 0 || args[0] != "check")
        {
            error.WriteLine(Usage);
            return 2;
        }

        string root = ".", format = "agent";
        string? bans = null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var excludes = new List<string>();
        for (var i = 1; i < args.Count; i++)
        {
            if (i + 1 >= args.Count)
            {
                error.WriteLine($"deps check: '{args[i]}' needs a value.\n{Usage}");
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
                case "--format" when value is "agent" or "json":
                    format = value;
                    break;
                case "--today" when DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date):
                    today = date;
                    break;
                default:
                    error.WriteLine($"deps check: unknown option or bad value '{args[i - 1]} {value}'.\n{Usage}");
                    return 2;
            }
        }

        root = Path.GetFullPath(root);
        if (!Directory.Exists(root))
        {
            error.WriteLine($"deps check: root '{root}' is not a directory.");
            return 2;
        }

        var banPath = Path.GetFullPath(bans ?? Path.Combine(root, DefaultBanFile));
        if (!File.Exists(banPath))
        {
            error.WriteLine($"deps check: no ban file at {banPath}. Pass --bans <file> or add {DefaultBanFile} to the root.");
            return 2;
        }

        var banName = Path.GetFileName(banPath);
        try
        {
            var banList = BanList.Parse(File.ReadAllText(banPath), banName);
            var files = DependencyCheck.Discover(root, excludes);
            if (files.Count == 0)
            {
                error.WriteLine(
                    $"deps check: no {GraphReaders.AssetsFile}, {GraphReaders.PaketLock} or {GraphReaders.ToolManifest} "
                    + "found under the root. Restore first (dotnet restore or paket install); an unrestored "
                    + "repository is not evidence of a clean one.");
                return 2;
            }

            var result = DependencyCheck.Evaluate(DependencyCheck.Read(root, files), banList, today, banName);
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

            error.WriteLine(
                $"deps check: {banList.Count} bans, {result.GraphFiles} graph files, {result.PackageVersions} "
                + $"package versions, {result.Findings.Count} findings, {result.Exempted} exempted.");
            return result.Findings.Any(f => f.Gated) ? 1 : 0;
        }
        catch (Exception e) when (e is BanFileException or FormatException or IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"deps check: {e.Message}");
            return 2;
        }
    }
}

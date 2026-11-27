using System.Globalization;

namespace Diffra.Guardrails.Core.Dependencies;

/// <summary>One line of a ban file: a package, the versions it covers, why, and an optional exemption date.</summary>
public sealed record Ban(string PackageId, VersionConstraint Constraint, string Reason, DateOnly? ExemptUntil, int Line);

/// <summary>A ban file could not be read. The run stops: a skipped line would be a silent pass.</summary>
public sealed class BanFileException(string message) : Exception(message);

/// <summary>
/// Reads a ban file. The syntax is Paket's <c>paket.dependencies</c> <c>nuget</c> line, plus a
/// required <c>// reason</c> and an optional <c>exempt-until: yyyy-MM-dd</c>:
/// <code>
/// nuget FluentAssertions &gt;= 8.0 // Xceed commercial licence from 8.0
/// nuget Verify exempt-until: 2027-11-30 // SmallRevenue licence, renew yearly
/// </code>
/// Blank lines and lines starting with <c>//</c> or <c>#</c> are comments. Anything else is an error.
/// </summary>
public static class BanList
{
    private const string ExemptKeyword = "exempt-until:";

    public static IReadOnlyList<Ban> Parse(string text, string fileName)
    {
        var bans = new List<Ban>();
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('#'))
            {
                continue;
            }

            try
            {
                bans.Add(ParseLine(line, i + 1));
            }
            catch (FormatException e)
            {
                throw new BanFileException($"{fileName}:{i + 1}: {e.Message}");
            }
        }

        return bans;
    }

    private static Ban ParseLine(string line, int number)
    {
        var comment = line.IndexOf("//", StringComparison.Ordinal);
        var reason = comment >= 0 ? line[(comment + 2)..].Trim() : "";
        if (reason.Length == 0)
        {
            throw new FormatException("every ban needs a reason after '//', so the finding can say why.");
        }

        var tokens = new List<string>(line[..comment].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (tokens.Count < 2 || tokens[0] != "nuget")
        {
            throw new FormatException("expected 'nuget <PackageId> [version constraint] [exempt-until: yyyy-MM-dd] // reason'.");
        }

        DateOnly? exemptUntil = null;
        var exempt = tokens.FindIndex(t => t.StartsWith(ExemptKeyword, StringComparison.Ordinal));
        if (exempt >= 0)
        {
            var inline = tokens[exempt][ExemptKeyword.Length..];
            var dateText = inline.Length > 0 ? inline : exempt + 1 < tokens.Count ? tokens[exempt + 1] : "";
            var used = inline.Length > 0 ? 1 : 2;
            if (exempt + used != tokens.Count)
            {
                throw new FormatException("'exempt-until:' must come last, after the version constraint.");
            }

            if (!DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                throw new FormatException($"'{dateText}' is not a date; write exempt-until as yyyy-MM-dd.");
            }

            exemptUntil = date;
            tokens.RemoveRange(exempt, used);
        }

        return new Ban(tokens[1], VersionConstraint.Parse(tokens.Skip(2).ToList()), reason, exemptUntil, number);
    }
}

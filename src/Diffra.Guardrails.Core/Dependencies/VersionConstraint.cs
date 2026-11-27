namespace Diffra.Guardrails.Core.Dependencies;

/// <summary>
/// A Paket version constraint, read as the set of versions a ban covers. Supported forms: nothing
/// (every version), <c>1.2.3</c> or <c>= 1.2.3</c> (exactly that version), any mix of
/// <c>&gt;= &gt; &lt;= &lt;</c> bounds (all must hold), and <c>~&gt; 1.2</c> (<c>&gt;= 1.2 &lt; 2.0</c>).
/// </summary>
/// <remarks>
/// Bans match prereleases. A lower bound with no prerelease label is compared against the
/// version's release part, so <c>&gt;= 8.0</c> covers <c>8.0.0-rc.1</c>; upper bounds use full
/// ordering, so <c>&lt; 4.20.2</c> covers <c>4.20.2-beta</c>. Both lean towards banning.
/// </remarks>
public sealed class VersionConstraint
{
    private static readonly string[] PrereleaseFlags = ["alpha", "beta", "rc", "prerelease"];

    private readonly IReadOnlyList<(string Op, PackageVersion Version)> _bounds;

    private VersionConstraint(string text, IReadOnlyList<(string Op, PackageVersion Version)> bounds)
    {
        Text = text;
        _bounds = bounds;
    }

    /// <summary>The constraint as written in the ban file, or "any version".</summary>
    public string Text { get; }

    public static VersionConstraint Parse(IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return new VersionConstraint("any version", []);
        }

        var text = string.Join(' ', tokens);
        if (tokens[0] == "~>")
        {
            if (tokens.Count != 2)
            {
                throw new FormatException($"'{text}': '~>' takes exactly one version.");
            }

            var lower = ParseVersion(tokens[1], text);
            return new VersionConstraint(text, [(">=", lower), ("<", lower.TwiddleWakkaUpperBound())]);
        }

        if (tokens.Count == 1 && char.IsAsciiDigit(tokens[0][0]))
        {
            return new VersionConstraint(text, [("=", ParseVersion(tokens[0], text))]);
        }

        var bounds = new List<(string, PackageVersion)>();
        for (var i = 0; i < tokens.Count; i += 2)
        {
            var op = tokens[i];
            if (op == "==")
            {
                throw new FormatException(
                    $"'{text}': '==' is a Paket conflict override, not a version set; use '= x.y.z' for one version.");
            }

            if (PrereleaseFlags.Contains(op, StringComparer.OrdinalIgnoreCase))
            {
                throw new FormatException(
                    $"'{text}': prerelease flags are not needed, because bans already cover prereleases.");
            }

            if (op is not ("=" or ">=" or ">" or "<=" or "<"))
            {
                throw new FormatException($"'{text}': '{op}' is not a version operator (use = >= > <= < or ~>).");
            }

            if (i + 1 >= tokens.Count)
            {
                throw new FormatException($"'{text}': '{op}' needs a version after it.");
            }

            if (op == "=" && tokens.Count != 2)
            {
                throw new FormatException($"'{text}': '=' cannot be combined with other bounds.");
            }

            bounds.Add((op, ParseVersion(tokens[i + 1], text)));
        }

        return new VersionConstraint(text, bounds);
    }

    /// <summary>True when the ban covers every version.</summary>
    public bool IsAny => _bounds.Count == 0;

    public bool Matches(PackageVersion version) => _bounds.All(b => Holds(b.Op, b.Version, version));

    private static bool Holds(string op, PackageVersion bound, PackageVersion version)
    {
        var lowerSide = bound.IsPrerelease ? version : version.Release;
        return op switch
        {
            "=" => version.CompareTo(bound) == 0,
            ">=" => lowerSide.CompareTo(bound) >= 0,
            ">" => lowerSide.CompareTo(bound) > 0,
            "<=" => version.CompareTo(bound) <= 0,
            "<" => version.CompareTo(bound) < 0,
            _ => throw new InvalidOperationException(op),
        };
    }

    private static PackageVersion ParseVersion(string token, string text) =>
        PackageVersion.TryParse(token, out var version)
            ? version
            : throw new FormatException($"'{text}': '{token}' is not a NuGet version.");
}

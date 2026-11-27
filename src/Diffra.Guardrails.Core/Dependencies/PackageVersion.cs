namespace Diffra.Guardrails.Core.Dependencies;

/// <summary>
/// A NuGet package version: up to four numeric parts, an optional prerelease label and ignored
/// build metadata. Ordering follows SemVer 2.0 as NuGet applies it: missing parts are zero, a
/// release sorts above its prereleases, and labels compare part by part (numbers numerically,
/// text case-insensitively, numbers below text).
/// </summary>
public sealed class PackageVersion : IComparable<PackageVersion>
{
    private readonly long[] _release;
    private readonly string[] _prerelease;

    private PackageVersion(long[] release, string[] prerelease, string original)
    {
        _release = release;
        _prerelease = prerelease;
        Original = original;
    }

    public string Original { get; }

    public bool IsPrerelease => _prerelease.Length > 0;

    /// <summary>The same version without its prerelease label, so 8.0.0-rc.1 gives 8.0.0.</summary>
    public PackageVersion Release => IsPrerelease ? new PackageVersion(_release, [], Original) : this;

    public static bool TryParse(string text, out PackageVersion version)
    {
        version = null!;
        var value = text.Trim();
        var plus = value.IndexOf('+');
        if (plus >= 0)
        {
            value = value[..plus];
        }

        var dash = value.IndexOf('-');
        var core = dash >= 0 ? value[..dash] : value;
        var label = dash >= 0 ? value[(dash + 1)..] : "";
        var parts = core.Split('.');
        if (parts.Length is < 1 or > 4 || (dash >= 0 && label.Length == 0))
        {
            return false;
        }

        var release = new long[4];
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0 || !parts[i].All(char.IsAsciiDigit) || !long.TryParse(parts[i], out release[i]))
            {
                return false;
            }
        }

        var prerelease = label.Length == 0 ? [] : label.Split('.');
        if (prerelease.Any(p => p.Length == 0))
        {
            return false;
        }

        version = new PackageVersion(release, prerelease, text.Trim());
        return true;
    }

    public static PackageVersion Parse(string text) =>
        TryParse(text, out var version) ? version : throw new FormatException($"'{text}' is not a NuGet version.");

    /// <summary>The upper bound of Paket's <c>~&gt;</c>: bump the second-to-last given part.</summary>
    public PackageVersion TwiddleWakkaUpperBound()
    {
        var given = Original.Split('-', '+')[0].Split('.').Length;
        var bump = Math.Max(0, given - 2);
        var release = new long[4];
        Array.Copy(_release, release, bump);
        release[bump] = _release[bump] + 1;
        return new PackageVersion(release, [], string.Join('.', release.Take(Math.Max(given - 1, 1))));
    }

    public int CompareTo(PackageVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        for (var i = 0; i < 4; i++)
        {
            var c = _release[i].CompareTo(other._release[i]);
            if (c != 0)
            {
                return c;
            }
        }

        if (!IsPrerelease || !other.IsPrerelease)
        {
            return other.IsPrerelease.CompareTo(IsPrerelease);
        }

        for (var i = 0; i < Math.Min(_prerelease.Length, other._prerelease.Length); i++)
        {
            var c = CompareLabel(_prerelease[i], other._prerelease[i]);
            if (c != 0)
            {
                return c;
            }
        }

        return _prerelease.Length.CompareTo(other._prerelease.Length);
    }

    private static int CompareLabel(string a, string b)
    {
        var aNumber = a.All(char.IsAsciiDigit) && long.TryParse(a, out var x) ? x : (long?)null;
        var bNumber = b.All(char.IsAsciiDigit) && long.TryParse(b, out var y) ? y : (long?)null;
        return (aNumber, bNumber) switch
        {
            ({ } l, { } r) => l.CompareTo(r),
            ({ }, null) => -1,
            (null, { }) => 1,
            _ => string.Compare(a, b, StringComparison.OrdinalIgnoreCase),
        };
    }

    public override string ToString() => Original;
}

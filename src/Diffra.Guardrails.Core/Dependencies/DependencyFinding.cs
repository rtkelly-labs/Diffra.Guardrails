namespace Diffra.Guardrails.Core.Dependencies;

/// <summary>
/// A dependency-policy finding. The common fields match the finding contract in
/// docs/adapters.md; <c>package</c>, <c>version</c> and <c>via</c> are specific to DFG6xx.
/// </summary>
public sealed record DependencyFinding(
    string Rule,
    string Path,
    int Line,
    string Package,
    string Version,
    IReadOnlyList<string> Via,
    string Message,
    string Fix)
{
    public int EndLine => Line;

    public string Severity => "error";

    public bool Gated => true;
}

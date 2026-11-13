namespace Diffra.Guardrails.Core.Models;

public sealed record Finding(
    string Rule,
    string Path,
    int Line,
    int EndLine,
    string? Block,
    string Message,
    string Fix,
    string Severity = "error",
    bool Gated = true,
    string Scope = "new",
    string? Test = null);

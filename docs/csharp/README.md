# Diffra Guardrails for C# & .NET

Status: Active. Conforms to [VISION.md](../../VISION.md).

`Diffra.Guardrails.CSharp` provides deterministic Roslyn analyzers, code fixes, and structural guardrails for modern C# (12, 13, and 14) running on .NET 10 and .NET 11.

---

## 1. Quickstart (Level 1)

### Installation

Add the analyzer package to your project or directory build props:

```xml
<ItemGroup>
  <PackageReference Include="Diffra.Guardrails.CSharp" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

### Immediate CLI Verification

Run the syntax checker directly over any C# codebase:

```bash
diffra-guardrails check src/ --format agent
```

### Recommended Default (`Directory.Build.props`)

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <!-- Pragmatic bundle: functional core, scoped high-performance builders -->
    <DiffraGuardrailsBundle>pragmatic</DiffraGuardrailsBundle>
  </PropertyGroup>
</Project>
```

### Granular Configuration via `.editorconfig`

Configure different style bundles per project or folder across the .NET solution:

```ini
# Root configuration: Pragmatic Default
[*.cs]
diffra_guardrails_bundle = pragmatic
dotnet_diagnostic.DFG201.severity = error
dotnet_diagnostic.DFG202.severity = error
dotnet_diagnostic.DFG203.severity = error
dotnet_diagnostic.DFG204.severity = error
dotnet_diagnostic.DFG205.severity = error

# Domain Layer: Pure Functional
[src/Ordering.Domain/**/*.cs]
diffra_guardrails_bundle = functional
dotnet_diagnostic.DFG202.severity = error

# Compiler / Hot Path / Analyzers: High Performance
[src/Diffra.Guardrails.CSharp/**/*.cs]
diffra_guardrails_bundle = high-performance
diffra_guardrails_allow_scoped_builders = true
dotnet_diagnostic.DFG204.severity = error
```

---

## 2. Progressive Documentation Path

* **[Functional C# Guidance (Level 2)](guidance.md)**: Idiomatic C# patterns, collection expressions `[.. a, .. b]`, switch expressions, static lambdas, and property design.
* **[Roslyn Analyzer Architecture (Level 3)](roslyn-analyzer.md)**: Technical design of `DiagnosticAnalyzer` and `CodeFixProvider` implementations, syntax reject guards, and AST node visitors.
* **[Zero-Allocation Performance Tuning (Level 3)](performance-tuning.md)**: Upstream inspirations (NetAnalyzers, Meziantou, Roslynator) and our 5-step playbook for writing zero-allocation Roslyn analyzers.
* **[Rule Catalog & Specifications](../rules/README.md)**: Complete specifications for DFG101–DFG108 and DFG201–DFG205.

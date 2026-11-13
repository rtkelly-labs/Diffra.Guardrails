# Diffra Guardrails for F# (.NET)

Status: Roadmap & Adapter Architecture, November 2026. Conforms to [VISION.md](../../VISION.md).

`Diffra.Guardrails.FSharp` brings deterministic structural boundaries to F# test suites and domain implementations, enforcing test structure constraints (DFG1xx) and functional immutability principles (DFG2xx) via `FSharp.Analyzers.SDK`.

---

## 1. Quickstart (Level 1)

### Integration via `FSharp.Analyzers.SDK`

Add the analyzer package to your F# project:

```xml
<ItemGroup>
  <PackageReference Include="Diffra.Guardrails.FSharp" Version="1.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### CLI Verification

Verify F# files using the Diffra Guardrails CLI:

```bash
diffra-guardrails check src/ --format agent
```

---

## 2. Progressive Documentation Path

* **[F# Tooling & Functional Plan (Level 2 & 3)](tooling-and-plan.md)**: Deep dive into the F# Compiler Service (FCS) `ParsedInput` AST traversal, Expecto test framework integration, marker parsing, and mapping of rules DFG101–DFG205 to F# idioms.
* **[Rule Catalog & Specifications](../rules/README.md)**: Complete specifications for DFG101–DFG108 and DFG201–DFG205.
* **[The Adapter Contract (The Six Questions)](../architecture/adapters.md)**: Universal requirements every language adapter must satisfy.

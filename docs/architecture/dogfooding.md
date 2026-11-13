# Dogfooding Diffra Guardrails

Status: Engineering Protocol & Policy, November 2026. Conforms to [VISION.md](../../VISION.md).
Scope: All packages and subprojects within Diffra Guardrails.

---

## 1. The Dogfooding Principle

**We run our own guardrails on ourselves on every build.**

If a rule is too brittle, produces false positives, or creates unacceptable performance friction for engineers and tools writing analyzers, it is not fit for general adoption. By dogfooding Diffra Guardrails across our own packages, we guarantee:

1. **Practicality:** Constraints are proven in real development workflows.
2. **Performance Authenticity:** The high-performance bundle is validated against compiler and AST pass requirements.
3. **Zero Drift:** Rule contracts and diagnostics are tested against our own code before any release.

---

## 2. Multi-Language Dogfooding Matrix

The repository contains distinct sub-domains, each demonstrating a specific architectural style bundle:

| Package / Subsystem | Primary Function | Active Bundle | Architectural Enforcement |
| :--- | :--- | :--- | :--- |
| **Core Models** (`src/Diffra.Guardrails.Core`) | Language-neutral finding models & contracts | `functional` | Pure records, immutable structures, zero in-place mutations. |
| **C# Analyzer** (`src/Diffra.Guardrails.CSharp`) | Roslyn AST syntax and semantic analysis | `high-performance` | Zero-allocation AST walks, static closures (`DFG204`), scoped builder buffers. |
| **CLI Runner** (`src/Diffra.Guardrails.Tool`) | Command-line execution & payload export | `pragmatic` | Single-assignment flow, zero swallowed exceptions (`DFG203`), diagnostic exit codes. |
| **TypeScript Plugin** (`eslint-plugin-diffra`) | ESLint AST visitors for Vitest/Jest | `functional` | Pure visitor functions, immutable AST inspection, zero array mutations. |
| **Python Checker** (`diffra-guardrails`) | Zero-dependency AST/tokenize runner | `pragmatic` | Safe exception propagation, pure AST visitor transforms. |
| **Test Suites** (`tests/`, `conformance/`) | Conformance test corpus and unit tests | `functional` | Strict AAA markers (`DFG101`), single Act budget (`DFG103`), descriptive assertion messages (`DFG106`). |

---

## 3. Enforcement in CI & Local Builds

Every commit and pull request enforces self-dogfooding at build time:

1. **Syntax & Style Gates:** The checker is invoked over our own source tree on every change.
2. **Zero False Positives:** Any finding raised against our own source tree blocks CI until either the code is cleaned or a genuine rule bug is resolved.
3. **Language-Specific Enforcements:**
   * For .NET projects, the Roslyn analyzer is included directly as a project analyzer. (See [csharp/performance-tuning.md](../csharp/performance-tuning.md)).
   * For TypeScript, ESLint runs `eslint-plugin-diffra` against the plugin source. (See [typescript/tooling-and-plan.md](../typescript/tooling-and-plan.md)).
   * For Python, `pytest` executes with `--diffra-check` enabled. (See [python/tooling-and-plan.md](../python/tooling-and-plan.md)).

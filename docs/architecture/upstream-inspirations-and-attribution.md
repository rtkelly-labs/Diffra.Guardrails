# Upstream Inspirations, Lineage, and Attribution

Status: Reference & Attribution Guide, November 2026. Conforms to [VISION.md](../../VISION.md).
Scope: Universal attribution across C#, F#, TypeScript, and Python adapters.

---

## 1. Executive Summary

Diffra Guardrails stands on the shoulders of foundational static analysis projects across the compiler and software quality communities. While Diffra Guardrails uniquely imposes structural test constraints (DFG1xx) and functional/performance architectural boundaries (DFG2xx) paired with Diffra ratchet budgets, several diagnostic heuristics and AST validation techniques draw inspiration from proven open-source analyzers.

This document formally records upstream inspirations across all language ecosystems, credits original authors, documents permissive licensing terms (MIT / Apache 2.0), and establishes attribution guidelines for all code within this repository.

---

## 2. Multi-Language Upstream Lineage

### 2.1 C# & .NET Ecosystem

* **Microsoft NetAnalyzers & .NET SDK (`CA1024`, `CA1031`, `IDE0320`, `IDE0066`):**
  * *License:* MIT License (Copyright &copy; .NET Foundation and Contributors).
  * *Inspiration:* Core heuristics for property behavior limits (DFG201), broad exception catching (DFG203), and static lambdas (DFG204).
* **Meziantou.Analyzer (`MA0016`, `MA0038`, `MA0045`):**
  * *License:* MIT License (Copyright &copy; G&eacute;rald Barr&eacute;).
  * *Inspiration:* Immutability preferences (DFG202), static closure detection (DFG204), and unswallowed exception propagation (DFG203).
* **Roslynator (`RCS1047`, `RCS1218`, `RCS1258`):**
  * *License:* Apache License 2.0 (Copyright &copy; Josef Pihrt).
  * *Inspiration:* High-speed AST token pattern-matching, `if`-to-`switch` expression collapse (DFG205), and static lambda syntax guards.
* **ErrorProne.NET & SharpSource (`ERP021`, `ERP022`):**
  * *License:* Apache License 2.0 / MIT.
  * *Inspiration:* Exception stack-trace preservation and mutating collection member detection.
* *Detailed C# implementation and in-code attribution guidelines are in [csharp/performance-tuning.md](../csharp/performance-tuning.md).*

---

### 2.2 TypeScript & JavaScript Ecosystem

* **`eslint-plugin-functional`:**
  * *Repository:* <https://github.com/eslint-community/eslint-plugin-functional>
  * *License:* MIT License (Copyright &copy; Jonas van den Berg and contributors).
  * *Inspiration:* Rules `no-mutating-methods` (informing DFG202), `prefer-readonly-type`, and `no-let` (informing DFG205 single-assignment).
* **`eslint-plugin-vitest` & `eslint-plugin-jest`:**
  * *Repository:* <https://github.com/veritem/eslint-plugin-vitest>
  * *License:* MIT License.
  * *Inspiration:* Structural test boundaries, assertion counting heuristics (DFG105), and test-block AST isolation (DFG101–DFG104).
* **`eslint-plugin-unicorn`:**
  * *Repository:* <https://github.com/sindresorhus/eslint-plugin-unicorn>
  * *License:* MIT License (Copyright &copy; Sindre Sorhus).
  * *Inspiration:* `catch-error-name` and defensive error rethrowing patterns informing DFG203.

---

### 2.3 Python Ecosystem

* **`flake8-bugbear`:**
  * *Repository:* <https://github.com/PyCQA/flake8-bugbear>
  * *License:* MIT License (Copyright &copy; Łukasz Langa and contributors).
  * *Inspiration:* Rule `B001` (Do not use bare `except:`), informing DFG203 defensive exception discipline.
* **`flake8-immutable` & `flake8-pie`:**
  * *License:* MIT License.
  * *Inspiration:* Banning default mutable argument assignments and encouraging immutable sequence expressions (DFG202).
* **`ruff`:**
  * *Repository:* <https://github.com/astral-sh/ruff>
  * *License:* MIT / Apache 2.0.
  * *Inspiration:* High-speed zero-dependency AST traversal patterns and unified linter diagnostics.

---

### 2.4 F# Ecosystem

* **`FSharp.Analyzers.SDK` & `G-Research.FSharp.Analyzers`:**
  * *Repository:* <https://github.com/ionide/FSharp.Analyzers.SDK>
  * *License:* Apache License 2.0 (Copyright &copy; Ionide & G-Research).
  * *Inspiration:* FCS `ParsedInput` AST traversal, compiler service integration, and Expecto test suite analysis.

---

## 3. License Audit & Attribution Policy

1. **Permissive Only:** All upstream inspirations utilize permissive open-source licenses (MIT or Apache 2.0). No GPL, AGPL, or copyleft code is vendored or incorporated.
2. **In-Code Headers:** When any language adapter directly adapts an algorithm or AST heuristic from an upstream project, an explicit attribution comment documenting the source repository, original author, and license must precede the implementation.
3. **Clean Room Independent Rule Contracts:** While heuristics and syntax parsing ideas draw inspiration from existing tools, rule contracts (`RuleContracts.cs`), public messages, and fix texts are original public contracts maintained solely by Diffra Guardrails.

# Diffra Guardrails: Rule Catalogue & Contract

Status: Current. Conforms to [VISION.md](../../VISION.md).

Diffra Guardrails provides deterministic structural boundaries across testing structure and code architecture. Every rule is specified as an invariant with an immutable identifier, a stable finding message, and an actionable fix recommendation.

---

## 1. Rule Organization & Progressive Disclosure

The rule catalogue is structured across five rule families:

```mermaid
flowchart TD
    subgraph DFG1xx["DFG1xx: Test Shape & Readability"]
        direction TB
        R101["DFG101: Structure Markers"]
        R102["DFG102: Marker Integrity"]
        R103["DFG103: Act Statement Budget"]
        R104["DFG104: Assertion Placement"]
        R105["DFG105: Assertion Budget"]
        R106["DFG106: Failure Message"]
        R107["DFG107: Failure Message Quality"]
        R108["DFG108: No Test Control Flow"]
        R109["DFG109: Mock Budget (proposed)"]
    end

    subgraph DFG2xx["DFG2xx: Functional Architecture & Clean Code"]
        direction TB
        R201["DFG201: No Behavior in Properties"]
        R202["DFG202: Immutability & No Mutations"]
        R203["DFG203: Defensive Exception Discipline"]
        R204["DFG204: Zero-Allocation Closures"]
        R205["DFG205: Single Assignment Expressions"]
    end

    subgraph DFG3xx["DFG3xx: Contract & Data Purity"]
        direction TB
        R301["DFG301: Pure Schemas (No Methods)"]
        R302["DFG302: Auto-Properties Only"]
        R303["DFG303: Immutable Collections"]
        R304["DFG304: Permitted Declarations Only"]
    end

    subgraph DFG4xx["DFG4xx: Deep Immutability & Type Safety"]
        direction TB
        R401["DFG401: Deep Immutability"]
        R402["DFG402: Mandatory Init Properties"]
        R403["DFG403: Banned Non-Deterministic APIs"]
    end

    subgraph DFG5xx["DFG5xx: Algebraic Types & Exhaustive Matching"]
        direction TB
        R501["DFG501: Full-Time Exhaustive Matching"]
        R502["DFG502: Pure Domain Functions"]
    end

    subgraph Bundles["Architectural Style Profiles"]
        B1["Functional Bundle"]
        B2["High-Performance Bundle"]
        B3["Pragmatic Bundle"]
    end

    DFG1xx --> Bundles
    DFG2xx --> Bundles
    DFG3xx --> Bundles
    DFG4xx --> Bundles
    DFG5xx --> Bundles
```

* **[DFG1xx: Test Structure Rules](dfg1xx-test-structure.md)**: Physical shape of unit and integration tests (markers, act statement budget, assertion roulette, failure messages).
* **[DFG2xx: Functional Architecture Rules](dfg2xx-functional-architecture.md)**: Clean code and functional discipline (lightweight property accessors, collection immutability, defensive exceptions, static lambdas, single assignment).
* **[DFG3xx: Contract & Data Purity Rules](dfg3xx-contract-and-data-purity.md)**: Segregation of pure data schemas from behavior, immutability, and boundary scoping.
* **[DFG4xx: Deep Immutability & Type Safety Rules](dfg4xx-deep-immutability.md)**: Deep immutability in records, mandatory init setters in domain models, and banned non-deterministic APIs (`DateTime.Now`, task blocking).
* **[DFG5xx: Algebraic Types & Exhaustive Matching](dfg5xx-algebraic-types.md)**: Full-time compile-time exhaustive pattern matching banning discard `_` on closed union types, and pure domain functions.
* **[Architectural Style Bundles](style-bundles.md)**: Configurable rule profiles balancing pure functional idioms with compiler hot-path performance requirements.

---

## 2. Public Finding Contract

Every Diffra Guardrails check emits findings conforming to a strict JSON structure:

```json
{
  "rule": "DFG201",
  "path": "src/OrderService.cs",
  "line": 14,
  "end_line": 14,
  "block": null,
  "severity": "error",
  "message": "Property getter 'Total' contains non-trivial behavior (LINQ query invocation); property getters must be lightweight, allocation-free accessors.",
  "fix": "Convert 'Total' to a method, or precompute and store the value in an init-only property or field."
}
```

### Invariants

1. **Rule IDs Never Change**: Rule identifiers are permanent contracts. A retired rule retains its number and is never recycled.
2. **Deterministic Fixes**: Every message is accompanied by an actionable fix instruction that an automated agent or developer can follow directly.
3. **Language Neutrality**: Rule semantics are identical across all language adapters (C#, F#, TypeScript, Python). Conformance is proven via `conformance/cases/`. A semantic rule (see [Scope and host class](#3-scope-and-host-class)) is checked only by hosts with type information.

---

## 3. Scope and Host Class

Every rule states two things beside its ID.

**Scope** is the code it runs on. `test` rules run only on recognised test files. `code` rules run
on production and test code alike.

**Host class** is what the rule needs to see. A `syntax` rule can be decided from one file's syntax
tree, so every host checks it: the CLI, the Roslyn analyzer, ESLint and the Python checker. Their
findings must agree, and the CLI-versus-analyzer parity test covers them. A `semantic` rule needs
type or symbol information (what a name resolves to, whether a type is closed or mutable, what a
lambda captures). Only hosts with a semantic model check it: the Roslyn analyzer for C#, and
type-aware ESLint for TypeScript. Semantic rules are excused from the parity test, and their
findings reach Diffra by ingesting the host's SARIF output, not from the CLI. A host without a
semantic model skips their corpus cases and reports them as not checked, never as passing.

| Rule | Scope | Host class | Why semantic |
| --- | --- | --- | --- |
| DFG101–DFG108 | test | syntax | |
| DFG109 (proposed) | test | syntax | |
| DFG201, DFG203, DFG205 | code | syntax | |
| DFG202 | code | semantic | `Add` or `Sort` mutates only when the receiver is a mutable collection type |
| DFG204 | code | semantic | Whether a lambda captures anything is a data-flow question |
| DFG301–DFG304 | code | syntax | |
| DFG401 | code | semantic | Whether a referenced type is mutable is known only from its declaration, often in another file |
| DFG402 | code | syntax | |
| DFG403 | code | semantic | `DateTime.Now` must resolve to `System.DateTime`, not a local type of the same name |
| DFG501 | code | semantic | A wildcard is wrong only when the matched type is a closed enum or hierarchy |
| DFG502 | code | syntax | |

The classification is a proposal for review. A rule moves from `semantic` to `syntax` only if a
corpus case shows a syntax-only adapter gets it right.

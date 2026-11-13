# DFG2xx: Functional Architecture & Clean Code Specification

Status: Specification & Design, November 2026. Conforms to [VISION.md](../../VISION.md).
Family: `DFG2xx` (Functional Architecture & Clean Code).

---

## 1. Overview & Motivation

The **DFG2xx** rule family enforces functional programming discipline and architectural cleanliness across all supported languages. While DFG1xx constrains the physical shape of tests, DFG2xx governs the structural design of implementation code and tests alike, focusing on five foundational pillars:

1. **Segregation of Data and Behavior:** Pure data representation versus computational transformation.
2. **Immutability & Value Semantics:** Eliminating in-place mutation and temporal coupling.
3. **Resilience & Fault Observability:** Prohibiting broad exception swallowing and lost diagnostic traces.
4. **Zero-Allocation Discipline:** Enforcing pure, non-capturing static closures to prevent heap bloat.
5. **Expression-Oriented Flow:** Eliminating mutable procedural state through single-assignment expressions.

Every DFG2xx rule adheres to strict invariants:

* **Language Neutrality:** The rule invariant is defined abstractly. Syntax-specific AST node targets are defined in language adapter specifications.
* **Deterministic Evaluation:** Analyzed via static syntax and data flow trees without heuristic AI models.
* **Stable Contracts:** Rule IDs, message strings, and actionable fix recommendations form an immutable public contract.
* **Diffra Metrics Ratchet:** Gated on new and changed code, tracked via Diffra metrics payloads.

---

## 2. Rule Catalogue

| ID | Name | Severity | Gate Policy | Failure Mode Prevented |
| :--- | :--- | :--- | :--- | :--- |
| **DFG201** | No Behavior in Property Getters | Error | New & changed files | Hidden $O(N)$ computations, collection allocations, and queries inside property accessors |
| **DFG202** | Immutable Collections & Transformations | Error | New & changed files | In-place collection mutations (`add`, `remove`, `clear`); temporal coupling and concurrency race conditions |
| **DFG203** | Defensive Exception Discipline | Error | New & changed files | Broad exception catching, swallowed faults, and lost stack traces |
| **DFG204** | Zero-Allocation Closures | Error | New & changed files | Redundant heap closure allocations when lambdas capture no outer state |
| **DFG205** | Single Assignment & Expression Flow | Error | New & changed files | Declaring mutable variables and procedurally reassigning them across conditional branches |

---

## 3. Universal Rule Specifications

### DFG201: No Behavior in Property Getters

* **Rule ID:** `DFG201`
* **Category:** `FunctionalArchitecture`
* **Severity:** `Error`
* **Invariant:** Property getters must be lightweight, $O(1)$, allocation-free accessors. Executing queries, instantiating new collections, performing I/O, or running procedural loops inside a getter obscures computational complexity and causes performance regressions in serializers, loggers, debuggers, and data binding.

#### Public Contract

* **Message:** `Property getter '{property}' contains non-trivial behavior ({violation}); property getters must be lightweight, allocation-free accessors.`
* **Fix:** `Convert '{property}' to a method, or precompute and store the value in an init-only property or field.`

#### Cross-Language Implementation Targets

* **C# / .NET:** Prohibits LINQ invocations, collection allocations, and loops in `get` accessors. (See [csharp/roslyn-analyzer.md](../csharp/roslyn-analyzer.md)).
* **TypeScript:** Prohibits array mutations, filtering methods, and loops inside class getter methods (`get prop()`). (See [typescript/tooling-and-plan.md](../typescript/tooling-and-plan.md)).
* **Python:** Prohibits list comprehensions, allocations, and loops inside `@property` decorated methods. (See [python/tooling-and-plan.md](../python/tooling-and-plan.md)).

---

### DFG202: Immutable Collections & Pure Transformations

* **Rule ID:** `DFG202`
* **Category:** `FunctionalArchitecture`
* **Severity:** `Error`
* **Invariant:** In-place collection mutation creates temporal coupling, invalidates caller assumptions, and introduces multithreaded race conditions. Collections must be treated as immutable snapshots transformed via pure functional operations or collection expressions.

#### Public Contract

* **Message:** `In-place mutation '{method}' detected on collection '{target}'; steer toward immutable collections and pure transformations.`
* **Fix:** `Replace '{method}' with a functional transformation, collection expression (e.g., '[.. {target}, item]'), or immutable collection.`

#### Exemptions

* Within private builders or local factory scopes that do not escape their containing function.
* Explicit inline suppression when required by low-level system buffers.

#### Cross-Language Implementation Targets

* **C# / .NET:** Flags `.Add()`, `.Remove()`, `.Clear()`, `.Sort()`. Recommends `[.. target, item]` collection expressions. (See [csharp/guidance.md](../csharp/guidance.md)).
* **TypeScript:** Flags `.push()`, `.splice()`, `.sort()`. Recommends `[...target, item]` or `toSorted()`. (See [typescript/tooling-and-plan.md](../typescript/tooling-and-plan.md)).
* **Python:** Flags `.append()`, `.extend()`, `.remove()`, `.clear()`. Recommends `[*target, item]` or tuple operations. (See [python/tooling-and-plan.md](../python/tooling-and-plan.md)).
* **F#:** Strictly enforced by default via native immutable lists and maps. (See [fsharp/tooling-and-plan.md](../fsharp/tooling-and-plan.md)).

---

### DFG203: Defensive Exception Discipline

* **Rule ID:** `DFG203`
* **Category:** `Reliability`
* **Severity:** `Error`
* **Invariant:** Catching general exceptions or swallowing errors masks fatal faults and destroys diagnostic observability. Catch blocks must specify exact exception types, filter conditions, or preserve stack traces via explicit rethrows.

#### Public Contract

* **Message:** `Catch block catches broad '{type}' without an exception filter or rethrow; catch-all exception swallowing is disallowed.`
* **Fix:** `Catch a specific exception type, add an exception filter ('when (...)'), or rethrow with 'throw;' to preserve diagnostic flow.`

#### Cross-Language Implementation Targets

* **C# / .NET:** Flags bare `catch` or `catch (Exception)` without `when (...)` or bare `throw;`. (See [csharp/roslyn-analyzer.md](../csharp/roslyn-analyzer.md)).
* **TypeScript:** Flags empty `catch (err) {}` or generic catches without rethrow/logging. (See [typescript/tooling-and-plan.md](../typescript/tooling-and-plan.md)).
* **Python:** Flags bare `except:` or `except Exception:` without `raise` or specific exception classes. (See [python/tooling-and-plan.md](../python/tooling-and-plan.md)).

---

### DFG204: Zero-Allocation Closures

* **Rule ID:** `DFG204`
* **Category:** `Performance`
* **Severity:** `Error`
* **Invariant:** Anonymous functions, lambdas, or local functions that do not capture outer lexical scope variables must be declared pure / static. Omitting static modifiers incurs avoidable heap allocations for delegate instances and closure classes.

#### Public Contract

* **Message:** `Lambda or local function does not capture outer variables and should be declared 'static' to guarantee zero closure allocation.`
* **Fix:** `Add the 'static' modifier to the lambda or local function.`

#### Cross-Language Implementation Targets

* **C# / .NET:** Enforces the `static` keyword on lambdas and local functions with empty capture sets. (See [csharp/guidance.md](../csharp/guidance.md)).
* **TypeScript:** Enforces module-level function extraction for pure arrow functions inside tight iteration loops. (See [typescript/tooling-and-plan.md](../typescript/tooling-and-plan.md)).
* **Python:** Enforces module-level `def` over allocating closures inside hot paths. (See [python/tooling-and-plan.md](../python/tooling-and-plan.md)).

---

### DFG205: Single Assignment & Expression-Oriented Flow

* **Rule ID:** `DFG205`
* **Category:** `FunctionalArchitecture`
* **Severity:** `Error`
* **Invariant:** Declaring an uninitialized or default-initialized mutable variable followed by conditional procedural reassignment is error-prone. Modern languages provide expression-oriented alternatives (pattern-matching expressions, ternary operators, or early returns).

#### Public Contract

* **Message:** `Variable '{variable}' is declared and subsequently reassigned across conditional branches; prefer single assignment via a switch expression, ternary expression, or early return.`
* **Fix:** `Initialize '{variable}' directly using a switch expression, ternary expression, or return early.`

#### Cross-Language Implementation Targets

* **C# / .NET:** Flags uninitialized `var x;` followed by `if`/`switch` assignments; steers toward `switch` expressions. (See [csharp/guidance.md](../csharp/guidance.md)).
* **TypeScript:** Flags `let x;` reassigned inside `if`/`else`; steers toward `const x = cond ? a : b` or pattern matching. (See [typescript/tooling-and-plan.md](../typescript/tooling-and-plan.md)).
* **Python:** Flags procedural default assignments reassigned in `if/else`; steers toward conditional expressions `a if cond else b`. (See [python/tooling-and-plan.md](../python/tooling-and-plan.md)).

---

## 4. Diffra Metrics Payload Integration

Every rule emission across any language adapter integrates into Diffra's universal ratchet payload:

```json
{
  "scope": "project/src",
  "methodology": "functional-architecture-v1",
  "metrics": {
    "files": {
      "src/domain/basket.ext": {
        "DFG201": { "value": 1, "unit": "violations" },
        "DFG202": { "value": 2, "unit": "violations" },
        "DFG204": { "value": 3, "unit": "violations" }
      }
    }
  }
}
```

Any absolute increase in violations within new or changed files fails the pull request gate. Untouched files are tracked for baseline scoring.

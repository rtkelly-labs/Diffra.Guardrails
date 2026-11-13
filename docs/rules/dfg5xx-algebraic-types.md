# DFG5xx: Algebraic Types & Exhaustive Matching Specification

Status: Specification & Design, November 2026. Conforms to [VISION.md](../../VISION.md).
Family: `DFG5xx` (Algebraic Data Types & Pure Domain Functions).

---

## 1. Overview & Motivation

Discriminated unions, algebraic data types (ADTs), and closed sum types represent finite domain states (e.g., `Result<T, E> = Ok(T) | Err(E)` or `PaymentMethod = Card | Cash | Crypto`).

The primary safety benefit of algebraic types is **compile-time exhaustiveness**: if a new domain variant is added, every pattern match across the codebase immediately triggers a compiler diagnostic until handled.

However, developers frequently subvert this safety invariant by writing wildcard fallback arms (`_ => ...` in C#/F#, `case _:` in Python, `default:` in TypeScript). When a new domain variant is introduced, these wildcard fallbacks silently absorb the new case, leading to subtle logic corruption or silent failure.

The **DFG5xx** rule family enforces **full-time compile-time exhaustiveness** on closed union types and requires **pure domain functions** for all state transformations.

---

## 2. Rule Catalogue

| ID | Name | Severity | Gate Policy | Failure Mode Prevented |
| :--- | :--- | :--- | :--- | :--- |
| **DFG501** | Full-Time Exhaustive Pattern Matching | Error | Gated | Wildcard discard `_` masking unhandled domain variants in closed unions |
| **DFG502** | Pure Domain Functions | Error | Gated | In-place mutation of domain entity fields inside instance methods |

---

## 3. Detailed Specifications

### DFG501: Full-Time Exhaustive Pattern Matching

* **Rule ID:** `DFG501`
* **Category:** `AlgebraicTypes`
* **Severity:** `Error`
* **Invariant:** Pattern matching expressions over closed union types (enums, discriminated union hierarchies, sealed sum types) must explicitly enumerate every variant. Wildcard discards (`_ => ...` or fallback cases) are prohibited on closed algebraic domains.
* **Message:** `Switch expression over closed union type '{type}' uses a wildcard discard '_' which masks unhandled variants; pattern matching must explicitly handle all cases.`
* **Fix:** `Remove the wildcard discard and explicitly match all variants of '{type}'.`

#### Ecosystem Realization

* **C# / .NET (.NET 10 / .NET 11 RTM):** In .NET 11 (C# 14), native union types and closed hierarchies can be conditionally compiled using the standard SDK constant `#if NET11_0_OR_GREATER`. While the compiler emits warning CS8509 on incomplete switches, developers routinely silence it using wildcard discards (`_ => ...`). DFG501 elevates exhaustiveness to a full-time, unbypassable compile-time invariant by prohibiting `_` on closed algebraic union and enum hierarchies.
* **TypeScript:** Using `ts-pattern` (`.exhaustive()`) or TypeScript `switch` with `assertNever(x)` on `default:` to enforce that every union tag is handled.
* **Python:** Python 3.10+ `match` statements over `Enum` or `typing.Union` must exhaustively list all branches without a trailing `case _:`.
* **F#:** F# compiler warning FS0025 on incomplete pattern matches must not be bypassed with `| _ -> ...`.

---

### DFG502: Pure Domain Functions

* **Rule ID:** `DFG502`
* **Category:** `FunctionalArchitecture`
* **Severity:** `Error`
* **Invariant:** Domain business operations must be pure static functions of the shape $\text{State} \times \text{Command} \to \text{State} \times \text{Events}$. Instance methods that mutate receiver state or fields are prohibited.
* **Message:** `Domain method '{method}' in '{type}' is non-static or writes to state; domain operations must be pure static functions.`
* **Fix:** `Make method static and return a new state instead of mutating fields.`

#### Ecosystem Realization

* **C# / .NET:** Domain methods must be `static` functions returning new record instances.
* **TypeScript:** Domain functions are exported pure functions `(state: State, cmd: Command): State` rather than class methods mutating `this`.
* **Python:** Domain functions are top-level pure functions rather than methods mutating `self`.

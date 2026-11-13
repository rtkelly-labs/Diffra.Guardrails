# DFG4xx: Deep Immutability & Type Safety Specification

Status: Specification & Design, November 2026. Conforms to [VISION.md](../../VISION.md).
Family: `DFG4xx` (Deep Immutability & Type Safety).

---

## 1. Overview & Motivation

Shallow immutability offers a false sense of safety. A data container whose outer properties are read-only (`readonly`, `init`, or `frozen`) remains dangerously mutable if its fields hold references to mutable objects or mutable collections. In multithreaded systems, distributed messaging pipelines, and functional event stores, non-thread-safe reference types introduce data races, temporal coupling, and lost update bugs.

Furthermore, non-deterministic system dependencies (such as directly querying wall-clock system time) and synchronous blocking calls introduce unpredictable test flakiness and deadlock hazards.

The **DFG4xx** rule family enforces **deep immutability** and **deterministic type safety** across all application layers.

---

## 2. Rule Catalogue

| ID | Name | Severity | Gate Policy | Failure Mode Prevented |
| :--- | :--- | :--- | :--- | :--- |
| **DFG401** | Deep Immutability in Value Types & Records | Error | Gated | Shallow immutability leaking mutable reference objects or collections |
| **DFG402** | Mandatory Init / Read-Only Domain Properties | Error | Gated | Mutable property setters (`set;`) enabling in-place state corruption |
| **DFG403** | Banned Non-Deterministic & Blocking APIs | Error | Gated | Direct system clock queries (`DateTime.Now`), thread sleeping, and task blocking |

---

## 3. Detailed Specifications

### DFG401: Deep Immutability in Value Types & Records

* **Rule ID:** `DFG401`
* **Category:** `DeepImmutability`
* **Severity:** `Error`
* **Invariant:** Records, value objects, and message contracts must be deeply immutable. A record's properties and constructor arguments must reference exclusively primitive value types, deeply immutable records, or read-only/immutable collections.
* **Message:** `Record '{record}' has property '{property}' of mutable reference type '{type}'; records must be deeply immutable.`
* **Fix:** `Make type an immutable record or value type, or use an immutable collection.`

#### Ecosystem Realization

* **C# / .NET:** Records must not contain mutable class references or mutable collections (`List<T>`, `Dictionary<K, V>`). Use `IReadOnlyList<T>`, `IReadOnlyDictionary<K, V>`, or `ImmutableArray<T>`.
* **TypeScript:** Read-only interfaces and type aliases must use `ReadonlyArray<T>`, `ReadonlyMap<K, V>`, and recursive `Readonly<T>` wrappers.
* **Python:** `@dataclass(frozen=True)` classes must not hold mutable `list` or `dict` fields; use `tuple`, `frozenset`, or `MappingProxyType`.

---

### DFG402: Mandatory Init / Read-Only Domain Properties

* **Rule ID:** `DFG402`
* **Category:** `DeepImmutability`
* **Severity:** `Error`
* **Invariant:** Domain entities, aggregates, and data transfers must declare properties that are assignable only at construction time. Mutable `set;` accessors are prohibited; properties must use `init;` or get-only accessors. State transitions must produce new object instances via non-destructive mutation.
* **Message:** `Property '{property}' in domain type '{type}' declares a mutable setter 'set;'; domain properties must be immutable ('init;' or get-only).`
* **Fix:** `Replace 'set;' with 'init;' or remove setter.`

#### Ecosystem Realization

* **C# / .NET:** Replace `public string Name { get; set; }` with `public string Name { get; init; }`. Non-destructive mutation is achieved via `entity with { Name = "New" }`.
* **TypeScript:** Declare all properties with `readonly prop: Type;`. Updates produce fresh copies: `{ ...entity, prop: "New" }`.
* **Python:** Use `frozen=True` dataclasses or pydantic models with `frozen=True`. Updates use `dataclasses.replace(entity, prop="New")`.

---

### DFG403: Banned Non-Deterministic & Blocking APIs

* **Rule ID:** `DFG403`
* **Category:** `TypeSafety`
* **Severity:** `Error`
* **Invariant:** Direct reads of the host system clock, synchronous thread sleeping, synchronous task blocking, and legacy untyped collections are strictly prohibited in domain and pipeline logic.
* **Message:** `Use of banned symbol '{symbol}': {reason}.`
* **Fix:** `Use '{alternative}' instead.`

#### Banned Symbol Matrix

| Symbol / Pattern | Reason | Required Alternative |
| :--- | :--- | :--- |
| `DateTime.Now`, `DateTime.UtcNow` | Non-deterministic system clock read | Injectable `TimeProvider.GetUtcNow()` |
| `Thread.Sleep(...)` | Synchronous thread blocking | Asynchronous `Task.Delay(...)` |
| `Task.Result`, `Task.Wait()` | Synchronous blocking risks thread-pool starvation & deadlocks | Asynchronous `await` |
| `ArrayList`, `Hashtable` | Legacy untyped non-generic collection | `ImmutableArray<T>` or `IReadOnlyList<T>` |
| Python `datetime.datetime.now()` | Non-deterministic system clock dependency | Injected clock provider / `time_machine` |
| TypeScript `Date.now()`, `new Date()` | Non-deterministic system clock dependency | Injected clock service |

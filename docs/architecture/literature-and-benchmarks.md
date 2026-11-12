# Literature, Benchmarks, and Durable Technical Rationale

Status: Reference & Rationale Guide, November 2026.
Scope: Rules DFG101–DFG108 and DFG201–DFG205.

---

## 1. Executive Summary

Diffra Guardrails rules are not arbitrary stylistic preferences. Every rule is grounded in:

1. **Academic empirical software engineering research** (defect correlation, mutation testing, test smell decay).
2. **Authoritative language design guidance** (Microsoft Framework Design Guidelines, C# language specifications).
3. **Low-level runtime performance engineering** (Stephen Toub's .NET performance architectures, garbage collection pressure, closure heap allocations).

This document compiles the formal bibliography, durable technical links, and empirical rationale
grounding every rule.

---

## 2. Rule Rationale & Grounding Matrix

### 2.1 DFG201: No Behavior in Property Getters

* **The Problem:** Executing LINQ queries, allocating collections, or executing loops in property
  getters misleads callers, debuggers, serializers, and ORMs into treating expensive $O(N)$
  operations as $O(1)$ field reads.
* **Authoritative Rationale:**
  * **Microsoft Framework Design Guidelines (Property Design):**
    *Durable Link:* <https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/property>
    *"If the accessor requires computational work, calls remote services, or allocates a new array,
    it should be a method, not a property. A property getter must be an O(1) operation without
    noticeable side-effects."*
  * **Eric Lippert (Former C# Compiler Principal Developer):**
    *Durable Link:* <https://ericlippert.com/category/properties-vs-methods/>
    *Analysis:* Properties represent state; methods represent actions. Returning newly allocated
    objects or running LINQ queries from getters breaks referential expectations ($a.Prop == a.Prop$)
    and creates hidden memory cliffs.
  * **Stephen Toub (Partner Software Engineer, .NET Performance):**
    *Durable Link:* <https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-8/>
    *Analysis:* Demonstrates how hidden allocations in property getters compound inside serialization
    and logging pipelines, driving GC latency spikes.

---

### 2.2 DFG202: Immutable Collections & Pure Transformations

* **The Problem:** In-place mutations (`.Add()`, `.Remove()`, `.Clear()`, `.Sort()`) introduce
  temporal coupling, shared mutable state, race conditions in multithreaded systems, and invalidate
  aggregate encapsulation.
* **Authoritative Rationale:**
  * **Stephen Toub (Behind the Scenes of Collection Expressions):**
    *Durable Link:* <https://devblogs.microsoft.com/dotnet/behind-the-scenes-collection-expressions-in-csharp-12/>
    *Analysis:* Explains how modern C# collection expressions (`[.. a, .. b]`) leverage compiler
    knowledge to pre-size buffers, avoid intermediate reallocations, and supersede procedural
    `List<T>.Add` loops.
  * **Rich Hickey (The Value of Values):**
    *Durable Link:* <https://www.infoq.com/presentations/Value-Values/>
    *Core Concept:* Mutable objects conflate identity with state. In-place mutation destroys the
    reproducibility of data over time. Pure transformations over immutable data structures eliminate
    temporal coupling.
  * **Microsoft Learn (System.Collections.Immutable Architecture):**
    *Durable Link:* <https://learn.microsoft.com/en-us/dotnet/api/system.collections.immutable>
    *Guidance:* Immutable data structures guarantee thread safety by design, eliminating locks and
    defensive copies across boundary layers.
  * **TC39 Record & Tuple Proposal & Redux Principles (JavaScript / TypeScript):**
    *Durable Link:* <https://github.com/tc39/proposal-record-tuple>
    *Analysis:* Immutability in modern JavaScript eliminates accidental side-effects, optimizes change
    detection, and prevents state corruption in asynchronous UI and service architectures.
  * **PEP 557 & Python Frozen Data Structures:**
    *Durable Link:* <https://peps.python.org/pep-0557/>
    *Standard:* Python's `frozen=True` dataclass design prevents mutation of shared references across
    application pipelines.

---

### 2.3 DFG203: Defensive Exception Discipline

* **The Problem:** Broad `catch (Exception)` blocks without filters or rethrow swallow critical
  faults (`OutOfMemoryException`, `ArgumentException`), corrupt domain state, and truncate stack
  traces.
* **Authoritative Rationale:**
  * **Krzysztof Cwalina & Brad Abrams (Framework Design Guidelines: Exceptions):**
    *Durable Link:* <https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/exceptions>
    *Rule:* Section 7.2: *"Do not swallow errors by catching general exception types such as
    System.Exception, System.SystemException, or System.Object without rethrowing."*
  * **Martin Fowler (Fail Fast Principle):**
    *Durable Link:* <https://martinfowler.com/ieeeSoftware/failFast.pdf>
    *Core Concept:* Software must fail immediately and visibly upon discovering an invalid state.
    Swallowing an exception allows a program to limp forward with corrupted state, converting an
    obvious local defect into an intractable bug downstream.
  * **PEP 8 Style Guide for Python Code (Programming Recommendations):**
    *Durable Link:* <https://peps.python.org/pep-0008/#programming-recommendations>
    *Rule:* *"When catching exceptions, mention specific exceptions whenever possible instead of
    using a bare 'except:' clause... A bare 'except:' clause will catch SystemExit and
    KeyboardInterrupt exceptions, making it harder to interrupt a program."*
  * **Sergey Teplyakov (ErrorProne.NET: Exception Antipatterns):**
    *Durable Link:* <https://github.com/SergeyTeplyakov/ErrorProne.NET>
    *Analysis:* Catching general exception types hides assertion failures in tests and prevents
    diagnostic telemetry from capturing the true root cause. Bare `throw;` must be enforced to
    preserve original stack traces.

---

### 2.4 DFG204: Zero-Allocation Closures

* **The Problem:** Lambdas and local functions that do not capture outer variables still incur heap
  delegate allocations if not marked `static`.
* **Authoritative Rationale:**
  * **Mads Torgersen (C# Language Design: Static Anonymous Functions):**
    *Durable Link:* <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/static-anonymous-functions>
    *Mechanism:* An unmarked lambda requires the compiler to emit a closure instance or delegate
    allocation unless proven pure. The `static` modifier guarantees at compile-time that zero state
    is captured and delegate instances are safely cached as singletons.
  * **Konrad Kokosa (Pro .NET Memory Management):**
    *Reference:* Chapter 5 & Chapter 8: Delegate Internals & GC Allocation Budgets.
    *Finding:* In high-throughput pipelines, non-static lambdas inside LINQ queries or collections
    constitute a top source of gen-0 GC churn.
  * **G&eacute;rald Barr&eacute; (Meziantou Performance Insights):**
    *Durable Link:* <https://www.meziantou.net/performance-boost-using-static-lambdas.htm>
    *Empirical Benchmark:* Benchmarks showing up to $100\%$ reduction in allocation overhead by
    converting hot-path delegates to `static`.

---

### 2.5 DFG205: Single Assignment & Expression-Oriented Flow

* **The Problem:** Declaring an uninitialized or default mutable variable and mutating it inside
  conditional blocks (`if`/`else` or procedural `switch`) allows partially initialized states and
  obscures data flow.
* **Authoritative Rationale:**
  * **Mads Torgersen & Bill Wagner (Do more with patterns in C#):**
    *Durable Link:* <https://devblogs.microsoft.com/dotnet/do-more-with-patterns-in-c-8-0/>
    *Design Principle:* Switch expressions and pattern matching transform procedural branching into
    exhaustive expressions, enforcing that every path produces a value and eliminating intermediate
    state.
  * **Scott Wlaschin (Railway Oriented Programming):**
    *Durable Link:* <https://fsharpforfunandprofit.com/rop/>
    *Functional Concept:* Expression orientation guarantees referential transparency. Binding values
    at declaration prevents temporal mutation bugs.

---

### 2.6 DFG1xx: Test Structure & Smell Research

* **Assertion Roulette (DFG105 / DFG106):**
  * *van Deursen, Moonen, Bergh, & Kok (2001):* *Refactoring Test Code.* Defined Assertion Roulette:
    when multiple assertions in one test fail without descriptive messages, identifying the defect
    requires a debugger.
  * *Zhang & Mesbah (FSE 2015):* Found assertion count and clarity directly correlate with test suite
    effectiveness.
* **Conditional Test Logic (DFG108):**
  * *Meszaros (2007):* *xUnit Test Patterns.* Conditional statements (`if`, `for`, `try`) in tests
    introduce cyclomatic complexity into the test itself, causing tests to silently pass without
    executing assertions.
* **Test Smells & Maintenance Costs:**
  * *Wang et al. (ASE 2021, PyNose):* Empirical study proving test smells correlate directly with
    maintenance cost and flakiness rather than fault detection.
  * *Trautsch & Grabowski (ICST 2017 / JSS 2020):* Unit tests catch defects with higher fault
    localization precision than integration tests, justifying the tight single-action Act budget.

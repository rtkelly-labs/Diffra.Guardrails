# Conformance corpus

Each case is one test, written once in every supported language, plus the findings every adapter
must report for it. A language is supported when its adapter reports exactly the expected findings
for every case. A rule changes here first, then in each adapter.

Status: skeleton. There is no checker in this repository yet. The cases are data only.

## Layout

```text
conformance/
  expected.schema.json        JSON Schema for expected.json
  cases/
    001-marked-small-test/
      test_case.py            Python (pytest)
      CaseTests.cs            C# (xUnit, `Should()` assertions)
      case.test.ts            TypeScript (Vitest)
      expected.json           Findings, with the line in each language
```

- **One test per file.** A case is identified by its folder, so no cross-language test naming
  scheme is needed. `expected.json` still records each language's test name for messages.
- **The code under test is imaginary.** `pricing`, `Pricing` and `./pricing` do not exist. The files
  are parsed by the adapters, never compiled or run, and no build in this repository picks them up.
- **Findings are a set**, compared on `rule`, `block` and the language's `line`. Order does not
  matter. `fix` is the text an agent should be able to act on; adapters may word it differently,
  but must name the same change.
- **Size** is the size the test is checked at. Cases declare none in the code, so they get the
  small budget; a medium or large case will declare its size the way the adapter reads it.

## Assertion APIs used

The rules do not depend on an assertion library. Each case uses a form that has a message slot in
its language, so the expected findings are the same everywhere: `Should().Be(expected, because)` in
C#, Vitest's `expect(value, message)` in TypeScript, and a bare `assert` with a message in Python.
An API with no message slot, such as most of xUnit's `Assert`, gets a DFG106 warning instead of a
finding; see [the adapter contract](../docs/adapters.md#message-slots). That needs a language-specific
case, which is planned.

## Rule precedence in the cases

These are the conventions the expected findings follow. The first checker settled them, and every
adapter must apply them the same way.

1. When DFG101 fires (no markers), DFG102, DFG103 and DFG104 are not reported for that test. They
   only measure marked tests.
2. DFG105 treats an unmarked body as one block, and a marked test per Then block. Its finding is
   on the first assertion past the first one.
3. DFG106 reports every assertion without a message: an error where the API has a message slot,
   a warning where it has none. DFG107 only scores messages that exist.
4. DFG108 reports the line of each `if`, loop or `try` statement in the test body.

## Cases

| Case | Shows | Expected |
| --- | --- | --- |
| 001-marked-small-test | The target shape: markers, one Act statement, one assertion with a message | none |
| 002-two-assertions-without-messages | Assertion Roulette: two checks in one Assert, no messages | DFG105, DFG106 ×2 |
| 003-unmarked-test-with-a-loop | No markers; a loop where a data-driven test belongs | DFG101, DFG108 |
| 004-property-getter-with-linq | Property getter with LINQ query and collection allocation | DFG201 |
| 005-collection-add-mutation | In-place collection mutation instead of functional transformation | DFG202 |
| 006-broad-exception-swallowed | Catch-all exception swallowing without filter or rethrow | DFG203 |
| 007-non-static-pure-lambda | Non-static lambda capturing no outer variables | DFG204 |
| 008-mutable-variable-if-reassignment | Mutable variable reassigned across conditional branches | DFG205 |
| 009-property-getter-with-control-flow | Property getter with loop and multi-statement computation | DFG201 |
| 010-collection-remove-mutation | In-place collection mutation with Remove | DFG202 |
| 011-clean-functional-pipeline | Pure functional test obeying all structural and functional rules | none |
| 012-capturing-lambda-allowed | Lambda capturing outer threshold (no false positive) | none |
| 013-filtered-exception-allowed | Catch with explicit exception filter (no false positive) | none |
| 014-property-getter-allocation | Property getter allocating a new array on every access | DFG201 |
| 015-property-getter-switch-statement | Property getter executing procedural switch statement | DFG201 |
| 016-clean-computed-property | Clean computed property with simple arithmetic (no false positive) | none |
| 017-collection-clear-mutation | In-place collection mutation with Clear | DFG202 |
| 018-clean-collection-expression | Pure collection expression combining arrays (no false positive) | none |
| 019-bare-catch-all | Bare catch-all block swallowing faults | DFG203 |
| 020-clean-rethrow-exception | Catch with bare throw rethrow (no false positive) | none |
| 021-non-static-local-function | Non-static local function capturing no state | DFG204 |
| 022-clean-static-lambda | Static lambda guaranteeing zero closure allocation (no false positive) | none |
| 023-mutable-variable-switch-reassignment | Mutable variable reassigned across switch statement | DFG205 |
| 024-clean-switch-expression | Single assignment via switch expression (no false positive) | none |
| 025-misspelt-marker-order | Marker misspelt as Arange | DFG102 |
| 026-act-budget-exceeded | Act block in small test contains two statements | DFG103 |
| 027-assertion-in-act-block | Assertion placed inside Act block | DFG104 |
| 028-property-getter-new-collection | Property getter instantiating new List | DFG201 |
| 029-collection-sort-mutation | In-place collection mutation via Sort | DFG202 |
| 030-scoped-builder-allowed | Scoped builder producing immutable collection (no false positive) | none |
| 031-rethrow-losing-stack-trace | Rethrow using explicit identifier throw ex | DFG203 |
| 032-lambda-capturing-this-allowed | Lambda capturing instance state this (no false positive) | none |
| 033-clean-early-return | Early return flow avoiding mutable reassignment (no false positive) | none |
| 034-contract-with-method | Contract type declares method instead of domain function | DFG301 |
| 035-contract-property-with-getter-body | Contract property declares computed getter body | DFG302 |
| 036-contract-mutable-setter | Contract property declares mutable setter `set;` | DFG303 |
| 037-contract-mutable-collection | Contract property declares mutable `List<T>` collection | DFG303 |
| 038-contract-disallowed-class | Contract boundary declares mutable class instead of record | DFG304 |
| 039-clean-pure-contract | Pure immutable contract record with read-only collection (no false positive) | none |
| 040-record-deep-immutability-violation | Record property references a mutable reference class type | DFG401 |
| 041-domain-entity-mutable-setter | Domain entity declares mutable setter `set;` instead of `init;` | DFG402 |
| 042-banned-datetime-now | Domain logic accesses `DateTime.UtcNow` instead of `TimeProvider` | DFG403 |
| 043-union-switch-wildcard-discard | Switch expression over union/enum uses wildcard discard `_` | DFG501 |
| 044-domain-method-mutates-state | Domain instance method mutates state instead of acting as pure function | DFG502 |
| 045-clean-exhaustive-union-match | Exhaustive match over union with zero discards and pure functions (no false positive) | none |

## Adding a case

1. Pick the next number and a name that says what the test shows.
2. Write the test in every language, keeping the same statement order so the shapes match.
3. Write `expected.json`. Take line numbers from the files (for example with `grep -n`), not by eye.
4. CI checks that the JSON parses. The adapters' own CI checks the findings.

Planned next cases: Act & Assert for an expected exception; a shared-context class whose fixture
carries Arrange and Act; a medium test with two When/Then pairs; a marker misspelt (DFG102); an
assertion in the Act block (DFG104); an inline exemption; an assertion API with no message slot
(DFG106 warning).

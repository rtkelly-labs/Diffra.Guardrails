# Adapter contract

An adapter brings one language under the DFA rules. It reads test files, never runs them, and emits
findings in one shared format. This page defines what an adapter must answer, what it must emit, and
which existing linter rules it reuses instead of rebuilding.

Status: proposal. No adapter lives in this repository yet.

## The six questions

Every adapter answers the same questions. The rules are written against the answers, never against
a language's syntax.

| Question | Python (pytest) | C# (xUnit, NUnit, MSTest) | TypeScript (Vitest, Jest) | F# (Expecto, xUnit) |
| --- | --- | --- | --- | --- |
| What is a test? | `test_*` functions, and `test_*` methods of `Test*` classes, in files pytest collects | Methods with `[Fact]`, `[Theory]`, `[Test]`, `[TestCase]`, `[TestMethod]` or `[DataTestMethod]` | Calls to `test`, `it` and their `.each`, `.only`, `.skip` forms | `testCase`/`test` in Expecto lists; `[<Fact>]` methods |
| What is an assertion? | `assert` statements, `self.assert*` calls, `pytest.raises`/`warns` blocks, registered helpers | `Assert.*`, `Should()` chains, `Verify(...)`, registered helpers | `expect(...)` chains, `expect.soft`, `assert.*`, registered helpers | `Expect.*`, Unquote `test <@ ... @>`, registered helpers |
| Where is the failure message? | `assert x, "msg"`; `msg=` on `unittest` asserts | See [message slots](#message-slots) | `expect(x, "msg")` in Vitest | Expecto's last argument |
| How are comments reached? | `tokenize` over the source, matched to `ast` line spans | Roslyn leading and trailing trivia | ESLint `sourceCode.getCommentsBefore` / `getAllComments` | FCS untyped tree plus the source text's comment ranges |
| How is size declared? | `@pytest.mark.small`, `medium`, `large` (registered marks) | `[Trait("Size", "Medium")]`, NUnit `[Category("Medium")]` | A `describe` title tag (`[medium]`) or a `*.medium.test.ts` file name | Expecto `testList` naming; xUnit traits |
| How is shared context expressed? | Fixtures (by name lookup) and `setup_method` | Constructor, `IClassFixture<T>`, `[SetUp]`, `[TestInitialize]` | `beforeEach` / `beforeAll` in the enclosing `describe` | Fixture functions passed to the test |

**Registered helpers** are custom assertion functions named in the repository's configuration (or,
in Python, matched by an `assert_*` name). One call to a registered helper counts as one assertion
for DFA105, which is how a test checks a compound value without breaking the one-assertion rule.

### Markers

The marker grammar is the same everywhere: a line comment whose whole text is one of `Arrange`,
`Act`, `Assert`, `Act & Assert`, `Given`, `When`, `Then`, in the language's line-comment syntax
(`#` or `//`). A comment with more words after a marker (`# Given a repo`) is description, not a
marker. Typo matching for DFA102 applies only to single words of four or more letters, so ordinary
comments are not flagged.

### Message slots

DFA106 applies only where the assertion API has a place for a message. Where it has none, the
adapter reports the assertion as `not-applicable` (counted, never a finding), and the repository
decides whether to move to an API that has one.

| API | Message slot | DFA106 |
| --- | --- | --- |
| Python `assert` | `assert x, "msg"` | applies |
| Python `unittest` asserts | `msg=` | applies |
| `pytest.raises` / `pytest.warns` | none (`match=` checks the error, it is not a message) | not applicable; Act & Assert form |
| xUnit `Assert.*` | only `Assert.True` / `Assert.False` take a message | not applicable except those two |
| NUnit `Assert.That` | `message` argument | applies |
| MSTest `Assert.*` | `message` argument | applies |
| FluentAssertions / AwesomeAssertions | `because` argument | applies |
| Shouldly | `customMessage` argument | applies |
| Vitest `expect` | `expect(value, message)` | applies |
| Jest `expect` | none without the `jest-expect-message` package | not applicable unless that package is present |
| Expecto `Expect.*` | the last argument, which is required | always satisfied; DFA107 still scores it |
| Unquote `test <@ ... @>` | none; the failure prints the reduced expression | not applicable |

FluentAssertions moved to a commercial licence from version 8 (January 2025); AwesomeAssertions is
the Apache-licensed community fork with the same API. The C# adapter recognises the `Should()` API
shape, not a package name.

### Exemptions

Each adapter uses the language's own suppression mechanism, so editors understand it, and requires
a reason. Every exemption is counted per rule as evidence.

| Language | Form |
| --- | --- |
| Python | `# dfa: allow DFA105 -- one value, two views of it` on the line, or the line above |
| C# | `#pragma warning disable DFA105 // reason` or `[SuppressMessage("Diffra.Guardrails", "DFA105", Justification = "...")]`; counted from the SARIF log's suppression records |
| TypeScript | `// eslint-disable-next-line diffra/dfa105 -- reason`; counted from ESLint's `suppressedMessages` |
| F# | The FSharp.Analyzers.SDK suppression comment, with a reason |

An exemption without a reason is an error from the adapter, never a counted exemption. (If it needs
its own rule ID later, it gets a new number in the 1xx range.) File-wide or project-wide suppression of a gated rule is rejected by
the CI action.

## What an adapter emits

### Findings

One JSON object per finding, the same in every language. The CLI wrapper
([interventions](interventions.md#4-wrappers)) reads these and renders them as text for an agent,
GitHub annotations or a job summary.

```json
{
  "rule": "DFA105",
  "path": "tests/test_basket.py",
  "line": 13,
  "endLine": 13,
  "test": "test_a_discount_code_changes_the_total_and_the_label",
  "block": "Assert",
  "scope": "new",
  "gated": true,
  "message": "This Assert block has 2 assertions; a Then checks one thing.",
  "fix": "Split this test in two, one per behaviour, or compare both values in one assertion."
}
```

- `scope` is `new`, `changed` or `untouched`, relative to the merge-base. `gated` says whether this
  finding fails the changed-test gate. Untouched tests produce findings with `gated: false`; they feed
  the score only.
- `message` and `fix` are stable text per rule, with only counts and names substituted, so agent
  instructions can quote them.
- The Roslyn and ESLint adapters already produce diagnostics in their own formats. They map to this
  shape in the wrapper, not inside the analyzer, so the analyzers stay ordinary IDE citizens.

### The Diffra metrics payload

Each adapter (or the wrapper on its behalf) writes Diffra's published metrics payload,
`{scope, methodology, metrics}`. It never depends on Diffra's internals. Because Diffra applies one
set of thresholds to every measurement in a payload, metrics with different budgets go in different
payloads:

| Payload | `methodology` | Metrics | Evaluated |
| --- | --- | --- | --- |
| Structure | `test-structure-v1` | Per file, per rule: DFA101–DFA106 and DFA108 counts | Yes, fail on any absolute increase |
| Score | `test-structure-score-v1` | Per file: DFA107 count, exemptions per rule | Yes, warn on any absolute increase |
| Totals | `test-structure-totals-v1` | Tests, assertions, assertions with a message, by size | No; reported beside the Delta only |

```json
{
  "scope": "example/project",
  "methodology": "test-structure-v1",
  "metrics": {
    "files": {
      "tests/Pricing.Tests/BasketTests.cs": {
        "DFA101": { "value": 12, "unit": "violations" },
        "DFA106": { "value": 30, "unit": "violations" }
      }
    }
  }
}
```

Rules for the payload:

- **Counts only, lower is better.** Diffra budgets increases. A ratio such as message coverage rises
  as things improve, so it is never budgeted; "assertions without a message" is.
- **Sparse by file.** A file appears only when it has a non-zero count. A new, clean test file then
  adds nothing, so it does not trigger `needs-review`. The cost: a file cleaned to zero appears as
  `removed`, which Diffra treats as `needs-review`. Cleaning a file is rarer than adding one, so
  sparse is the better default until Diffra can treat a missing measurement as zero for a
  methodology (an open question for Diffra).
- **Paths are keys.** File paths are object keys under `files`. Diffra addresses them by JSON
  Pointer, which escapes `/` as `~1`; the producer writes the plain path.
- **Methodology changes are breaking.** Changing what a rule counts bumps the methodology version,
  which Diffra reports as `incompatible` rather than as a change.

## Reuse or build

Reuse an existing rule wherever it fully covers a DFA rule, enable complementary rules that check
nearby problems, and build only the gap. Anything that needs the marker block model (which line is
in which block) is a build in every language, because no existing tool knows about markers beyond
"is there a comment".

| DFA rule | Python (Ruff) | C# (Roslyn) | TypeScript (ESLint) |
| --- | --- | --- | --- |
| DFA101 markers present | Build | Build. StrictCSharp.Analyzers checks AAA comments, but not Given/When/Then or blocks; do not run both | Build |
| DFA102 marker spelling and order | Build | Build, with a code fix | Build, with an autofix |
| DFA103 Act budget by size | Build | Build | Build |
| DFA104 assertion outside Assert | Build. Enable PT017 (assert in `except`) alongside | Build | Build. Enable `vitest/no-standalone-expect` and `vitest/no-conditional-expect` alongside |
| DFA105 one assertion per Then | Build. Enable PT018 (split composite `assert a and b`) alongside | Build | Build. `vitest/max-expects` with `max: 1` is equivalent only for one-Then tests and ignores helpers; use it as a stopgap before the plugin exists |
| DFA106 message present | Build (no Ruff rule exists) | Build | Build (no Vitest rule exists; `vitest/require-to-throw-message` is about thrown errors, not assertion messages) |
| DFA107 message quality | Build | Build | Build |
| DFA108 no control flow | Build | Build | Partial reuse. `vitest/no-conditional-in-test` flags `if` statements only. Until the plugin exists, core `no-restricted-syntax` scoped to test files can ban loops, `try` and `switch` |
| Act & Assert form | Reuse PT012 (one simple statement in a `raises` block) and PT011 (`raises` too broad) | Build | Build |
| Test has an assertion at all | Build (falls out of DFA105 counting) | Reuse SonarAnalyzer S2699 where Sonar runs | Reuse `vitest/expect-expect` |

Notes:

- **Ruff has no plugin system**, so the Python checker is its own program. It uses only the
  standard library (`ast` and `tokenize`), so it adds no dependency to the repositories it checks.
- **Roslyn and ESLint diagnostics come with editor support for free.** That is the main reason C#
  and TypeScript get analyzer and plugin hosts rather than standalone programs.
- **F# needs FSharp.Analyzers.SDK.** Roslyn does not see F# syntax. The F# adapter comes last and
  starts with DFA101, DFA104 and DFA108.
- **Complementary rules are configured, not wrapped.** The repository's own Ruff, analyzer or ESLint
  configuration enables them. Guardrails ships the recommended settings as a template and does not
  re-report their findings.

## Supporting a language

A language is supported when:

1. its adapter reports exactly the expected findings for every case in the
   [conformance corpus](../conformance/README.md);
2. it emits the finding JSON and the three payloads above;
3. its exemption form is documented in this page and counted;
4. it has a recommended configuration for the complementary rules in the table.

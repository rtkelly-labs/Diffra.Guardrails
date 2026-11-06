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
An API with no message slot, such as most of xUnit's `Assert`, gets a DFA106 warning instead of a
finding; see [the adapter contract](../docs/adapters.md#message-slots). That needs a language-specific
case, which is planned.

## Rule precedence in the cases

These are the conventions the expected findings follow. The first checker settled them, and every
adapter must apply them the same way.

1. When DFA101 fires (no markers), DFA102, DFA103 and DFA104 are not reported for that test. They
   only measure marked tests.
2. DFA105 treats an unmarked body as one block, and a marked test per Then block. Its finding is
   on the first assertion past the first one.
3. DFA106 reports every assertion without a message: an error where the API has a message slot,
   a warning where it has none. DFA107 only scores messages that exist.
4. DFA108 reports the line of each `if`, loop or `try` statement in the test body.

## Cases

| Case | Shows | Expected |
| --- | --- | --- |
| 001-marked-small-test | The target shape: markers, one Act statement, one assertion with a message | none |
| 002-two-assertions-without-messages | Assertion Roulette: two checks in one Assert, no messages | DFA105, DFA106 ×2 |
| 003-unmarked-test-with-a-loop | No markers; a loop where a data-driven test belongs | DFA101, DFA108 |

## Adding a case

1. Pick the next number and a name that says what the test shows.
2. Write the test in every language, keeping the same statement order so the shapes match.
3. Write `expected.json`. Take line numbers from the files (for example with `grep -n`), not by eye.
4. CI checks that the JSON parses. The adapters' own CI checks the findings.

Planned next cases: Act & Assert for an expected exception; a shared-context class whose fixture
carries Arrange and Act; a medium test with two When/Then pairs; a marker misspelt (DFA102); an
assertion in the Act block (DFA104); an inline exemption; an assertion API with no message slot
(DFA106 warning).

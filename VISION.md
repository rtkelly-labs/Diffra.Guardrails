# Diffra Guardrails: vision

Status: proposal, 6 November 2026. Nothing here is built yet in this repository. A first Python
checker for rules DFA101–DFA108 is being trialled in one private codebase and will move here once
it has proved itself.

## Purpose

AI agents now write a large share of the tests. Most of those tests pass and add coverage. Far
fewer pin down behaviour, and fewer still tell a reviewer anything when they fail. Diffra Guardrails
sets limits on the shape a test can take. A test written within them is short, has one purpose, and
explains itself when it fails. A test that breaks them is reported in terms an agent can act on.

The project rests on one belief: **the right constraints, delivered at the moment a test is
written, produce better tests than open-ended guidance**, whether a person or a model writes them.
That belief is a hypothesis. The project measures it (see [Measurement](#measurement)) and drops any
rule that does not earn its keep.

Guardrails produces evidence and leaves the gating to Diffra. Each language's checker turns test
code into typed findings, counts them, and emits them as a Diffra metrics payload. Diffra compares
the payload with a trusted baseline and applies budgets. The checker stays deterministic and
explainable, and Diffra makes sure test quality cannot get worse quietly, one agent change at a
time.

**Core rule:** each rule is defined once and checked the same way in every language. Comment
markers declare the structure, and the checker confirms each block contains what its marker claims.
Findings are evidence; policy decides what they mean. New tests must meet every rule, changed tests
must meet the structural ones, and the rest of the repository is scored and ratcheted. No language
model is involved in checking.

## It is not only a checker

A rule that fires in CI, twenty minutes after an agent has finished, is the most expensive place to
learn about a test's shape. Guardrails is a set of interventions, each cheap on its own, that make
the right shape the default:

1. **Authoring defaults:** templates, an `AGENTS.md` block and a skill, so the first draft already
   has markers.
2. **Edit-time feedback:** an agent hook, pre-commit and IDE diagnostics, so findings reach the
   agent inside its own edit loop.
3. **CI:** one pinned action that gates changed tests, annotates lines and writes the Diffra payload.
4. **Wrappers:** one CLI over every language's checker, test-runner plugins that improve failure
   output, and a codemod for markers.
5. **Baseline and ratchet:** a template Diffra rules file, so a repository adopts with no day-one
   failures.
6. **Measurement:** so each intervention is turned on only after its effect can be seen.

The design for each layer, its cost per repository and how to measure it are in
[docs/interventions.md](docs/interventions.md).

## Why constraints, and what they can and cannot do

A test is worth keeping when its cost to read and maintain is less than what it tells you when it
fails. Generated tests often fall short in the same few ways, all described in the literature:
Assertion Roulette (van Deursen et al., *Refactoring Test Code*, 2001), Eager Test, Conditional Test
Logic and Mystery Guest (Meszaros, *xUnit Test Patterns*, 2007). Open-ended advice such as "write
clear tests" does not reach a model at the moment it writes the test. A rule that checks the shape,
and that the agent reads as a finding in its own loop, does.

The research sets honest limits on the claim:

- **Smells predict cost more than missed bugs.** Studies of test smells, including PyNose (Wang et
  al., ASE 2021), link them to maintenance cost and flakiness more than to faults that escape. So
  the rules aim at readability, diagnosis and maintenance. Whether a test finds bugs is measured
  separately, by mutation score.
- **Only some bans pay.** Boogerd and Moonen (around 2008) checked MISRA C rules against recorded
  faults and found that only some correlated with faults; the rest added work. Each DFA rule
  therefore needs a stated failure mode it prevents and a measure that shows it, and is retired if
  the measure does not move.
- **Assertion count matters.** Zhang and Mesbah (FSE 2015) found that the number of assertions
  correlates strongly with a suite's effectiveness. DFA105 (one assertion per Then) must not
  quietly reduce what a suite checks. Its intended fix is one assertion over a whole value (a
  record, tuple or object) or a split into more tests, not a deleted check. The mutation score of
  new tests is the check on this (see [Measurement](#measurement)).
- **Small tests locate faults.** Trautsch, Herbold and Grabowski (JSS 2020) found that unit and
  integration tests caught the same kinds of defect, but unit tests located them more precisely.
  That is the case for the tight Act budget on small tests: one action, one place to look.

The structure itself is Arrange-Act-Assert (Wake), or Given-When-Then for tests written as
behaviour. Both are written as comment markers. Markers make the author's intent visible, so a
person can review it and a tool can check it. Guessing structure from statements alone is
unreliable, so it is used only for warnings and for one-off migrations.

## Rules

Rule IDs use one prefix, `DFA`, with a number range for each family, as Roslyn analyzers do. An ID
never changes meaning and is never reused. Each language maps the rule onto its own syntax.

| ID | Rule | Gate | Prevents |
| --- | --- | --- | --- |
| DFA101 | Test has no `Arrange/Act/Assert` or `Given/When/Then` markers | new and changed tests | Intent nobody can review |
| DFA102 | Marker misspelt or out of order (autofix to the exact form) | new and changed tests | Markers that drift until they mean nothing |
| DFA103 | Act/When over budget for the test's size: small 1 statement; medium ≤ 2 When/Then pairs; large ≤ 4; every When followed by its Then | new and changed tests | Eager Test |
| DFA104 | Assertion outside the Assert/Then block | new and changed tests | Checks hidden in setup |
| DFA105 | More than one assertion in one Then, unless it is a single call to a custom assertion helper | new tests | Assertion Roulette |
| DFA106 | Assertion with no failure message; a warning, never gated, where the assertion API has no place for one | new tests | Failures that need a debugger to read |
| DFA107 | Low-quality message: restates the expression, contains the expected literal, uses a generic phrase, or is under three words | score only | Messages written to satisfy DFA106 |
| DFA108 | Control flow (`if`, loop, `try`) in a test body | new and changed tests | Conditional Test Logic |

DFA105 and DFA106 gate only **new** tests. Editing an old test makes you fix its structure, not
rewrite every assertion in it. This is the choice the first checker made after meeting a real
codebase, and it is what keeps the gate cheap to adopt.

Accepted forms:

- **Act & Assert** is the one combined marker. It is for asserting an exception or rejection, as in
  `pytest.raises`, `Assert.Throws` or `expect(...).rejects`. Such a form counts as an assertion for
  DFA104 only.
- **No Arrange** when there is nothing to set up.
- **Shared context:** a fixture or class setup carries the Arrange/Act markers, and each test carries
  only Assert. The checker finds the fixture's markers by looking up the fixture.
- **Data-driven tests** (`parametrize`, `[Theory]`, `test.each`) are one test. The case table is
  data, not Arrange statements.

Precedence: when DFA101 fires, DFA102–DFA104 are not reported for that test, and DFA105 treats the
unmarked body as one block. The [conformance corpus](conformance/README.md) pins these conventions.

**Exemptions** are written inline, name one rule ID and give a reason, and are counted as evidence in
their own right. An exemption is never invisible. The syntax is defined per language in
[docs/adapters.md](docs/adapters.md).

**Test size** comes from what a test does: small runs in-process only; medium may use local I/O or
child processes; large crosses process or network boundaries. Size is declared with the framework's
usual mechanism (a marker, trait, tag or naming convention). An unmarked test gets the small budget,
so a looser budget always has to be asked for. Declared sizes drift: Trautsch and Grabowski (ICST
2017) found that developers overestimate how many unit tests they write. Where a runtime probe can
measure a test's real size (by recording whether it opens a socket, starts a process or touches the
disk), the measured size wins.

**Later families** are listed for direction only and are not committed: 2xx test doubles (mocking the
unit under test, asserting on a stub's own setup); 3xx assertion strength (tautologies, asserts that
cannot fail, snapshots that cover everything); 4xx test data (Mystery Guest, unexplained literals);
5xx timing (sleeps, wall-clock dependence, unseeded randomness, which Google's flaky-test reports
and Gruber et al., ICST 2021, rank among the main causes of flakiness). Each family needs a failure
mode often seen in generated tests, with a precise definition, before it becomes a rule.

## Languages

The rule specification is language-neutral. A language joins through an **adapter** that answers a
fixed set of questions: what a test is, what an assertion is, where its message goes, how comments
are reached, how size is declared and how shared context is expressed. The contract, and which
existing linter rules each adapter reuses rather than rebuilds, are in
[docs/adapters.md](docs/adapters.md).

| Language | Host | Status |
| --- | --- | --- |
| Python | stdlib `ast` plus `tokenize`; no third-party parser | first version being trialled; moves here when proven |
| C# | Roslyn analyzer, shipped as `Diffra.Guardrails` on NuGet, with code fixes for DFA102 | next |
| TypeScript / JavaScript | ESLint plugin, `eslint-plugin-diffra`, covering Vitest and Jest | next |
| F# | FSharp.Analyzers.SDK (Roslyn does not cover F#) | later |

Natural next languages are Go, Rust, Java/Kotlin and Swift. None is planned. The adapter contract and
the conformance corpus exist so that adding one is bounded work, not a redesign.

**Conformance corpus.** A shared folder of test cases, each written in every supported language and
paired with its expected findings. A language is supported once its adapter passes the whole corpus.
A rule changes in the corpus first, then in every adapter. Without it the checkers would drift into
dialects.

## Gate shape

A zero baseline does not work in a real codebase. The first one this was tried on had about 2,850
tests and 6,400 asserts, of which 2% had a message. So:

- **Gate** only the test functions added or changed since the merge-base. Agents write many new
  tests, and every one meets the rules from its first commit. Untouched tests are left alone.
- **Score** the whole repository for each rule and report it as Diffra evidence. Diffra compares it
  with the trusted baseline and applies budgets. Counts are grouped by file, so a fix in one file
  cannot hide a regression in another.
- **Budget only what should fall.** Diffra budgets increases, so every evaluated metric is a count
  where lower is better: violations, assertions without a message, exemptions. Totals such as tests
  and assertions are reported beside them but never budgeted. The rules file and its consequences
  are in [docs/interventions.md](docs/interventions.md#5-baseline-and-ratchet).

## Findings an agent can act on

The checker's main reader is an agent in the middle of an edit, not a person scanning CI output.
Every finding names the rule, the exact lines, and the change that would fix it, such as "move this
assertion under `# Assert`" or "split this test: it has two Then blocks". Messages are stable, so an
agent's instructions can refer to them. Every rule has a short, deterministic explanation that can
be passed to a model as context.

## Measurement

Guardrails is judged by whether tests written under it get better, not by how many findings it
raises. The headline measures:

- findings per agent pull request, and the share of agent pull requests whose first push passes;
- the mutation score of new tests, compared between tests written with and without the rules, so
  that a rule which makes tests tidier but weaker shows up;
- the share of new assertions with a message, and how often that message alone explains a killed
  mutant.

Each intervention is switched on only after a baseline for its measure exists. Details are in
[docs/interventions.md](docs/interventions.md#6-measurement).

## Deliberate first-version limits

Python first, then C# and TypeScript. No formatter, no general-purpose linting, and no autofix beyond
marker spelling, order and the marker codemod. No language model in the checking loop. No coverage or
mutation measurement: those are separate Diffra evidence, and Guardrails sits beside them. No rules
about production code; rules run only on recognised test files. No plugin system for user rules
until the adapter contract has held for three languages.

## Success test

The same test case, written in Python, C# and TypeScript, produces the same DFA findings in all
three. An agent given only the finding text can fix the test. A change that adds a test with two
Thens, or an assertion with no message, fails its gate. The same change to an existing, untouched
test does not fail but shows in the repository score. Diffra reports the score change against the
baseline, by rule and by file. Adding a fourth language takes an adapter and a passing corpus, and no
change to the rules. And the mutation score of tests written under the rules is no lower than before.

## Names

The project is **Diffra Guardrails**, and the repository is `Diffra.Guardrails`. Rule IDs are `DFA`
plus a three-digit number. Packages: `Diffra.Guardrails` (NuGet, the Roslyn analyzer),
`Diffra.Guardrails.Tool` (NuGet, the `diffra-guardrails` CLI as a .NET tool), `eslint-plugin-diffra`
(npm) and `diffra-guardrails` (PyPI, the Python checker). Configuration lives with the Diffra
configuration of the repository being checked. The project uses the Diffra protocol to share evidence and does not depend on the internals
of the Diffra host.

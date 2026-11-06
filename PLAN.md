# Plan: the first buildable slice

Status: proposal, 6 November 2026. Stacks on PR #1 (`rtkelly13/foundation`), which holds the
design this plan builds from. Read [VISION.md](VISION.md), [docs/adapters.md](docs/adapters.md) and
[conformance/README.md](conformance/README.md) before starting.

## Goal

Build the **C# syntax-only adapter** and the **`diffra-guardrails` CLI** so that, for every case in
an expanded conformance corpus, `dotnet test` reports exactly the expected C# findings, and the CLI
can check any C# test file, print findings an agent can act on, and write the three Diffra metrics
payloads.

The slice is finished when all of these hold on a clean checkout:

1. `dotnet test Diffra.Guardrails.slnx -m:2` passes, including one conformance test per corpus case.
2. The corpus has at least one case for every rule DFG101–DFG108 and every precedence rule in
   [conformance/README.md](conformance/README.md#rule-precedence-in-the-cases).
3. `dotnet run --project src/Diffra.Guardrails.Tool -- check --all --format agent` over
   `conformance/cases` prints exactly the corpus's C# findings, and exits 1 because some are gated.
4. `check --base <ref>` marks a test `new`, `changed` or `untouched` against the merge-base, and
   gates only new and changed tests.
5. `payload --out <dir>` writes the structure, score and totals payloads, and each one is a valid
   Diffra metrics payload (see [Payload check](#payload-check)).
6. CI builds and tests on every pull request that touches `src/`, `tests/` or `conformance/`.

## Why this slice

- **It has a machine-checkable end.** The conformance corpus is the oracle: a case either matches
  or it does not. An agent can work toward it without asking what "done" means.
- **It matches the repository's tooling.** Repository tooling is C#, the CLI is planned as a .NET
  tool, and the C# syntax-only mode lives inside the same tool. One toolchain covers it all.
- **It does not collide with work elsewhere.** The Python checker is being trialled in a private
  codebase and moves here once proven (VISION.md). Rebuilding it here would fork it. *To overrule:
  say "Python first" and phases 3–4 retarget to a Python adapter run by the CLI as a child process.*
- **It unblocks the rest.** The edit-time hook needs a sub-second per-file check; the CI action
  needs the finding JSON and payloads; the Roslyn analyzer reuses the same rule logic. All three
  sit on this slice.

## Out of scope

Each of these is a later step in [docs/interventions.md](docs/interventions.md#recommended-roll-out).
An agent working this plan does not start them.

- The Roslyn analyzer for IDEs and builds, its code fixes, and the `ingest --sarif` path.
- The ESLint plugin, the Python adapter and the F# adapter. Their corpus files are still written.
- The Claude Code hook, pre-commit entry, `agents-md`, `new test`, `init` and `codemod markers`.
- The composite GitHub Action, annotations, job summary, baseline fetching and run records.
- Running `diffra diff` or `diffra eval`, and the rules-file templates.
- Publishing to NuGet, and any release.
- Runtime size probes. Size comes only from declarations.

## Decisions this plan pins

These are choices the design leaves open. Each is the default an agent uses; the user can change
any of them before work starts. Anything not covered here and not pinned by a corpus case is a
[stop condition](#stop-conditions).

### Toolchain

- `global.json` pins `10.0.401` with `rollForward: latestPatch`, the newest SDK installed locally.
  Diffra pins `10.0.403`; nothing here depends on Diffra's build, so the pins need not match.
- Target `net10.0`. Nullable and warnings-as-errors on, in `Directory.Build.props`.
- Dependencies: `Microsoft.CodeAnalysis.CSharp` (syntax only, no workspaces), `System.CommandLine`,
  `JsonSchema.Net` (to validate corpus files and payloads), and xUnit with AwesomeAssertions for
  the repository's own tests. Not FluentAssertions: version 8 onwards is under a paid commercial
  licence. (The corpus files may still use the `FluentAssertions` namespace; they are parsed, never
  built.) Nothing else without a reason in the pull request.

### Layout

```text
Diffra.Guardrails.slnx
src/Diffra.Guardrails.Core/        finding model, rule catalogue, block model, payload writer
src/Diffra.Guardrails.CSharp/      C# syntax-only adapter (CSharpSyntaxTree.ParseText)
src/Diffra.Guardrails.Tool/        the diffra-guardrails CLI (PackAsTool, not published)
tests/Diffra.Guardrails.Tests/     unit tests, plus one conformance theory row per corpus case
```

The block model (which line sits in which marker block) lives in Core and is language-neutral. An
adapter only answers the six questions in [docs/adapters.md](docs/adapters.md#the-six-questions)
and hands Core a list of tests, statements, assertions and comments with line numbers.

### Finding text

`message` and `fix` are a public contract (AGENTS.md). This table is the first version. It lives
in one place in Core, `{n}` and similar are the only substitutions, and the corpus pins each `fix`.

| Rule | `message` | `fix` |
| --- | --- | --- |
| DFG101 | This test has no Arrange/Act/Assert or Given/When/Then markers. | Add Arrange, Act and Assert markers (or Given, When and Then). |
| DFG102 (spelling) | The marker `{found}` should be written `{marker}`. | Change the comment to exactly `{marker}`. |
| DFG102 (order) | The marker `{marker}` is out of order: it follows `{previous}`. | Reorder the blocks to Arrange, Act, Assert (or Given, When, Then). |
| DFG102 (mixed) | This test mixes Arrange/Act/Assert and Given/When/Then markers. | Use one set of markers in each test. |
| DFG103 (statements) | This Act block has {n} statements; a {size} test acts once. | Move setup into Arrange, or split this test in two. |
| DFG103 (pairs) | This test has {n} When/Then pairs; a {size} test allows {max}. | Split this test, or declare a larger size if it really crosses I/O. |
| DFG103 (no Then) | This {block} block is not followed by an {expected} block. | Add an {expected} block that checks what this {block} did. |
| DFG104 | This assertion is in the {block} block. | Move this assertion under `{expected}`. |
| DFG105 | This {block} block has {n} assertions; a Then checks one thing. | Split this test in two, one per behaviour, or compare both values in one assertion. |
| DFG106 (error) | This assertion has no failure message. | Add a message that says what behaviour this assertion protects. |
| DFG106 (warning) | This assertion API has no place for a failure message. | Use an assertion form that takes a message, or wrap this check in a registered helper that does. |
| DFG107 | This failure message {reason}. | Say what behaviour broke, not what the values were. |
| DFG108 (loop) | This test has a loop. | Replace the loop with a data-driven test: one case per input. |
| DFG108 (if, switch) | This test has a branch. | Split this test so each branch is its own test. |
| DFG108 (try) | This test catches exceptions itself. | Use the Act & Assert form to check the exception. |

DFG107 `{reason}` is one of: `restates the assertion`, `contains the expected value`,
`is a generic phrase`, `is under three words`.

### Rule details the design leaves open

- **DFG102 spelling.** A single-word line comment of four or more letters within edit distance 2 of
  a marker word, case-insensitive, or a marker with a trailing colon or different case
  (`// arrange:`), is misspelt. `Act and Assert` is a misspelling of `Act & Assert`.
- **DFG103 budgets.** Small: one Act/When block of exactly one statement. Medium: up to two
  Act/When blocks, large up to four, each followed by its Assert/Then; statements per block are not
  limited above small. A C# test declares size with `[Trait("Size", "Medium")]` or NUnit
  `[Category("Medium")]`; anything else is small.
- **DFG104.** An assertion in Arrange, Act, Given or When, or before the first marker. `Act & Assert`
  is exempt.
- **DFG107.** *Restates*: every word of the message, ignoring `a`, `an`, `the`, `is`, `be`,
  `should`, `to`, `equal`, matches an identifier in the assertion. *Expected value*: the message
  contains the source text of a literal argument. *Generic*: case-insensitive match against a list
  held in Core (start with `assertion failed`, `values should match`, `should be equal`,
  `unexpected value`, `wrong result`, `test failed`, `expected value`). *Short*: under three words.
- **DFG108.** `if`, `switch`, `for`, `foreach`, `while`, `do` and `try` statements anywhere in the
  test body, including lambdas and local functions. Conditional expressions (`?:`, `??`) are not
  control flow.
- **C# assertions.** `Assert.*` calls; any call chain containing `.Should()`; `Verify(...)`; and
  helpers named in an optional `diffra-guardrails.json` at the repository root.
- **Configuration.** `diffra-guardrails.json` holds `{"helpers": [...], "exclude": [...]}`.
  `exclude` is a list of globs the CLI never checks; this repository excludes `conformance/**`,
  whose files break the rules on purpose.
- **C# message slots.** The framework comes from the file's `using` directives (`Xunit`,
  `NUnit.Framework`, `Microsoft.VisualStudio.TestTools.UnitTesting`). xUnit `Assert.True`/`False`
  have a slot (second argument); other xUnit `Assert.*` have none. NUnit and MSTest asserts have a
  `message` argument after the values. For `Should()` chains, a named `because:` argument always
  fills the slot; otherwise the slot is the argument after the method's value arguments, from an
  arity table in the adapter (`Be`, `NotBe`, `BeEquivalentTo`, `Contain`, `HaveCount`,
  `BeGreaterThan`… take one value; `BeTrue`, `BeNull`, `BeEmpty`… take none). Unknown methods are
  assumed to take one value. Write the table into [docs/adapters.md](docs/adapters.md#message-slots).
- **Corpus schema.** A language-specific case (such as the xUnit `Assert.Equal` DFG106 warning)
  lists only the languages it covers. Change `expected.schema.json` so `files`, `test` and `line`
  require at least one language rather than all three. A case that covers all three stays the norm.

### CLI

- `check (--all | --files <paths...> | --base <ref>) [--format agent|json|github]`, C# files only
  for now; other extensions are skipped with one notice line.
- Without `--base`, every test has scope `new`, the strictest. That is also what the edit-time hook
  wants when it checks one file. Explicit `--files` paths ignore `exclude`.
- `payload --out <dir> [--scope <name>]` writes `test-structure-v1.json`,
  `test-structure-score-v1.json` and `test-structure-totals-v1.json`, shaped as in
  [docs/adapters.md](docs/adapters.md#the-diffra-metrics-payload).
- Exit codes: 0 when no gated finding remains; 1 when one does; 2 for usage or internal errors.
- `agent` format: one block per finding, `path:line DFG105 message`, then `fix:` on the next line.
  `json`: the finding objects as a JSON array. `github`: `::error`/`::warning` lines, at most ten
  errors, then one `::notice` with the count left out.

### Payload check

`diffra collect` is the real test of payload shape, but Diffra's CLI is still on an unmerged pull
request stack and pins an SDK not installed here. So: a unit test validates each payload against
the rules in Diffra's published CLI documentation (`scope` and `methodology` strings, only
`metrics` besides them, measurements as `{value, unit}` with integer values). Round-tripping
through `diffra collect` is a follow-up once Diffra's stack lands.

## Phases

One pull request per phase, stacked, each with a Conventional Commits title. Each phase ends with a
command whose result proves it finished.

### Phase 0: build skeleton

`global.json`, `Directory.Build.props`, the solution and the four empty projects, and a `build`
job in `.github/workflows/ci.yml` that runs `dotnet test -m:2` only when `src/`, `tests/`,
`conformance/` or build files change. Filter inside the job (a first step decides, later steps
skip), not with workflow-level `paths:`, so the job always reports and a required check never
hangs at "expected" on a docs-only pull request. Check out with `fetch-depth: 0` and
`filter: blob:none` so phase 5 and 6 can resolve the merge-base. Actions pinned by 40-character
SHA, tag in a comment.

**Done when** `dotnet test Diffra.Guardrails.slnx -m:2` passes with one placeholder test, and the
CI job runs green on the pull request.

### Phase 1: corpus expansion

Write the cases below, each in Python, C# and TypeScript unless marked, following
[Adding a case](conformance/README.md#adding-a-case). Take every line number from `grep -n`.
Update the README's case table and the schema change above.

| Case | Shows | Expected |
| --- | --- | --- |
| 004-act-and-assert-for-an-exception | Act & Assert on an expected exception | none |
| 005-shared-context-fixture | Arrange and Act in the constructor or fixture; tests carry only Assert | none |
| 006-medium-test-two-when-then-pairs | A declared medium test within budget | none |
| 007-misspelt-marker | `# Arange` | DFG102 |
| 008-markers-out-of-order | Assert before Act | DFG102 |
| 009-two-statement-act-in-a-small-test | Act over budget | DFG103 |
| 010-when-without-then | A When with no Then after it | DFG103 |
| 011-assertion-in-the-act-block | A check hidden in Act | DFG104 |
| 012-unmarked-test-with-two-assertions | Precedence: DFG101 suppresses DFG102–104; DFG105 sees one block. Both assertions have good messages | DFG101, DFG105 |
| 013-generic-and-short-messages | Messages that only satisfy DFG106 | DFG107 ×2 |
| 014-try-catch-in-a-test | Exception caught by hand | DFG108 |
| 015-inline-exemption | `DFG105` exempted with a reason | none (exemption counted) |
| 016-xunit-assert-without-message-slot | C# only: `Assert.Equal` | DFG106 warning |
| 017-data-driven-test | `[Theory]`, `parametrize`, `test.each` with a case table | none |
| 018-mixed-marker-families | Arrange, then When, then Then | DFG102 |
| 019-medium-test-three-when-then-pairs | A declared medium test over budget | DFG103 |
| 020-if-in-a-test | A branch on the result | DFG108 |

Together these pin every `fix` variant in the finding text table.

**Done when** every `expected.json` validates against the schema (a theory in the test project
checks this), the JSON step in CI passes, and the README table lists every case.

### Phase 2: Core

The finding model and its JSON, the rule catalogue (the finding text table above, nowhere else),
the language-neutral block model and rule evaluation, and the payload writer. Unit tests drive the
block model from hand-built inputs, with no parser involved.

**Done when** unit tests cover each rule's positive and negative case through the block model, and
a payload test passes the [payload check](#payload-check).

### Phase 3: C# adapter

Parse one file with `CSharpSyntaxTree.ParseText`, find tests, assertions, message slots, markers
from trivia, size declarations, shared context (constructor, `[SetUp]`, `[TestInitialize]`) and
exemptions (`#pragma warning disable DFGnnn // reason` and `[SuppressMessage]`). An exemption with
no reason is an error, not a counted exemption.

**Done when** the conformance theory passes for the C# column of every case, and checking one file
takes under a second (a test asserts it, with generous slack, on the largest corpus file).

### Phase 4: CLI

`check --all | --files` and `payload` as pinned above, over the C# adapter.

**Done when** `check --all --format agent` over `conformance/cases` prints exactly the corpus's C#
findings and exits 1, and `payload` writes three files that pass the payload check. Both are
covered by tests that run the command in-process.

### Phase 5: change scope

`check --base <ref>`: resolve the merge-base with `HEAD` (fail with a hint if it cannot), read each
changed file at the merge-base with `git show`, and mark a test `new` when its qualified name is
absent there and `changed` when `git diff -U0` touches its span. Gate DFG101–DFG104 and DFG108 on
new and changed tests, and DFG105–DFG106 on new tests only.

**Done when** tests that build throwaway git repositories under `temp/` show: a new test with two
assertions in one Assert is gated; an old test with the same problem that the diff does not touch
is not gated but is counted in the payload; an edited old test is gated for DFG104 but not DFG106.
The tests set `GIT_AUTHOR_*` and `GIT_COMMITTER_*` themselves, because CI runners have no git
identity.

### Phase 6: dogfood

Add `diffra-guardrails.json` excluding `conformance/**`, and a
`check --base origin/main --format github` step to the CI build job with `continue-on-error: true`
(report mode: it annotates but never fails the job). New tests written in phases 2–5 follow the
rules; fix any that do not.

**Done when** the step runs on a pull request and reports no gated findings in `tests/`.

## Working rules for an autonomous run

- Follow [AGENTS.md](AGENTS.md): pull requests only, squash merge, no `.sh` or `.ps1`, scratch and
  worktrees under `temp/`, task notes in the ignored `TODO.md`.
- Keep local builds light: build and test only the project that changed, with `-m:2`. Push and let
  CI run the full suite.
- Track progress in `TODO.md`, one line per phase item, so a fresh session can resume.
- A rule changes in the corpus first, then in code. Never make a test pass by editing its
  `expected.json` unless the case itself was wrong, and say so in the pull request.

### Stop conditions

Stop and ask the user, rather than guess, when:

- a case needs a rule decision not pinned in this plan or by an existing case;
- the finding text table needs to change;
- a dependency beyond those listed is needed;
- anything would publish, release, or push to `main`.

## Open items for the user

- PR #1 is still a draft. This plan stacks on it; land it first, or retarget this branch.
- The main checkout has an uncommitted `AGENTS.md` and `.ls-lint.yml` that differ from the
  `AGENTS.md` in PR #1. Reconcile them before an agent reads the wrong one.
- Review the finding text table and the rule details above. They become a public contract when
  phase 2 lands.
- Confirm C# before Python (see [Why this slice](#why-this-slice)).

## Kick-off prompt

Paste this into a fresh session in a worktree of this branch to run the plan:

```text
Read PLAN.md, then AGENTS.md, VISION.md, docs/adapters.md and conformance/README.md. Work the
phases in PLAN.md in order, one stacked pull request per phase. Track progress in TODO.md so you can
resume. Each phase is finished only when its "Done when" check passes; run it and quote the result
in the pull request. Stop and ask me when a stop condition in PLAN.md applies. Do not merge.
```

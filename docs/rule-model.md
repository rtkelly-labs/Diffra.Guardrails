# Rule model

How rules are written, hosted and registered, so that a rule is written once and runs in every
language. This page covers the code structure behind the rules in [VISION.md](../VISION.md#rules).
The questions an adapter answers, and what it emits, are in [adapters.md](adapters.md); where the
checks run is in [interventions.md](interventions.md).

Status: proposal. Nothing here is built. It is informed by a review of
[dotnet-slopwatch](https://github.com/Aaronontheweb/dotnet-slopwatch), a .NET tool that detects LLM
reward hacking in code, and says what to borrow and what to leave.

## Two plugin axes

There are two extension points. Both are internal interfaces for now (see [Staging](#staging)).

| Interface | One per | Does |
| --- | --- | --- |
| `IAdapter` | language | Answers the [six questions](adapters.md#the-six-questions) and returns a language-neutral model of a file: tests, statements, assertions, comments, each with a line number |
| `IGuardrailRule` | DFG ID | Reads that model and returns findings. It never sees a syntax tree |

A rule's input is the block model from [PLAN.md](../PLAN.md#layout): which line sits in which
marker block, plus the tests and assertions an adapter found. The model also carries the scope of
each test (`new`, `changed` or `untouched`), so a rule decides whether a finding is gated from data
it is given and never runs git itself.

Slopwatch has one axis. Its `IDetectionRule` receives a `DetectionContext` that holds the file
text and a Roslyn `SyntaxTree`, so every rule that wants structure is written against C#. That is
fine for a C# tool and wrong here. Splitting the axes gives:

- A rule is written once and runs in every language.
- A new language is one `IAdapter`. The [conformance corpus](../conformance/README.md) proves the
  adapter gets every existing rule right, with no new rule code.
- A rule change is one change plus a corpus case, as [AGENTS.md](../AGENTS.md) already requires.

Rules that cannot be language-neutral do not belong in this model. A check that needs types or
overload resolution needs the semantic model, which only the Roslyn host has (see below). Such a
rule is out of scope until it can be stated against the model.

## Two hosts, the same rule objects

A host loads an adapter, runs the registered rules over its model and reports findings. There are
two for C#, and they run the same `IGuardrailRule` objects.

| Host | Adapter input | Serves |
| --- | --- | --- |
| CLI (`diffra-guardrails`) | Syntax only, no semantic model | The edit-time hook (git-dirty files only, gated rules only, exit 2), CI, and Diffra evidence |
| Roslyn `DiagnosticAnalyzer` | A C# adapter built from the semantic model | The IDE and the build; SARIF goes through the wrapper |

The analyzer host stays an ordinary IDE citizen. It reports Roslyn diagnostics, and the wrapper maps
them onto the shared finding shape, as [adapters.md](adapters.md#findings) already says. The
semantic model lets the analyzer adapter answer some of the six questions better (a registered
helper resolved by type, an assertion found by overload), but it must give the same answers the CLI
adapter gives on the corpus.

Parity is tested, not assumed:

- Both hosts run the whole conformance corpus.
- A parity test runs both over the same files and asserts the findings are identical, by rule,
  path, line and scope.
- A case where they must differ is a case the corpus gets wrong, or an adapter that has drifted.
  Fix one of them; do not add an exception.

TypeScript and Python follow the same shape: one rule model, a host per tool (ESLint, a stdlib
checker), parity against the corpus. The parity test is a C# commitment first because it is the
only language with two hosts in the same process.

## Registry

The registry is the fixed list of rules. It is built once, at startup, from a static list in the
Core project, and is immutable after that. This differs from slopwatch's `RuleRegistry`, which is a
mutable list behind a lock with a `RegisterRule` call. Nothing here registers rules at runtime, so
there is nothing to lock.

- **Duplicate IDs are rejected** when the list is built. The build fails; it does not pick one.
- **Retired IDs are never reused.** A test holds the list of retired IDs and fails if any rule
  claims one. This makes the rule in [AGENTS.md](../AGENTS.md#agent-instructions) a check.
- **`list-rules`** prints each rule's ID, public `message` and `fix` text, and gate, straight from
  the registry. The finding text table in [PLAN.md](../PLAN.md#finding-text) then lives in one place
  in code, and docs and agent instructions can be generated from it rather than copied.

## Staging

[VISION.md](../VISION.md#deliberate-first-version-limits) holds back a plugin system for user rules
until the adapter contract has held for three languages. This model does not change that, because
it is the internal structure, not a public surface.

- `IAdapter` and `IGuardrailRule` stay `internal`. No NuGet package exposes them.
- They may become public once a third adapter lands without any change to the contract. Until
  then, a contract change is cheap because nobody outside depends on it.
- Until then, other tools come in through **ingestion**, not plugins. `ingest --sarif` already maps
  analyzer output onto the finding shape ([interventions](interventions.md#4-wrappers)). The same
  path takes another tool's JSON, such as slopwatch's `--output json`, and records it as Diffra
  evidence under its own methodology. The tool's rules stay its own; Guardrails does not re-host
  them.

## Borrow from slopwatch

**Diff-scoped context.** Slopwatch's `DetectionContext.IsLineInScope` answers whether a line is in
added or modified lines of the diff. Guardrails already scopes tests as `new`, `changed` or
`untouched`. Keep that, and put it in the model the rule receives, so every rule asks one question
and no rule reads a diff.

**Finding identity that ignores line numbers.** Slopwatch hashes the rule ID, the relative path and
a normalized snippet, and leaves the line number out so moved code keeps its identity. Diffra's
baseline needs the same, or moving a test up a file looks like one fixed finding and one new one.
Notes from slopwatch's own code:

- Normalize line endings and path separators. Trim lines only for text where whitespace is not
  significant: slopwatch added a second hash because trimming collapsed findings that differed only
  inside a multiline string.
- Where a test's qualified name is stable, prefer it to a snippet. Two tests with the same snippet
  then stay distinct.

**Checking the justification on an exemption.** An exemption already needs a reason
([adapters.md](adapters.md#exemptions)). Slopwatch also checks the reason: a minimum length,
rejected phrases (`false positive`, `not needed`, `hack`), and an issue link. Guardrails gains the
same checks, so an exemption cannot pass with a reason that says nothing. A failing exemption is an
error, like an empty one. The rejected phrases and the minimum length are fixed text in Core, not
configuration. Reviewer names and emails are not recorded.

**Skipping generated code.** Findings in generated files are noise. Recognise them by the standard
markers (`<auto-generated>` headers, `[GeneratedCode]`, `@generated`) and skip them in the adapter,
so the rules never see them. This is not suppression by path: the file says it is generated, and
the adapter reads that.

**New rule families.** Proposed only; none has an ID. Each needs, before it becomes a rule, a
precise definition and a corpus case, as [VISION.md](../VISION.md#rules) requires for every family.

| Proposal | Family | Notes |
| --- | --- | --- |
| Disabled tests: `Skip`, `Ignore`, `#if false`, and their equivalents in other languages | New. None of the planned 2xx to 5xx families covers it | A skipped test counts as passing coverage while checking nothing. A candidate for gating on new and changed tests |
| Sleeps in tests | The planned 5xx timing family | Fits the existing "sleeps" direction in VISION |
| Empty `catch` in tests | Proposed; family to be settled | DFG108 already flags `try` in a test body. This catches the empty handler where a `try` is otherwise accepted, such as in a helper. Settle whether it is a DFG108 variant before it gets an ID |
| Count of warning suppressions in test projects: `#pragma warning disable`, `[SuppressMessage]`, `NoWarn` | Evidence only | A count in the score payload, budgeted like exemptions. It is not a gated rule |

Every rule above is **proposed** and has no ID yet. Numbers are assigned when a rule lands, following the
family ranges in [VISION.md](../VISION.md#rules), and never before.

## Reject from slopwatch

**Suppression by path glob in a config file.** Slopwatch's own `.slopwatch/config.json` turns off
five rules (SW001 to SW005) for its test-fixtures project with one glob. That is the file-wide and
project-wide suppression that [adapters.md](adapters.md#exemptions) says the CI action rejects, and
the repository's own config shows how readily it is used. Guardrails exempts one rule at one site,
inline, with a reason, and counts it.

One existing feature looks similar and is not. `exclude` in `diffra-guardrails.json`
([PLAN.md](../PLAN.md#rule-details-the-design-leaves-open)) lists files the CLI never treats as
test files, such as `conformance/**`, whose files break the rules on purpose. It removes code from
scope; it does not silence a rule. Keep it that narrow, and do not use it to hide a failing suite.

**A committed local baseline file.** Slopwatch keeps baselined findings in a file in the repository.
Anyone who can edit the repository can edit that file in the same change that adds the finding.
Diffra's trusted baseline, taken from a run on `main`, replaces it
([interventions](interventions.md#5-baseline-and-ratchet)).

**A broad category list.** Slopwatch asks for a category on each suppression, including `Legacy`,
`TDD` and `Testing`. A category such as these explains nothing and fits any case, so it becomes the
standard excuse. Guardrails asks for a specific reason and an issue link, not a category.

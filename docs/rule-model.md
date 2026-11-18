# Rule model

How rules are written, hosted and registered, so that a rule is written once and runs in every
language that can check it. This page covers the code structure behind the rules in
[the rule catalogue](rules/README.md), including each rule's
[scope and host class](rules/README.md#3-scope-and-host-class).
The questions an adapter answers, and what it emits, are in [adapters.md](adapters.md); where the
checks run is in [interventions.md](interventions.md).

Status: proposal. Nothing here is built. It is informed by a review of
[dotnet-slopwatch](https://github.com/Aaronontheweb/dotnet-slopwatch), a .NET tool that detects LLM
reward hacking in code, and says what to borrow and what to leave.

## Two kinds of rule

Every rule has a host class ([rules/README.md](rules/README.md#3-scope-and-host-class)):

- A **syntax rule** can be decided from one file's syntax. It is written once against a
  language-neutral model and runs in every host. Most rules are syntax rules: all of DFG1xx and
  most of DFG2xx to DFG5xx.
- A **semantic rule** needs type or symbol information: whether a type is closed or mutable, what a
  name resolves to, what a lambda captures. It is written per language, natively in a host that has
  a semantic model, and is checked nowhere else. DFG202, DFG204, DFG401, DFG403 and DFG501 are
  semantic.

Both kinds share one registry, one ID space and one finding shape. Only syntax rules use the two
plugin axes below.

Each rule also has a scope: `test` rules see only recognised test files, `code` rules see all code.
The adapter tells the rule which files are tests; a rule never decides that itself.

## Two plugin axes

There are two extension points for syntax rules. Both are internal interfaces for now (see
[Staging](#staging)).

| Interface | One per | Does |
| --- | --- | --- |
| `IAdapter` | language | Answers the [six questions](adapters.md#the-six-questions) and returns a language-neutral model of a file: tests, statements, assertions, comments and, for `code` rules, declarations, each with a line number |
| `IGuardrailRule` | DFG ID | Reads that model and returns findings. It never sees a syntax tree |

A test rule's input is the block model from [PLAN.md](../PLAN.md#layout): which line sits in which
marker block, plus the tests and assertions an adapter found. A code rule's input is a declaration
model: types and their members, property accessors, catch clauses, lambdas and local assignments.
That part of the model is defined by the first code rule that needs each element, not up front. The model also carries the scope of
each test (`new`, `changed` or `untouched`), so a rule decides whether a finding is gated from data
it is given and never runs git itself.

Slopwatch has one axis. Its `IDetectionRule` receives a `DetectionContext` that holds the file
text and a Roslyn `SyntaxTree`, so every rule that wants structure is written against C#. That is
fine for a C# tool and wrong here. Splitting the axes gives:

- A rule is written once and runs in every language.
- A new language is one `IAdapter`. The [conformance corpus](../conformance/README.md) proves the
  adapter gets every existing rule right, with no new rule code.
- A rule change is one change plus a corpus case, as [AGENTS.md](../AGENTS.md) already requires.

A check that needs types or overload resolution cannot be stated against this model. It is a
semantic rule (see [Semantic rules](#semantic-rules)), not a gap in the model to be patched with
type names guessed from syntax.

## Two hosts, the same rule objects

A host loads an adapter, runs the registered syntax rules over its model and reports findings.
There are two for C#, and they run the same `IGuardrailRule` objects.

| Host | Adapter input | Serves |
| --- | --- | --- |
| CLI (`diffra-guardrails`) | Syntax only, no semantic model | The edit-time hook (git-dirty files only, gated rules only, exit 2), CI, and Diffra evidence |
| Roslyn `DiagnosticAnalyzer` | A C# adapter built from the semantic model | The IDE and the build; SARIF goes through the wrapper. Also the only C# host for semantic rules |

The analyzer host stays an ordinary IDE citizen. It reports Roslyn diagnostics, and the wrapper maps
them onto the shared finding shape, as [adapters.md](adapters.md#findings) already says. The
semantic model lets the analyzer adapter answer some of the six questions better (a registered
helper resolved by type, an assertion found by overload), but it must give the same answers the CLI
adapter gives on the corpus.

Parity is tested, not assumed, for every syntax rule:

- Both hosts run the whole conformance corpus.
- A parity test runs both over the same files and asserts the findings are identical, by rule,
  path, line and scope.
- A case where they must differ is a case the corpus gets wrong, or an adapter that has drifted.
  Fix one of them; do not add an exception.
- Semantic rules are left out of the parity test by their host class, read from the registry. That
  is the only exclusion, and it is per rule, never per case.

TypeScript and Python follow the same shape: one rule model, a host per tool (ESLint, a stdlib
checker), parity against the corpus. The parity test is a C# commitment first because it is the
only language with two hosts in the same process.

## Semantic rules

A semantic rule is a native check in a host with a semantic model: a Roslyn `DiagnosticAnalyzer`
for C#, a type-aware ESLint rule for TypeScript. Python and F# have no such host planned, so
semantic rules are not promised there ([VISION.md](../VISION.md#9-deliberate-limits)).

- **It keeps the shared contract.** Its ID, `message` and `fix` text, scope and gate come from the
  registry, like any rule. Only the check is per language.
- **It has corpus cases.** Hosts with a semantic model must pass them. Hosts without one skip them
  and report them as not checked, never as passing.
- **It reaches Diffra by ingestion.** The CLI is syntax-only, so it never runs a semantic rule. The
  build's SARIF goes through `ingest --sarif` ([interventions](interventions.md#4-wrappers)) into
  the same payload. The edit-time hook does not see semantic findings; the IDE and the build do.
- **It uses the semantic model.** A semantic rule that matches type names in syntax gives wrong
  answers on aliases, same-named local types and types declared in another file. If a syntax match
  is good enough, the rule is a syntax rule and belongs in the shared model.

## Registry

The registry is the fixed list of rules, syntax and semantic. It is built once, at startup, from a
static list in the Core project, and is immutable after that. Each entry holds the ID, `message`
and `fix` text, scope, host class and gate; a syntax rule also holds its `IGuardrailRule`. This differs from slopwatch's `RuleRegistry`, which is a
mutable list behind a lock with a `RegisterRule` call. Nothing here registers rules at runtime, so
there is nothing to lock.

- **Duplicate IDs are rejected** when the list is built. The build fails; it does not pick one.
- **Retired IDs are never reused.** A test holds the list of retired IDs and fails if any rule
  claims one. This makes the rule in [AGENTS.md](../AGENTS.md#agent-instructions) a check.
- **`list-rules`** prints each rule's ID, public `message` and `fix` text, scope, host class and
  gate, straight from the registry. The finding text table in [PLAN.md](../PLAN.md#finding-text) then lives in one place
  in code, and docs and agent instructions can be generated from it rather than copied.

## Staging

[VISION.md](../VISION.md#9-deliberate-limits) holds back a plugin system for user rules
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

**New rules.** Proposed only; none has an ID. Each needs, before it becomes a rule, a precise
definition, a scope, a host class and a corpus case, as the
[rule catalogue](rules/README.md#3-scope-and-host-class) requires of every rule.

| Proposal | Family | Notes |
| --- | --- | --- |
| Disabled tests: `Skip`, `Ignore`, `#if false`, and their equivalents in other languages | DFG1xx, `test` scope, syntax | A skipped test counts as passing coverage while checking nothing. A candidate for gating on new and changed tests |
| Count of warning suppressions in test projects: `#pragma warning disable`, `[SuppressMessage]`, `NoWarn` | Evidence only | A count in the score payload, budgeted like exemptions. It is not a gated rule |
| Empty `catch` of a specific type, such as `catch (IOException) {}` | DFG2xx, `code` scope, syntax | DFG203 bans broad catches but accepts a specific type, so an empty handler passes. Settle whether it is a DFG203 variant or a new rule before it gets an ID |

**Sleeps in tests** need no new rule. DFG403 bans `Thread.Sleep`, but its spec limits it to domain
and pipeline code. Widening DFG403's scope to test code is the change to make, in its spec and
corpus first.

Numbers are assigned when a rule lands, in its family's range in the
[rule catalogue](rules/README.md), and never before.

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

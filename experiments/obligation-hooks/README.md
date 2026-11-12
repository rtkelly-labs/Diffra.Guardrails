# Obligation hook experiment

Status: executable, isolated feasibility probe, 12 November 2026. This is **not** a Guardrails
adapter, supported obligation API, production observer or Diffra evidence producer. It does not
change DFG101–DFG108. The [evaluation](../../docs/obligation-experiment.md) explains the findings
and the next experiment.

## Run

From the repository root, on macOS or Linux, with .NET SDK 10, Python 3.14, uv and Node 26:

```text
dotnet run experiments/obligation-hooks/Run.cs
dotnet run experiments/obligation-hooks/Run.cs cucumber-js
```

The first command runs pytest-bdd, ordinary pytest, Behave, Cucumber-JS with TypeScript bindings,
and Reqnroll/NUnit. The second runs just the TypeScript probe. Other selectable names are
`pytest-bdd`, `pytest-plain`, `behave` and `reqnroll`.

The C# file-based app copies fixtures into a fresh UUID directory under ignored
`temp/obligation-hooks/`, installs locked dependencies there, runs the probes, and reconciles
results against [cases.json](cases.json). It leaves command logs, native runner reports, per-run
assertion records, cleanup traces and `summary.json` there. Source directories are not built or
used as output directories. No CI job or package publication is added.

Dependency downloads need network access on a cold cache. The Python lock includes hashes, npm
uses `npm ci --ignore-scripts`, and NuGet uses locked restore. Dependencies are confined to this
experiment. The TypeScript bindings use Node's type stripping; this does not run a TypeScript
type checker. Windows and other runtime versions have not been tested.

## What is exercised

All four BDD implementations run the same [feature](contract.feature). Its ten Outline rows
include good and intentional zero-attempt cases, missing functional/policy assertions, a wrong
registered assertion identity, an unreachable assertion, a missing observer, an over-budget
result, an early functional failure and a setup-step failure. Ordinary pytest calls the exact
same Python fixture as a control.

The synthetic public reader returns rows and sets an attempt count. **It performs no network I/O
or retry algorithm.** Assertion records capture an obligation ID, assertion identity, expected and
observed values, and result. Reconciliation requires both exact obligation IDs independently,
rejects the wrong registration, and requires the observer flag for the attempt obligation. This
flag models missing observation; it is not proof that an actual observer covered every request.

Each native runner is expected to exit 1 because three cases deliberately throw. Exit 1 alone is
not a successful experiment: every expected case must also have one uniquely identified sealed
invocation, the expected two statuses and its cleanup trace. Missing or duplicate results fail
the experiment. The program exits 0 only when its experimental expectations all match.

The assertions intentionally contain conditional branches and missing calls. Do not lint these
runtime fixtures as examples of recommended DFG test shape. They live outside `conformance/`,
whose language files remain parse-only data. [Proposed review cases](../../conformance/proposals/obligations.md)
remain the semantic design reference; no rule IDs or public finding text are allocated here.

## File-tooling controls

The TypeScript probe additionally:

1. Runs Cucumber dry-run discovery and captures Cucumber Messages.
2. Replaces a Then with an undefined step, verifies dry-run still exits 0 and emits `UNDEFINED`.
3. Confirms those dry runs did not execute the evidence hooks.
4. Runs configured Gherkin lint on the valid file, then verifies a misspelt capability tag fails.

Native Cucumber and Reqnroll message files retain binding and execution metadata. Python's native
reports are their runners' JSON formats; they are not interchangeable with Cucumber Messages.
No native report alone establishes that the required assertions executed.

## Measured cleanup trap

The fixture records `before-scenario`, assertion events and `after-scenario`, then records
`fixture-cleanup` separately. In pytest-bdd the final event comes from pytest fixture teardown;
in Behave from `context.add_cleanup`; in Reqnroll from binding-instance `IDisposable`; in
Cucumber-JS from an After hook deliberately registered before the sealing hook (reverse order).
Ordinary pytest uses a yield-fixture finaliser.

The experiment requires cleanup to occur **after** the scenario seal in these arrangements.
This exposes an unsafe integration choice for resource-release checks: sealing in an
after-scenario hook can miss actual disposal. A production integration must explicitly close
product-owned resources and await background work before sealing the relevant observation.
The attempt-budget probe is synchronous and makes no resource-release claim.

## Limits

The fixed registration table is experimental runtime validation, not static symbol/type analysis.
Invocations use UUIDs and local case identity; native-ID/source joins are not normalised across
runners. Parallel workers, reruns, crashes, skipped/pending steps, failed before-hooks, multiple
cleanup failures, async cancellation, inherited tags/Rules/Backgrounds and real transport bypasses
remain follow-up tests. A normal setup-step failure here is distinct from a failed before-hook.
The reconciler is a laboratory oracle, not a hardened parser for untrusted artifacts.

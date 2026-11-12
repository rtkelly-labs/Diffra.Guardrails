# Experiment: obligations through existing BDD runners

Status: research plus executed hook probe, 12 November 2026. The
[obligation contract](obligations.md) remains proposed. The
[isolated experiment](../experiments/obligation-hooks/README.md) exercises runner integration seams;
it implements no DFG rules and makes no claim about real product compliance.

## Recommendation

Use an existing runner's scenario scope and execution metadata, explicit registered assertions,
and an independent expected-inventory reconciliation. Keep file lint, binding discovery and
runtime evidence separate. Prefer pytest-bdd for an existing pytest estate, Cucumber-JS as the
first TypeScript integration probe, and Reqnroll for .NET. Behave is viable where it is already
used. These are engineering judgments based on the hooks below and this limited local probe,
not a ranking of test effectiveness or a reason to migrate an existing suite.

The first result is encouraging: hooks can collect attributable assertion records without a new
test framework. The main discovered risk is cleanup order: "after scenario" does not necessarily
mean "after fixture disposal". The other important result is that a green step or successful
dry run cannot establish obligation completeness.

## Python options

| Candidate | Useful documented seams | Evaluation |
| --- | --- | --- |
| pytest-bdd | Before/after scenario; before step and before step call with resolved function/arguments; after successful step; step error and lookup-error hooks; tags as pytest marks | Best bridge to ordinary pytest tests and existing fixtures; resolved function supports explicit assertion registration. Use error hooks as well as success hooks. Scenario hooks are not the whole pytest teardown lifecycle |
| Behave | Run/feature/rule/scenario/step/tag hooks; layered Context; context cleanup and fixtures; custom formatters | Convenient feature-first runner. Keep evidence scenario-scoped rather than feature/global. Tag hooks can select setup, but ordinary pytest reuse needs a separate bridge |

pytest-bdd exposes the actual step function before its call, unlike a bare text-based lint pass.
Its documented after-step hook is success-only; after-scenario is documented to run after step
failure. Python fixture finalisation still needs an explicit lifecycle design. Do not depend on
private generated-test registries for pre-execution discovery: the current changelog documents
internal metadata changes. Use public collection/markers plus an explicit manifest where needed.
([pytest-bdd hooks and reporting](https://pytest-bdd.readthedocs.io/en/stable/#hooks),
[pytest plugin hook ordering](https://docs.pytest.org/en/stable/how-to/writing_hook_functions.html))

Behave's `environment.py` hosts lifecycle hooks, while fixtures and `context.add_cleanup` perform
cleanup when their context layer is removed. A formatter can provide file/step reporting; it must
not manufacture assertion results from passed step statuses. Use the stable API when implementing
an integration rather than copying a development-version formatter example.
([Behave API](https://behave.readthedocs.io/en/stable/api/),
[Behave fixtures](https://behave.readthedocs.io/en/stable/fixtures/))

## TypeScript options

| Candidate | Useful seams | Evaluation |
| --- | --- | --- |
| Cucumber-JS | Scenario World, named/tag-filtered Before/After, step and run hooks, async handlers; custom formatters and Cucumber Messages | Strong first probe for binding/source/result joins. Preserve test-case-start identity across attempts; do not use scenario title alone |
| Playwright-BDD | Generates native Playwright tests from feature files and step implementations; Playwright fixtures, reports, tracing, parallel/shard execution | Good candidate for a product already using Playwright, especially UI. Requires a separate generated-test/fixture lifecycle probe; not executed here |
| jest-cucumber | Links scenario-local step functions to Jest tests; validation keeps feature text and test definitions aligned; uses Jest setup/teardown | Fits an existing Jest estate. Needs a different discovery/reporting adapter; do not assume a Cucumber World or message stream. Not executed here |

Cucumber-JS After hooks run in reverse registration order; Before hooks run in registration order.
BeforeAll/AfterAll do not have a scenario World. Hooks support tag selection and async work.
The hook arguments expose scenario execution IDs, and Messages expose step definitions and their
source references. This is a useful foundation for joining runtime records to declarations,
but still requires explicit obligation registrations inside those bindings.
([Cucumber hooks](https://github.com/cucumber/cucumber-js/blob/main/docs/support_files/hooks.md),
[hook arguments](https://github.com/cucumber/cucumber-js/blob/main/docs/support_files/api_reference.md),
[formatters](https://github.com/cucumber/cucumber-js/blob/main/docs/formatters.md))

Playwright-BDD's generated tests retain Playwright's runner facilities; evaluate its mapping
alongside ordinary Playwright tests before adopting it. jest-cucumber instead keeps Jest tests
and feature steps in sync, with scenario-local definitions by default. Neither alternative was
installed or benchmarked in this probe. Existing ordinary Vitest tests should keep their runner;
an obligation fixture/reporter bridge can be evaluated separately without introducing Gherkin.
([Playwright-BDD project](https://github.com/vitalets/playwright-bdd),
[jest-cucumber project](https://github.com/bencompton/jest-cucumber))

## Reqnroll for .NET

Reqnroll supports scenario, step, scenario-block, feature and test-run hooks, including async
bindings and tag scoping. Scenario context injection provides an appropriate evidence scope.
Specify hook `Order` explicitly: lower values run first, including after hooks; this differs from
Cucumber-JS reverse After registration. An exception can prevent subsequent hooks of the same
type from running. Keep evidence preservation inside `try/finally`, and reconcile missing outputs
outside the test process. Avoid static scenario state, particularly under parallel execution.
([Reqnroll hooks](https://docs.reqnroll.net/latest/automation/hooks.html),
[context injection](https://docs.reqnroll.net/latest/automation/context-injection.html),
[parallel execution](https://docs.reqnroll.net/latest/execution/parallel-execution.html))

Use ordinary binding hooks for the first experiment. Runtime plugins can register wider
integration services; generator plugins affect generated tests. Do not begin by replacing the
execution engine or coupling to undocumented binding internals.
([Reqnroll plugins](https://docs.reqnroll.net/latest/extend/plugins.html))

Reqnroll v3+ supplies HTML and Cucumber Messages formatters. The latter is promising for common
post-processing with Cucumber-JS; the custom formatter interface explicitly carries a compatibility
warning, so prefer consuming the built-in output first. Dry-run can check binding resolution and
produce metadata without executing step handlers; configure missing/pending steps as errors and
never treat dry-run outcomes as policy execution evidence. The hook probe uses NUnit; xUnit,
MSTest and other host-runner lifecycles have not been tested here.
([Reqnroll formatters](https://docs.reqnroll.net/latest/reporting/reqnroll-formatters.html),
[dry run](https://docs.reqnroll.net/latest/execution/dry-run.html))

## Tooling around feature files

| Layer | Reuse | What Guardrails would still need |
| --- | --- | --- |
| Parsing and source spans | Official Gherkin implementations for Python, JavaScript and .NET; AST and compiled scenarios | Shared stable requirement/obligation identities, capability semantics and mapping of Outline rows to invocations |
| File lint | gherkin-lint allowed/required tags, unused variables, names, logical keyword order and custom rules | Named obligations, explicit applicability/exceptions and expected outcomes; tag presence is insufficient |
| Editing | Cucumber language service/server for completion, navigation, syntax diagnostics, formatting and localisation; Reqnroll IDE integrations | Obligation expansion and diagnostics linked to catalogue/bindings; verify actual runner resolution rather than trusting editor matches |
| Binding discovery | Runner dry-run/registry/formatter metadata | Detect unresolved, ambiguous, pending and incompatible bindings; inspect reported statuses rather than exit code alone |
| Reporting | Native JSON/TRX and Cucumber Messages/HTML | Per-obligation assertion records and expected-inventory reconciliation; a successful step can execute no assertion |
| Architecture | Separate import/dependency/primitive-owner checks | Adoption audit and real-adapter contract tests; feature files cannot establish absence of bypass |

Gherkin provides cross-language parsing and compilation; reuse it instead of regex-based keyword
recognition. gherkin-lint can enforce file conventions and tag vocabulary, and supports custom
rule directories. Keep repository-specific obligation semantics outside general style rules.
([Gherkin project](https://github.com/cucumber/gherkin),
[gherkin-lint rules and extension point](https://github.com/gherkin-lint/gherkin-lint))

The Cucumber language service lists completion, navigation, syntax validation, formatting and
multiple binding languages, including Python, C# and TypeScript. Those are valuable authoring aids,
not proof that the project's runner selected a unique executable binding. Reqnroll documents its
own IDE integration options; editor behaviour was researched, not exercised here.
([Cucumber language service](https://github.com/cucumber/language-service),
[Reqnroll IDE setup](https://docs.reqnroll.net/latest/installation/setup-ide.html))

## Executed experiment and results

Command: `dotnet run experiments/obligation-hooks/Run.cs`.
Local environment: macOS, .NET SDK 10.0.401, Python 3.14.0, uv 0.9.26, Node 26.12.1.
Locked direct versions: pytest-bdd 9.0.1, pytest 9.2.0, Behave 1.3.3, Cucumber-JS 13.3.0,
gherkin-lint 4.2.4, Reqnroll.NUnit 3.3.4, NUnit 4.4.0, NUnit3TestAdapter 6.3.1 and
Microsoft.NET.Test.Sdk 18.11.0. Lockfiles retain transitive resolutions.

| Measurement | Observed result |
| --- | --- |
| Shared cases | 10 per runner/control; 50 invocations, 100 obligation statuses matched the independent expected table |
| Native runner results | Each run had 7 passes and 3 deliberately failing cases |
| Missing/misclassified/unreachable assertions | Detected by the experiment reconciler even where native steps passed |
| Missing observer versus valid zero calls | Missing observation remained missing; the explicit zero case passed |
| Early functional/setup-step failure | Scenario finalisation still emitted records; later unexecuted obligations remained missing |
| Ordinary pytest control | Same obligation outcomes as pytest-bdd using the same fixture |
| Cleanup ordering | Cleanup occurred after the scenario seal in all five configured arrangements |
| Native message metadata | Cucumber-JS and Reqnroll each emitted 10 compiled scenarios, 4 step definitions and 10 test-case starts |
| Binding source detail | Cucumber-JS supplied URI/line references; this Reqnroll build supplied class/method metadata in `sourceReference.javaMethod`, requiring a further source-location join |
| Feature lint control | Valid file passed; misspelt capability tag failed the configured allowed-tags rule |
| Cucumber undefined-step dry run | Exit 0 with `UNDEFINED` in Messages; no evidence hooks executed |

The native-pass count is deliberately not the acceptance criterion. Only two of each runner's
ten cases satisfy both required obligations. The source contains a valid retry assertion under
an untaken branch; its lack of an execution record remains missing rather than becoming pass.

The TypeScript file-tooling controls were also run independently with
`dotnet run experiments/obligation-hooks/Run.cs cucumber-js`. Cucumber documents that dry-run skips
hooks and does not fail the process for undefined/ambiguous steps; the measured undefined-step
control confirms why a discovery gate must inspect the report.
([Cucumber dry-run semantics](https://github.com/cucumber/cucumber-js/blob/main/docs/dry_run.md))

The observer is synthetic: it sets an attempt count, with no actual HTTP transport, retry policy,
resource lifecycle or cancellation. The registration table validates exact assertion identities
at reconciliation time, not compile-time semantic types. The experiment establishes hook
feasibility and the need for reconciliation, not the correctness of a production harness.

## Next bounded experiment

Retain the existing runner choices and replace the synthetic reader with one public operation
using a recording local transport. Give it normal, transient-failure, exhaustion and cancellation
conditions. Start with functional rows plus an absolute attempt cap; add resource release only
after an explicit product-finalisation lifecycle exists. Keep normal tests as the comparison arm.

Before widening adoption, require these additional controls:

1. Generate discovery with selected obligation IDs, independent requirements/limits, source spans,
   assertion registrations and expected invocation inventory before product execution.
2. Join native scenario/row/attempt identities to assertion and observer records; delete one record,
   swap two parallel records and replay an old revision. Every case must become missing or invalid.
3. Run skipped/pending/unbound steps, failed before-hooks, aborted processes, retries and parallel
   workers. A partial artifact must never shrink the selected expected inventory.
4. Explicitly dispose product-owned resources while observers remain attached; inject disposal
   failure and background work. Preserve the initial failure even when cleanup also fails.
5. Independently add an extra transport attempt and bypass the recording abstraction. Verify runtime
   detection at its boundary and the complementary architecture/adoption diagnostic.
6. Test inherited tags, Rule/Background, multiple Examples tables, duplicate scenario titles and
   ambiguous/scoped bindings in each runner; use native parsing rather than assumed equivalence.

Keep this opt-in and local until the negative controls hold. Do not add a general framework,
estate-wide migration or all-language CI matrix on the strength of this small hook probe.

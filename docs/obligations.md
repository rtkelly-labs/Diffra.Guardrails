# Explicit test obligations and optional BDD overlays

Status: proposal, 12 November 2026. No checks, assertion APIs, runner integrations or evidence
formats on this page are implemented. [Review cases](../conformance/proposals/obligations.md)
state the intended positive and negative behaviour without reserving rule IDs.

An [isolated runner experiment](obligation-experiment.md) now evaluates hooks in pytest-bdd,
Behave, Cucumber-JS/TypeScript and Reqnroll, with an ordinary pytest control. Its synthetic
fixtures are executable; the reusable checks and production integrations on this page remain
proposals.

## Recommendation and fit

Clarity of what is being tested is the acceptance criterion. Require a visible functional outcome
**and every applicable named engineering obligation** for a product scenario. An arbitrary
"non-functional assertion" or an assertion count cannot substitute for a specific obligation.
Discovery must explain the expanded contract before execution; reports must explain each result
afterwards. Capability tags are selectors, not hidden checks or proof of behaviour.

The default branch currently contains design documents and three syntax-conformance cases, with
no checker or runtime. The [first build plan](../PLAN.md) targets syntax-only C# checking.
Open build-skeleton work (#3) and adapter/evidence documentation work (#5) are separate from this
proposal. This change adds no parser, dependency, general test framework or runtime gate.

Guardrails can own deterministic declaration and binding lint contracts. Runtime execution and
observation belong to runner/harness integrations; architecture checks need repository-wide
analysis. These exceed the single-file syntax scope of [interventions](interventions.md#principles)
and must remain separate inputs, with their own support status. Keep the existing rules and
payload methodologies unchanged.

Ordinary xUnit, pytest, Vitest and integration tests should express the same contract through
traits, markers, fixtures or configuration. Gherkin is optional. This proposal makes no claim that
Gherkin outperforms equivalent ordinary tests for coding agents, and requires no BDD migration.

## Common model

Trace **requirement → scenario → bound assertion → execution evidence**. A scenario drives a public
product API through declared observable dependency boundaries. Select meaningful scenario ×
execution-condition × obligation combinations, rather than the full Cartesian product.

| Record | Required information |
| --- | --- |
| Scenario | Stable identity, requirement references, public operation, functional expected outcome and source location |
| Condition | Identity, inputs, success/failure/cancellation setup, prerequisites that execution must establish |
| Capability catalogue | Version, recognised names, named obligation expansion, applicability predicates and permitted exceptions |
| Obligation | Stable ID/version, readable expected outcome, absolute limit or comparison oracle, observation boundary, ownership and evidence requirements |
| Binding | Scenario/condition and obligation ID, registered assertion identity/type, arguments and source locations |
| Execution result | Revision, catalogue version, scenario/data-row/condition/invocation identity, obligation, assertion and evidence references, expected/observed values, status and source locations |

Requirement expectations are authored independently of observed values. The assertion registration
defines which exact obligation IDs it can satisfy and its evidence input type. Names such as
`Functional.RowsMatch`, `Resilience.RetryBudget` and `Resources.OwnedStreamsReleased` are illustrative
obligation IDs, not DFG rule IDs. A helper's spelling or namespace alone grants no authority.
Custom helpers require explicit registration and independent tests of their assertion behaviour.

Candidate capability names include `remote_io`, `filesystem`, `subprocess`, `database`,
`serialization`, `streaming`, `concurrent`, `cache`, `sensitive_data` and `ui`. None selects an
unspecified universal policy. A versioned project catalogue must list each selected obligation,
its conditions, boundary and exceptions. An unknown name or unresolved applicability is a
configuration defect; it must not silently expand to an empty successful set.

## Proposed lint contracts

The labels below are discussion names, not allocated rules. Findings need source spans for the
declaration and binding, with related requirement/catalogue locations. Diagnostic wording and
DFG allocation require a later corpus-first change; no existing `message` or `fix` text changes.

| Contract | Exact defect checked | What a clean result establishes |
| --- | --- | --- |
| Observable functional outcome | An opted-in product scenario lacks a requirement-linked expected public result/effect, or lacks a compatible functional assertion binding | Explicit declaration and recognised binding exist; correctness still needs review and execution |
| Visible capability expansion | A declared capability is unknown, a catalogue entry lacks named expectations/boundaries/applicability, or a required exception lacks an allowed reason and source | Discovery can enumerate the selected contract |
| Typed obligation coverage | Any required scenario/condition/obligation lacks a uniquely resolved compatible assertion binding; bound step is recognised as empty/pending | All required IDs have bindings, regardless of how many generic assertions exist |
| Restricted primitive ownership | A resolved restricted I/O reference lies outside its configured approved owner | No forbidden resolved references within the analysed scope; dynamic and generated paths remain explicit limits |

The first two contracts check structured declarations, not whether natural language is meaningful.
Do not claim to detect semantic tautologies from wording. The third requires symbol/binding
resolution or trusted runner metadata when syntax is ambiguous. Report unsupported resolution
instead of accepting a same-named method. Empty-body and explicit pending detection is bounded to
recognised forms; an arbitrary no-op helper needs separate validation.

The fourth is an architecture/adoption companion, not a production-code rule smuggled into the
existing test-only checker. Configure restricted primitives, approved owners and analysis scope;
audit actual integrations against declared capabilities so omitting a tag cannot evade policy.
An observer cannot see above or outside its boundary. Resolved ownership does not prove runtime
adoption; retain real-adapter contract tests and explicit dynamic-call limitations.

### Compatibility with DFG105

DFG105 currently allows one assertion per Then, with a registered helper counted as one.
Do not redefine its meaning to accommodate obligation checks or collapse multiple results into
one opaque boolean. A proposed scenario helper may fit that existing exception only if discovery
exposes all constituent bindings and execution retains each result. Its internal calls still
need binding validation. A runner finaliser may instead evaluate overlays outside the test body.
Neither integration is implemented here. A Gherkin `And` step is not automatically another
comment-marker Then; mapping Gherkin steps to the existing block model needs separate corpus cases.

## Discovery and runtime completeness

Discovery must list the functional outcome, expanded obligations, conditions, applicability,
budgets, observation boundaries, assertion bindings and declaration sources. Show excluded cells
with reasons and expose unresolvable cells as errors. Do not let a tag hide its catalogue.

For each selected invocation, runtime completeness requires **every** required obligation to
produce a compatible, attributable result against that invocation's evidence. Assertion presence
inside an unexecuted branch is insufficient, even if syntax lint passes. A passing test-runner
status is insufficient. Deduplicate by obligation and invocation, not by assertion count; report
conflicting or incompatible results as invalid evidence rather than choosing a passing result.

| Status | Meaning | Compliance handling |
| --- | --- | --- |
| Pass | Assertion executed, required condition and observation established, expected outcome met | Satisfies this cell only |
| Fail | Attributable observation contradicts the expectation | Blocks required compliance |
| Not applicable with reason | Catalogue applicability or an allowed reviewed exception excludes the cell | Visible exclusion, never counted as pass |
| Missing evidence | No execution/result, unsupported binding, wrong attribution, absent observer or incomplete observation | Blocks required compliance |

Skipped, filtered, pending and unexecuted required cells remain missing. An incomplete run cannot
shrink the expected inventory to just the tests that happened to execute. A deliberately partial
run must show its scope and cannot establish full-suite compliance. Missing evidence is never
converted to zero violations or not applicable. A valid zero-event observation needs an attached,
completed observer and a condition that permits zero events, such as a specified cache hit.

An illustrative report line is:

```text
Read remote table / caller cancels / no requests start after cancellation observed
Resources: SDK transport; expected 0 starts; observed 2; fail
Requirement cancel-read; obligation Resilience.NoRequestsAfterCancellation; invocation run-17
Sources: features/read.feature:18; bindings/cancel-read:42; catalogue/remote-io:9
```

Actual diagnostics must link real source spans and evidence references; these paths are placeholders.
Avoid dumping sensitive inputs into reports. Retain safe values or redacted evidence references.

### Harness prerequisites and limits

- Observe a stated boundary: logical-client calls, SDK transport attempts and wire calls differ.
  Test retry layers and multiplication explicitly; a retry cap says nothing about safe writes.
- Correlate async/background operations and isolate parallel invocations, including data rows and
  reruns. A shared global counter cannot establish attribution.
- Establish fault-dependent conditions. Normal success cannot prove cancellation, retry exhaustion
  or rollback; controlled clocks, failures and schedules make relevant expectations repeatable.
- Finalise product-owned resources and background work while observers remain active, then seal
  evidence and evaluate policies. Run finalisation even after functional failure. Distinguish
  caller ownership from operation ownership, and preserve failures when cleanup also fails.
- Check recording/fault-injecting adapters against real adapters at the promised boundary.
  Static adoption checks, mechanism tests and exercised scenario evidence support different claims.
- Use requirement-derived absolute caps/deadlines directly. Reviewed baselines apply to comparative
  regression checks; approving a baseline cannot waive a breached absolute requirement.

## What can overlay a Gherkin file generally?

The file can be an index into several contracts. Keep their results distinct so a readable
scenario does not imply correct bindings or execution. These are candidate overlays, not a
commitment to implement them all in Guardrails or to add every tag to every scenario.

| Overlay | File-level lint or discovery | Additional evidence needed | Limit |
| --- | --- | --- | --- |
| Intent and requirement traceability | Requirement IDs, observable expected outcomes, scenario/condition identity | Reviewed requirements and attributable assertions | Keywords cannot prove meaningful behaviour |
| Examples and input contracts | Outline placeholders, table columns, explicit boundary/error examples | Domain partitions and independent faults/mutants | Table completeness is not domain coverage |
| Binding integrity | Unique bindings, recognised pending/empty steps, typed obligation mapping | Runner binding registry and symbol resolution | Matching step text is not an executed assertion |
| Capability and engineering policy | Expand resilience, efficiency, resource, compatibility and determinism obligations | Boundary-specific observers and assertion results | Tags alone establish none of these qualities |
| Execution conditions | Explicit faults, cancellation, clocks, seeds, workload and schedules | Proof the requested condition occurred | A label cannot establish a fault or schedule |
| Architecture and adoption | Declare boundary/owner and required capabilities | Repository dependency rules and real-adapter tests | File-local checks cannot detect every bypass |
| Security, privacy and accessibility | Declare threat/tenant/data or keyboard/focus expectations | Appropriate runtime probes, specialist checks and human review | Scans and tags cannot establish security or usability |
| Environment and compatibility | Declare supported OS/runtime/provider/version combinations | Relevant CI matrix and cross-version fixtures | One run cannot establish portability |
| Operational constraints | Resource ownership, deadlines, load and recovery requirements | Controlled load/soak/restart tests and cleanup evidence | Many checks cannot be cheap same-run overlays |
| Evidence and governance | Versions, owners, exception reasons, selected inventory and report schema | Revision/provenance validation, freshness and missing-result reconciliation | Formatting and approved baselines cannot prove compliance |

Prioritise intent, visible expansion and binding integrity. Reuse existing Gherkin parsers/linters
for grammar and established runner integrations for binding discovery. A Gherkin adapter would
need to resolve inherited metadata, Background setup, Outline rows and conditional hooks before
attributing obligations. Specify tag precedence and contradictory declarations explicitly; do not
infer policy from step prose. Keep those syntax-specific decisions in adapter documentation.

## Validation and adoption boundary

The [review cases](../conformance/proposals/obligations.md) cover missing/misclassified assertions,
unexecuted branches, missing traces, wrong attribution and absolute-limit violations. Today they
are a manual contract review, not executed detection tests.

The smallest future implementation should validate one explicit scenario/condition manifest and
its catalogue expansion, emit discovery, and diagnose missing/incompatible registered bindings.
Start with one existing ordinary test and one obligation, not a Gherkin parser or new harness.
Before implementation, add cross-language corpus examples with actual source-derived line numbers,
allocate new IDs, pin public finding text and document supported resolution modes. Do not change
any adapter's behaviour without those cases.

Then add runner completeness only when a real integration can supply execution identities and
observer lifecycles. Run the negative controls against it: remove each assertion, bind the wrong
type, put a valid assertion in an unexecuted branch, detach observation, inject an extra attempt,
leak an owned resource and swap parallel evidence. Each must produce its intended attributable
failure or missing result. Test valid zero-call and not-applicable cases to prevent false positives.

Keep detailed results as a separate versioned artifact until a published Diffra evidence contract
is selected and validated. The existing structure/score/totals metrics cannot represent this
matrix or safely encode missing results as sparse zero counts. A later producer may emit summary
counts using published payload shapes, but must retain the expected inventory and per-cell results;
no new evidence type IDs, methodology versions or Diffra host features are asserted here.

Measure diagnostic clarity, binding false positives, independently seeded fault detection and
authoring/maintenance cost before gating adoption. Equivalent ordinary-test and Gherkin variants
should expand to the same obligations and results; evaluate syntax choice separately.

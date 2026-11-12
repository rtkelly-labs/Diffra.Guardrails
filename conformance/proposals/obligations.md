# Obligation contract review cases

Status: proposed acceptance cases, not executable adapter conformance. No DFG IDs are allocated.
These cases precede the [design](../../docs/obligations.md); they do not change DFG101–DFG108,
`expected.schema.json` or the existing corpus. Promote cases to language files and exact source
locations before implementing a rule. Imaginary APIs below describe semantics only.

## Shared example

Requirement `read-rows` expects rows `[A, B]` at the public result boundary. Scenario `read-table`
has condition `transient-then-success`, capability `remote_io` and operation-owned responses.
Its versioned catalogue selects these three required obligations:

| Obligation | Observable expectation | Boundary | Registered assertion |
| --- | --- | --- | --- |
| `Functional.RowsMatch` | Rows equal `[A, B]` | Public API result | `rows_match` |
| `Resilience.RetryBudget` | At most 3 attempts, including the first | SDK transport | `retry_budget` |
| `Resources.OwnedStreamsReleased` | 0 operation-owned responses remain open | Response lifecycle | `owned_streams_released` |

The transient-failure condition also requires an injected retryable failure to be observed before
success. The example limit is a requirement, not a suggested universal retry policy. The budget
obligation alone establishes neither backoff correctness nor retry safety.

## Positive and negative cases

Each row changes only the stated part of the shared example. "Static clear" means the proposed
declaration/binding checks found no defect; it never means runtime compliance.

| Case | Input or change | Static expectation | Runtime expectation |
| --- | --- | --- | --- |
| Complete | All three bindings, matching invocation, fault observed, rows `[A, B]`, 2 attempts, 0 open responses | Static clear | Three passes |
| Missing functional outcome | Delete functional requirement and its assertion | Functional-outcome diagnostic at scenario | Missing functional evidence; cannot pass |
| Missing policy binding | Keep only `rows_match` | Missing binding for each of the two policy IDs | Both policy results missing |
| Misclassified assertion | Bind `rows_match` to `Resilience.RetryBudget` | Assertion registration does not support this ID | Reject incompatible result; retry evidence missing |
| Count padding | Add ten generic assertions but omit `retry_budget` | Retry binding still missing | Retry evidence still missing |
| Unknown capability | Misspell `remote_io` as `remote_oi` | Unknown capability, expansion incomplete | No compliance conclusion |
| Hidden policy | Capability resolves to no visible expansion | Discovery invalid | Execution must not establish compliance |
| Unbound step | Functional Then text has no unique executable binding | Missing or ambiguous binding | Missing functional evidence |
| Pending step | Matching binding exists but is empty or explicitly pending | Flag recognised empty/pending body | Skipped/pending is missing evidence |
| Unexecuted branch | Put a valid retry assertion inside an untaken branch | Binding may be present; DFG108 may separately flag branch | Retry result missing even if test passes |
| Empty observer | Assertions execute but transport observer never attaches | Usually beyond syntax | Missing evidence, not 0 attempts |
| Wrong execution | Retry result belongs to another scenario, data row, condition or attempt | Usually beyond syntax | Reject attribution; retry evidence missing |
| Incomplete trace | Collection starts after first request or stops before background work ends | Usually beyond syntax | Missing evidence for affected obligation |
| Fault absent | Provider succeeds without the required injected failure | Static clear | Condition unestablished; affected results missing |
| Cap breached | Trace has 4 attempts | Static clear | Retry fails: expected ≤ 3, observed 4 |
| Caller-owned stream | Caller-owned stream remains open; operation-owned responses are released | Static clear | Resource obligation passes; ownership remains visible |
| Cleanup fails | Finalisation cannot determine whether an owned response closed | Static clear | Resource evidence missing, or fail if a leak is observed |
| Approved exception | An explicitly inapplicable catalogue entry has a reviewed reason and source | Static clear; exception visible | Not applicable with reason, never pass |
| Invalid exception | Mark retry not applicable because observer is unavailable | Reject unsupported applicability change | Missing evidence, never not applicable |
| Bypass | Product uses restricted transport outside approved owner | Repository architecture check flags resolved reference | Observer alone cannot establish absence of bypass |
| Absolute cap baseline | Approve baseline with 4 attempts against cap 3 | Declaration still expects 3 | Still fails absolute cap |

## Additional acceptance cases before adoption

- Run two scenarios concurrently; exchanging their evidence must fail attribution.
- Exercise cancellation: functional cancellation outcome plus `NoRequestsAfterCancellation`;
  expected 0, observed 2 new starts after cancellation observation must fail.
- Establish an intentional zero-call cache-hit condition with an attached, completed observer;
  zero calls may pass there. The same empty event list without collection provenance must not.
- Fail the functional assertion early; policy finalisation must still produce attributable results
  where possible, and mark unavailable results missing. Do not lose the original failure.
- Use a controlled scheduler and fake clock for overlap/deadline checks; a request count alone
  cannot prove concurrency. Test SDK retries below the logical client separately.

These are review expectations only. No detection has been executed in this repository. A future
implementation must run these negative controls, not merely validate their input format.

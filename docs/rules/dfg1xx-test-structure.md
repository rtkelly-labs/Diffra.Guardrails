# DFG1xx: Test Structure & Readability Rules

Status: Current. Conforms to [VISION.md](../../VISION.md) and [AGENTS.md](../../AGENTS.md).

The DFG1xx rule family enforces structural constraints on the physical shape of tests. By eliminating "Assertion Roulette", procedural bloat, and cyclomatic complexity inside test bodies, tests remain readable, isolated, and trivially diagnostic when failures occur.

---

## Rule Catalogue

### DFG101: Structure Markers Required

* **Severity**: `error`
* **Intent**: Tests must declare clear structural boundaries using Arrange/Act/Assert or Given/When/Then comment markers.
* **Message**: `This test has no Arrange/Act/Assert or Given/When/Then markers.`
* **Fix**: `Add Arrange, Act and Assert markers (or Given, When and Then).`

---

### DFG102: Marker Integrity & Spelling

* **Severity**: `error`
* **Intent**: Structural markers must be correctly spelt, strictly ordered, and must not mix styles within the same test.
* **Sub-rules**:
  * **Spelling**:
    * **Message**: `The marker '{found}' should be written '{marker}'.`
    * **Fix**: `Change the comment to exactly '{marker}'.`
  * **Order**:
    * **Message**: `The marker '{marker}' is out of order: it follows '{previous}'.`
    * **Fix**: `Reorder the blocks to Arrange, Act, Assert (or Given, When, Then).`
  * **Mixed Styles**:
    * **Message**: `This test mixes Arrange/Act/Assert and Given/When/Then markers.`
    * **Fix**: `Use one set of markers in each test.`

---

### DFG103: Act Statement Budget

* **Severity**: `error`
* **Intent**: An Act block must contain exactly one logical invocation in a Small (unit) test.
* **Sub-rules**:
  * **Statement Budget**:
    * **Message**: `This Act block has {n} statements; a {size} test acts once.`
    * **Fix**: `Move setup into Arrange, or split this test in two.`
  * **Pair Budget**:
    * **Message**: `This test has {n} When/Then pairs; a {size} test allows {max}.`
    * **Fix**: `Split this test, or declare a larger size if it really crosses I/O.`
  * **Missing Then**:
    * **Message**: `This {block} block is not followed by an {expected} block.`
    * **Fix**: `Add an {expected} block that checks what this {block} did.`

---

### DFG104: Assertion Placement

* **Severity**: `error`
* **Intent**: Assertions must never be placed inside the Act block. Setup and execution must be decoupled from outcome verification.
* **Message**: `This assertion is in the {block} block.`
* **Fix**: `Move this assertion under '{expected}'.`

---

### DFG105: Assertion Budget & Roulette

* **Severity**: `error`
* **Intent**: Each test should verify a single logical behavior to eliminate "Assertion Roulette" where a failure on check 1 masks failures on checks 2 and 3.
* **Message**: `This {block} block has {n} assertions; a Then checks one thing.`
* **Fix**: `Split this test in two, one per behaviour, or compare both values in one assertion.`

---

### DFG106: Failure Message Required

* **Severity**: `error`
* **Intent**: Assertions must carry an explicit failure message documenting what specific contract failed.
* **Message**: `This assertion has no failure message.`
* **Fix**: `Add a message that says what behaviour this assertion protects.`

---

### DFG107: Mock Budget

* **Severity**: `error`
* **Intent**: Small unit tests should rely on pure domain functions or stubs; mocking frameworks are capped.
* **Message**: `This test configures {n} mocks; a {size} test allows {max}.`
* **Fix**: `Use real domain instances or pure functions, or split the test.`

---

### DFG108: Control Flow in Test Body

* **Severity**: `error`
* **Intent**: Tests must be straight-line execution paths. Loops, `if` conditions, and `switch` statements inside a test body are banned.
* **Message**: `Control flow statement in test body.`
* **Fix**: `Replace the loop with a data-driven test: one case per input.`

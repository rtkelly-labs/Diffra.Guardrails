# Diffra Guardrails for TypeScript & JavaScript

Status: Roadmap & Adapter Architecture, November 2026. Conforms to [VISION.md](../../VISION.md).

`eslint-plugin-diffra` provides deterministic ESLint rules enforcing test structure (DFG1xx) and functional immutability (DFG2xx) for TypeScript and JavaScript projects targeting Vitest, Jest, and Node.js test runners.

---

## 1. Quickstart (Level 1)

### Installation

```bash
npm install --save-dev eslint-plugin-diffra
```

### ESLint Flat Configuration (`eslint.config.js`)

```javascript
import diffra from "eslint-plugin-diffra";

export default [
  diffra.configs.recommended,
  {
    files: ["**/*.test.ts", "**/*.spec.ts"],
    rules: {
      "diffra/test-markers": "error",          // DFG101, DFG102
      "diffra/act-budget": "error",             // DFG103
      "diffra/assertion-placement": "error",    // DFG104
      "diffra/single-assertion": "error",       // DFG105
      "diffra/failure-message": "error",        // DFG106
      "diffra/no-test-loops": "error",          // DFG108
      "diffra/no-property-get-mutation": "error", // DFG201
      "diffra/no-inplace-mutation": "error",     // DFG202
      "diffra/defensive-exceptions": "error",   // DFG203
      "diffra/single-assignment": "error"       // DFG205
    }
  }
];
```

---

## 2. Progressive Documentation Path

* **[TypeScript Tooling & Functional Plan (Level 2 & 3)](tooling-and-plan.md)**: Deep dive into ESLint AST parsing, Vitest/Jest visitor algorithms, comment trivia tracking, and rule implementations.
* **[Rule Catalog & Specifications](../rules/README.md)**: Complete specifications for DFG101–DFG108 and DFG201–DFG205.
* **[The Adapter Contract (The Six Questions)](../architecture/adapters.md)**: Universal requirements every language adapter must satisfy.

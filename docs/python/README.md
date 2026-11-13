# Diffra Guardrails for Python

Status: Roadmap & Adapter Architecture, November 2026. Conforms to [VISION.md](../../VISION.md).

`diffra-guardrails` provides zero-dependency structural boundaries for Python test suites (targeting `pytest`) and modern Python applications (3.11, 3.12, 3.13+), enforcing test shape (DFG1xx) and functional cleanliness (DFG2xx) without LLMs.

---

## 1. Quickstart (Level 1)

### Installation

```bash
uv add --dev diffra-guardrails
```

### Pytest Integration (`pyproject.toml`)

```toml
[tool.pytest.ini_options]
addopts = "--diffra-check"
diffra_bundle = "pragmatic"
```

### Direct CLI Verification

```bash
uv run diffra-guardrails check tests/ --format agent
```

---

## 2. Progressive Documentation Path

* **[Python Tooling & Functional Plan (Level 2 & 3)](tooling-and-plan.md)**: Deep dive into the zero-dependency Python `ast` and `tokenize` visitor, token comment extraction, pytest hook architecture, and rule implementations.
* **[Rule Catalog & Specifications](../rules/README.md)**: Complete specifications for DFG101–DFG108 and DFG201–DFG205.
* **[The Adapter Contract (The Six Questions)](../architecture/adapters.md)**: Universal requirements every language adapter must satisfy.

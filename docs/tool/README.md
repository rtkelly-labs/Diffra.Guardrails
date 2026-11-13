# Diffra Guardrails CLI Tool (`diffra-guardrails`)

Status: Active. Conforms to [VISION.md](../../VISION.md) and [PLAN.md](../../PLAN.md).

`Diffra.Guardrails.Tool` is the cross-language command-line entry point for running Diffra Guardrails checks, generating editor diagnostics for AI agents, and emitting Diffra metrics ratchet payloads.

---

## 1. Quickstart (Level 1)

### Installation

```bash
dotnet tool install --global Diffra.Guardrails.Tool
```

### Basic Commands

```bash
# Check all files in current directory with human-readable output
diffra-guardrails check

# Check specific directory or file with agent-formatted output
diffra-guardrails check src/ --format agent

# Gate only new and changed files against git merge-base
diffra-guardrails check --base origin/main

# Emit Diffra structure, score, and totals metrics payloads
diffra-guardrails payload --out ./artifacts
```

---

## 2. Progressive Documentation Path

* **[Interventions & Rollout Strategy (Level 2 & 3)](interventions.md)**: Templates, editor hooks (Claude Code, Antigravity, Cursor), pre-commit hooks, CI ratchets, baseline fetching, and how to tell whether guardrails help.
* **[Rule Catalog & Specifications](../rules/README.md)**: Specifications and message/fix contracts for all rules.
* **[Dogfooding Policy](../architecture/dogfooding.md)**: How the CLI tool is tested and dogfooded against this repository.

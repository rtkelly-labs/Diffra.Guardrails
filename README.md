# Diffra.Guardrails

Constraints that steer people and AI agents toward well-structured tests, emitted as Diffra evidence.

A test written within the rules is short, has one purpose and explains itself when it fails: it is
marked Arrange/Act/Assert (or Given/When/Then), it does one thing, it checks one thing, and its
assertion says why it matters. Each language's checker reports findings an agent can act on, and
counts them as [Diffra](https://github.com/rtkelly-labs/Diffra) evidence so test quality cannot
quietly get worse.

Status: design. Nothing is built in this repository yet.

- [VISION.md](VISION.md): what this is, the rules, and why.
- [docs/interventions.md](docs/interventions.md): templates, agent hooks, CI, wrappers, the
  ratchet, and how to tell whether any of it helps.
- [docs/adapters.md](docs/adapters.md): what a language adapter must do, and which existing linter
  rules it reuses.
- [conformance/](conformance/README.md): the cases every adapter must agree on.
- [docs/obligations.md](docs/obligations.md): proposed functional and engineering obligations,
  optional BDD overlays and the boundary between lint and runtime evidence; no checks implemented.

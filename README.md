# Diffra.Guardrails

Constraints that steer people and AI agents toward well-structured tests, emitted as Diffra evidence.

A test written within the rules is short, has one purpose and explains itself when it fails: it is
marked Arrange/Act/Assert (or Given/When/Then), it does one thing, it checks one thing, and its
assertion says why it matters. Each language's checker reports findings an agent can act on, and
counts them as [Diffra](https://github.com/rtkelly-labs/Diffra) evidence so test quality cannot
quietly get worse.

Status: design. Nothing is built in this repository yet.

- [VISION.md](VISION.md): what this is, the rules, and why.
- [docs/](docs/README.md): documentation index organized by language and tool with progressive disclosure.
- [docs/rules/](docs/rules/README.md): test structure (DFG1xx), functional architecture (DFG2xx), contract purity (DFG3xx), deep immutability (DFG4xx), and algebraic types & exhaustive matching (DFG5xx) rule specifications.
- [docs/csharp/](docs/csharp/README.md): C# Roslyn analyzer, functional guidance, and performance tuning.
- [docs/tool/](docs/tool/README.md): CLI runner, agent hooks, CI ratchet, and Diffra payloads.
- [conformance/](conformance/README.md): the 45 conformance cases every adapter must agree on.

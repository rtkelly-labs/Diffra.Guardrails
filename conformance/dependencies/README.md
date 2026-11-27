# Dependency-policy corpus

Cases for the dependency-policy rules ([DFG6xx](../../docs/dependencies.md)). They are separate
from the test-structure cases in [`../cases`](../README.md) because their input is a resolved
dependency graph, not a test file, and there is one implementation rather than one per language.

## Layout

```text
conformance/dependencies/
  expected.schema.json          JSON Schema for expected.json
  cases/
    001-meta-package-brings-in-proprietary-coverage/
      banned.dependencies       The ban file for this case
      Pricing.Tests/project.assets.json
      expected.json             today, exit code and findings
```

- **Each case folder is a root.** `diffra-guardrails deps check --root <case>` reads its
  `banned.dependencies` and every graph file under it.
- **Graph files are hand-written and minimal.** A real `project.assets.json` records absolute
  restore paths on the machine that wrote it, so cases never copy one. Files sit in project-named
  folders, not `obj/`, which is ignored.
- **`today` is an input.** Exemption cases pass it as `--today`, so they do not change result as
  the calendar moves.
- **Findings are a set**, compared on `rule`, `path`, `package`, `version` and `via`. The line is
  not compared: in a JSON graph file it says little, and it moves with formatting.
- The test project runs every case through the command in-process
  (`DependencyConformanceTests`).

## Cases

| Case | Shows | Expected |
| --- | --- | --- |
| 001-meta-package-brings-in-proprietary-coverage | A meta-package pulls a banned package in transitively; two restore targets give one finding; the ban's ID is lower case | DFG601 |
| 002-paket-lock-major-version-ban | `paket.lock` with two groups: 8.x banned in one, 7.x allowed in the other; roots from `paket.dependencies` | DFG601 |
| 003-patch-window-ban | A two-patch range ban; the next patch is allowed | DFG601 |
| 004-exemption-active-and-expired | An exemption in date gives nothing; one past its date gives DFG602 | DFG602 |
| 005-prerelease-inside-a-banned-range | `8.0.0-rc.1` sorts below `8.0` but is still banned by `>= 8.0` | DFG601 |
| 006-tool-manifest-and-id-case | A local tool in `.config/dotnet-tools.json`, which neither restore graph lists | DFG601 |
| 007-clean-graph | Allowed versions of banned packages | none, exit 0 |
| 008-paket-lock-without-dependencies-file | `paket.lock` alone: the direct dependencies are unknown, so `via` is empty | DFG601 |

Usage errors (a ban line with no reason, an unknown operator, a root with no graph file) exit 2
and are unit tests, not cases.

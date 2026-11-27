# Dependency policy (DFG6xx)

`diffra-guardrails deps check` fails a build when a banned package is resolved anywhere in the
repository, directly or transitively. It exists so that a package with a commercial or proprietary
licence, or a release known to be bad, cannot arrive unnoticed through a meta-package or a version
bump.

This is a separate family from the test-structure rules. It reads resolved dependency graphs, not
test files, and it runs once per repository, not per language.

## Rules

Rule families DFG1xx–DFG5xx are described in the rule specifications pull request; DFG6xx is
dependency policy.

| ID | Rule | Gate |
| --- | --- | --- |
| DFG601 | A banned package version is resolved | always |
| DFG602 | A banned package version is resolved under an exemption whose date has passed | always |

Both are errors and both are gated: the command exits 1 while either remains.

## The ban file

`banned.dependencies` at the root of the checked tree, or any file passed with `--bans`. Each line
is a Paket `nuget` line, with a required reason and an optional exemption date:

```text
// Licences
nuget FluentAssertions >= 8.0 // Xceed commercial licence from 8.0; use AwesomeAssertions or 7.x
nuget Microsoft.Testing.Extensions.Retry // proprietary Microsoft Testing Platform extension licence
nuget Verify exempt-until: 2027-11-30 // small-revenue licence; review each year

// Known-bad releases
nuget Moq >= 4.20.0 < 4.20.2 // SponsorLink in 4.20.0 and 4.20.1
```

| Form | Covers |
| --- | --- |
| `nuget X` | every version |
| `nuget X 1.2.3`, `nuget X = 1.2.3` | exactly 1.2.3 |
| `>= a`, `> a`, `<= b`, `< b`, combined | every version within all the bounds |
| `~> 1.2` | `>= 1.2 < 2.0`; `~> 1.2.3` is `>= 1.2.3 < 1.3` |
| `exempt-until: yyyy-MM-dd` | the ban is not enforced up to and including that date; DFG602 after it |

- **Bans cover prereleases.** A lower bound with no prerelease label is compared with the
  version's release part, so `>= 8.0` covers `8.0.0-rc.1`. Upper bounds use normal ordering, so
  `< 4.20.2` covers `4.20.2-beta`. Both lean towards banning. Paket's prerelease flags (`alpha`,
  `beta`, `rc`, `prerelease`) are therefore rejected as redundant.
- **IDs ignore case**, as NuGet's do.
- **`==` is rejected.** In Paket it overrides a conflict; it is not a set of versions.
- **Every line must parse.** A line with no reason, an unknown operator or a bad date stops the run
  with exit 2. Skipping it would let the package through.
- Blank lines and lines starting with `//` or `#` are comments.

## What is read

| File | Graph | Direct dependencies |
| --- | --- | --- |
| `project.assets.json`, at any depth | one per restore target (framework, and framework plus runtime) | `project.frameworks.*.dependencies`, plus project references |
| `paket.lock` | one per group, `NUGET` sections only | `nuget` lines of the sibling `paket.dependencies`; without one, findings have no path |
| `dotnet-tools.json`, at any depth | local tools, which neither restore graph lists | every tool |

`.git` and `node_modules` are skipped, as are directories given with `--exclude <path>` (relative
to the root). Symbolic links are not followed. A finding is reported once per graph file, package
and version, with the shortest path from a direct dependency.

`project.assets.json` exists only after a restore. **When no graph file is found the command exits
2**, because an unrestored repository is not evidence of a clean one. The summary on standard error
gives the number of bans, graph files and package versions checked, so a run that read nothing is
visible.

## Command

```text
diffra-guardrails deps check [--root <dir>] [--bans <file>] [--exclude <path>]...
                             [--graph <file>]... [--today yyyy-MM-dd] [--format agent|json]
diffra-guardrails deps payload [the same options as check] --out <dir> [--scope <name>]
```

- Exit 0 when nothing is banned; 1 when a gated finding remains; 2 for usage errors, an unreadable
  ban file or graph file, or no graph file at all.
- `--graph` checks only the named graph files instead of searching the root. Each must exist and be
  one of the three file names above; paths in findings are still relative to the root.
- `--today` fixes the date exemptions are compared with; the corpus uses it.
- `agent` format: `path:line DFG601 message`, then `fix:` on the next line. `json`: an array of
  finding objects.

## Diffra evidence

`deps payload` runs the same check and writes the findings as a Diffra metrics payload,
`<out>/dependency-policy-v1.json`, in the shape described in
[the adapter contract](adapters.md#the-diffra-metrics-payload):

```json
{
  "scope": "example/pricing",
  "methodology": "dependency-policy-v1",
  "metrics": {
    "files": {
      "Pricing.Tests/obj/project.assets.json": {
        "DFG601": { "value": 1, "unit": "violations" }
      }
    }
  }
}
```

- **Per graph file, per rule**: the number of banned package versions resolved, the same count
  as the findings. Lower is better, and a clean graph file is absent.
- **One payload**, so DFG601 and DFG602 share one budget. Exemptions in date are not counted.
- **It measures; it does not gate.** It exits 0 once the payload is written, whatever it counts,
  and 2 for the same errors as `check`, including no graph file. `--scope` defaults to the name of
  the root directory.
- **The methodology is versioned.** Changing what is counted bumps `dependency-policy-v1`.

## During a build

[`msbuild/Diffra.Guardrails.Dependencies.targets`](../msbuild/Diffra.Guardrails.Dependencies.targets)
runs the check in every project's build, so a local build reports a banned package before CI
does. Copy it into the repository and import it from `Directory.Build.targets`:

```xml
<Import Project="$(MSBuildThisFileDirectory)build/Diffra.Guardrails.Dependencies.targets" />
```

| Property | Default | Meaning |
| --- | --- | --- |
| `DiffraGuardrailsDepsMode` | `report` | `report` prints findings and never fails; `gate` fails the build on a finding, or when the check cannot run; `off` |
| `DiffraGuardrailsBanFile` | nearest `banned.dependencies` at or above the project | with none, the target does nothing |
| `DiffraGuardrailsCommand` | `dotnet tool run diffra-guardrails` | how to start the tool |

- **It runs after `ResolvePackageAssets`**, in each project, on that project's
  `project.assets.json` (passed with `--graph`, so the finding names it from the ban file's
  directory). A solution build, a single-project build and `--no-restore` all run it;
  `dotnet restore` alone does not. It is skipped when `DesignTimeBuild` is `true`, as in an IDE's
  design-time builds.
- **A multi-targeted project is checked once per target framework.** With findings, each framework
  prints them, and gate mode gives one error per framework.
- **It checks only restore graphs.** `paket.lock` and `dotnet-tools.json` are not part of a
  project's build; the repository-wide command in CI covers them.
- **It never emits a warning.** Report mode prints messages, so `TreatWarningsAsErrors` cannot turn
  it into a failure. Gate mode emits one error per project, after the findings and their fixes.
- **A missing tool is not a finding.** Exit 1 counts as findings only when the output names a
  DFG6xx rule; a `dotnet tool run` that cannot find the tool also exits 1. When the check cannot
  run, report mode says so and carries on, and gate mode fails.
- **A clean result is stamped for the day.** The stamp in `obj/` is named by the UTC date, so a
  no-op build skips the check until the graph or the ban file changes, and an exemption that expires
  is still caught the next day. Findings are never stamped, so they print on every build.

## Before a commit

The repository's [`.pre-commit-hooks.yaml`](../.pre-commit-hooks.yaml) offers the hook
`diffra-guardrails-deps` to [pre-commit](https://pre-commit.com):

```yaml
repos:
  - repo: https://github.com/rtkelly-labs/Diffra.Guardrails
    rev: <full commit SHA>
    hooks:
      - id: diffra-guardrails-deps
```

- **It runs only when a commit touches** a project file, `Directory.Packages.props`,
  `Directory.Build.props` or `.targets`, `paket.dependencies`, `paket.lock`, `packages.lock.json`,
  `dotnet-tools.json` or `banned.dependencies`. It then checks the whole repository, not the staged
  files.
- **It starts the tool with `dotnet tool run`**, so the repository needs the tool in its local
  manifest. Override `entry` to start it another way.
- **It reads what is on disk.** `project.assets.json` reflects the last restore, not the project
  file being committed, so restore before committing a package change. `paket.lock` and
  `dotnet-tools.json` are committed files and are always current.
- **A fresh clone is not checked.** With no graph file the command exits 2 and the commit is
  blocked, which says to restore first. But the local tool manifest that installs the tool is
  itself a graph file, so in such a repository an unrestored project is not reported: only the
  manifest is checked, and the hook passes.

There is no edit-time hook for this family. An edit to a project file changes nothing the check can
see until the next restore, and the build-time targets above already report at that point.

## Finding text

`message` and `fix` are a public contract, like the test-structure rules' text. Only the names in
braces change.

| Rule | message | fix |
| --- | --- | --- |
| DFG601 | `{Id} {Version} is banned ({constraint}): {reason}.` then, for a transitive package, the sentence `It is resolved through {A} -> … -> {Id} {Version}.` | `Remove the dependency that brings in {Id} ({direct dependency}), or move to a version outside '{constraint}'. To keep it for now, add 'exempt-until: yyyy-MM-dd' to line {n} of {ban file}.` For a ban on every version, `or replace it` instead of the version clause |
| DFG602 | `{Id} {Version} is banned and its exemption expired on {date}: {reason}.` then the same path sentence | `Remove the dependency that brings in {Id} ({direct dependency}), or review the exemption again and move 'exempt-until' on line {n} of {ban file} to a new date.` |

`{constraint}` is the text from the ban file, or `any version`. The reason is quoted from the ban
file, so write it for the person who meets the finding: what is wrong and what to use instead.

The JSON finding carries the common fields from [the finding contract](adapters.md#findings)
(`rule`, `path`, `line`, `endLine`, `severity`, `gated`, `message`, `fix`) and three of its own:
`package`, `version`, and `via`, the path from a direct dependency as `Id Version` strings. It has
no `test`, `block` or `scope`.

## Version ordering

Versions compare as NuGet orders them: up to four numeric parts, missing parts zero; a release
above its prereleases; prerelease labels part by part, numbers numerically and below text, text
case-insensitively; build metadata ignored. This is implemented in Core rather than taken from
`NuGet.Versioning`, which would be a new dependency and asks for licence acceptance on restore.

## Not yet

- **Diffra rules files and upload.** The payload is written, but no rules file or CI step yet
  uploads it or evaluates it against a baseline.
- **Licence lookup.** Bans name packages; the command does not read licence metadata. A licence
  scan would find candidates for the ban file, not replace it.
- **`paket.references`.** Paket graphs are checked per group, not per project.

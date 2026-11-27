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
                             [--format agent|json] [--today yyyy-MM-dd]
```

- Exit 0 when nothing is banned; 1 when a gated finding remains; 2 for usage errors, an unreadable
  ban file or graph file, or no graph file at all.
- `--today` fixes the date exemptions are compared with; the corpus uses it.
- `agent` format: `path:line DFG601 message`, then `fix:` on the next line. `json`: an array of
  finding objects.

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

- **An MSBuild hook** (`Directory.Build.targets` running the check after restore), so a local build
  fails as well as CI.
- **A pre-commit or edit-time hook**, as for the test-structure rules
  ([interventions](interventions.md#2-edit-time-feedback)).
- **Diffra evidence.** Findings are not yet written to the metrics payload.
- **Licence lookup.** Bans name packages; the command does not read licence metadata. A licence
  scan would find candidates for the ban file, not replace it.
- **`paket.references`.** Paket graphs are checked per group, not per project.

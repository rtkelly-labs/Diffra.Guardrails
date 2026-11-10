# Diffra.Guardrails

Constraints on the shape of tests (rules DFG101–DFG108), checked per language and emitted as Diffra
evidence. Read `VISION.md` first, then `docs/interventions.md` and `docs/adapters.md`.

## Agent instructions

- `AGENTS.md` is the only agent-instruction file in this repository. Do not add `CLAUDE.md`,
  `.cursorrules` or other per-tool instruction files; tool configuration (such as hook settings) is
  fine.
- A rule changes in `conformance/` first, then in every adapter. Never change an adapter's behaviour
  without a corpus case that shows it.
- Rule IDs never change meaning and are never reused. A retired rule keeps its number.
- Finding `message` and `fix` text is a public contract that agents quote. Change it deliberately
  and say so in the pull request.
- Depend only on Diffra's published payload and document shapes, never on its internals.

## Conformance cases

- One test per language file per case. Code under test is imaginary; the files are parsed, never
  compiled or run.
- Take line numbers in `expected.json` from the files (for example `grep -n`), not by eye.

## Repository workflow

- Pull requests only, squash-merged, with linear history. Never commit to `main`.
- Pull request titles follow Conventional Commits (`feat:`, `fix:`, `docs:`, `ci:` and so on).
- Pin every GitHub Action to a full 40-character commit SHA, with the tag in a trailing comment.
- No `.sh` or `.ps1` scripts. Repository tooling is C#: a .NET file-based app (`dotnet run tool.cs`)
  for a small script, a project for anything larger.
- Keep CI cheap: lint only what changed in kind, never build what the job does not need.
- Keep scratch files and worktrees under `temp/` (ignored). Keep task notes in an ignored `TODO.md`.

## Checks

CI runs markdownlint over every Markdown file and checks that every JSON file parses. To run the
same checks locally, lint only the files you changed:

```text
npx markdownlint-cli2 <changed .md files>
jq empty <changed .json files>
```


## 🛑 Repository Conventions & Workflow Policy

1. **Squash Merge Only**: All pull requests must be merged into `main` using **Squash and Merge** exclusively.
2. **Delete Branch on Merge**: Feature branches must be automatically deleted immediately upon merge into `main`.
3. **Linear History**: Maintain a strictly linear history. Rebase feature branches onto `main` before merging.
4. **Direct Push Protection**: Direct pushes to `main` are blocked; PR mechanism required (force push allowed).
5. **Local Temp & Worktree Directory**: All temporary files, databases, and worktrees go in `/temp/` (gitignored).
6. **Gitignored Local TODO File**: A root `TODO.md` file MUST exist for local task tracking and be gitignored.
7. **Auto-Merge Enabled**: PRs may enable auto-merge (squash) so they merge automatically once required checks pass.
8. **Pinned Action SHAs**: All GitHub Actions workflows MUST use 40-character commit SHAs instead of mutable tags.
9. **Script Language Policy**: Automation scripts in `scripts/` MUST use the repository's approved
   language stack (e.g., C# `.cs` for .NET, TypeScript/JavaScript for Node, Python `.py` for Python).
   Alien scripting runtimes (e.g., `.py` in C# repos, `.sh`/`.ps1` everywhere) are prohibited.

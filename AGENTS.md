# Diffra.Guardrails

Constraints on the shape of tests (rules DFA101–DFA108), checked per language and emitted as Diffra
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
- No `.sh` or `.ps1` scripts. Repository tooling is Python, run through `uv run` (or `uvx`).
- Keep CI cheap: lint only what changed in kind, never build what the job does not need.
- Keep scratch files and worktrees under `temp/` (ignored). Keep task notes in an ignored `TODO.md`.

## Checks

CI runs markdownlint over every Markdown file and checks that every JSON file parses. To run the
same checks locally, lint only the files you changed:

```text
npx markdownlint-cli2 <changed .md files>
jq empty <changed .json files>
```

---
agent: 'agent'
model: GPT-5.1-Codex-Max
description: 'Generate a ready-to-paste PR title and description from the local diff using the repo template.'
---

**Goal:** Produce a ready-to-paste PR title and description for this repository by comparing the current branch against a user-selected target branch.

**Repo guardrails:**
- Treat `.github/pull_request_template.md` as the single source of truth; load it at runtime instead of embedding hardcoded content in this prompt.
- Preserve section order from the template but only surface checklist lines that are relevant for the detected changes, filling them with `[x]`/`[ ]` as appropriate.
- Cite touched paths with inline backticks. Keep wording concise and scoped to the actual diff.
- Call out validation explicitly: list automated/manual tests run (or why none), plus any remaining manual steps or risks.
- Note breaking changes, migrations, new dependencies, or configuration changes when present.

**Workflow:**
1. Determine the target branch from user context; default to `main` when none is supplied.
2. Run `git status --short` once to spot uncommitted files that may affect the summary.
3. Run `git diff <target-branch>...HEAD` once to review changes. Only drill into specific paths if needed (`git diff <target-branch>...HEAD -- <path>`).
4. From the diff, capture scope (areas/files), intent, risks/breaking changes, migrations/config/dependency shifts, and notable edge cases.
5. Confirm validation: list tests executed with outcomes, or why tests were skipped; include any manual verification steps still required.
6. Load `.github/pull_request_template.md`, mirror its section order, and fill it with the gathered facts. Include only relevant checklist items, marking them `[x]/[ ]` or `N/A` where appropriate.
7. Output the filled template inside a fenced ```markdown code block with no extra commentary, clearly flagging any remaining placeholders for the author.

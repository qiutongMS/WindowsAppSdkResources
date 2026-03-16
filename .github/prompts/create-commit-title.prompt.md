---
agent: 'agent'
model: GPT-5.1-Codex-Max
description: 'Generate an 80-character git commit title for the local diff.'
---

**Goal:** Provide a ready-to-paste git commit title (<= 80 characters) that captures the most important local changes since `HEAD`.

**Workflow:**
1. Run a single command to view the local diff since the last commit:
   ```@terminal
   git diff HEAD
   ```
2. Identify the dominant area from the diff (e.g., folder paths, module names), the type of change (bug fix, feature, docs update, refactor, config), and any notable impact.
3. Draft a concise, imperative commit title summarizing the dominant change. Keep it plain ASCII, <= 80 characters, and avoid trailing punctuation. Prefix with the primary component when obvious (e.g., `Services:`, `Docs:`, `ViewModels:`).
4. Respond with only the final commit title on a single line so it can be pasted directly into `git commit`.

**Best practices:**
- Use imperative mood ("Add feature" not "Added feature").
- Be specific: mention what changed and where.
- Avoid vague words like "fix stuff" or "update code".

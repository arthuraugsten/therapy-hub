@AGENTS.md

## Working with sub-agents (main session)

Keep the main context for planning, decisions, and talking to the user. Delegate the hands-on work:

| Work | Agent |
| --- | --- |
| Production code in `src/`, migrations, `Directory.*.props`, `compose.yaml`, `infra/` | `dotnet-backend-engineer` |
| Tests in `tests/`, running suites, reproducing bugs, end-to-end checks | `qa-engineer` |
| Git, AGENTS.md / CLAUDE.md / agent files, questions to the user | main session |

Default flow for a feature or fix:

1. Main session clarifies the requirement and decisions with the user.
2. `dotnet-backend-engineer` implements it and reports changed files + contract changes.
3. `qa-engineer` gets that report (contract changes, not the diff), adds/updates tests, runs the suites.
4. If QA reports defects, send them back to `dotnet-backend-engineer` with the failing test names; repeat 3.
5. Main session summarizes the result for the user and commits only when asked.

Briefing sub-agents: they start with no memory of this conversation. Give each one a self-contained task: the goal,
the decisions already made, the files or contracts involved, and what to report back. Ask for short reports
(file lists, counts, defects), not diffs or full logs.

Small, single-file edits that need no tests (docs, config tweaks) can stay in the main session.

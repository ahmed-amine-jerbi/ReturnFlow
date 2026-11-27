---
name: dev-pipeline
description: Feature pipeline for software, web and app projects — Investigate → Questions (only if needed) → Plan (topology, scalability, efficiency, requirements matrix) → Review (stop for approval) → Build → QA handoff. Use whenever the user wants something investigated and then implemented across several files or layers — a new feature, an endpoint plus its UI, an integration with an external API, a port from a reference repo / design / spec, a refactor of a whole module — e.g. "plan and build the refunds feature", "investigate this spec and implement it, ask me questions". Also `/dev-pipeline resume <slug>` and `/dev-pipeline status`.
argument-hint: "<what to investigate and build> | resume <slug> | status"
---

# /dev-pipeline

Run the request in `$ARGUMENTS` through five phases, in **auto mode**. Don't stop between phases
except where this file says STOP. The user prefers few interruptions, but sharp questions when they
matter.

```
1 Investigate ─► 2 Questions ─► 3 Plan ─► 4 Review ─► 5 Build ─► QA handoff
   (auto)        STOP only if    (auto)    STOP:        (auto)
                 something       always    approval +
                 must be asked             prerequisites
```

Read each phase file when you enter that phase, not before:
[investigate.md](investigate.md), [questions.md](questions.md), [plan.md](plan.md),
[review.md](review.md), [build.md](build.md).

## Arguments

- `<description>` — start a new pipeline. Derive a short kebab-case slug from the subject
  (`refund-flow`, `orders-csv-export`, `auth-refresh-tokens`). If `docs/pipeline/<slug>/` already
  exists, ask whether to resume it or start fresh. Starting fresh moves the old folder to
  `<slug>-<yyyymmdd>`. Begin by writing `brief.md` and a `state.json` with `phase: "investigate"`,
  using the Write tool.
- `resume <slug>` — read `docs/pipeline/<slug>/state.json` and every record file. Re-enter the saved
  phase (`resumeAt` when it is `paused`), and rewrite `state.json` with a fresh `updated`.
  Re-verify anything in the code that may have changed since `updated` (`git log` since then).
- `status` — print each pipeline's slug, phase and `updated` from `docs/pipeline/*/state.json`.
  Change nothing.

## The record — `docs/pipeline/<slug>/`

The record is the memory of the pipeline. It survives compaction and new sessions, and it's what a
teammate reads to understand why the code looks the way it does. Write it as you go, not at the end.

| File | Written in | Holds |
|---|---|---|
| `brief.md` | start | the user's request verbatim, the slug, every source (spec, ticket, design link, reference repo, API docs) |
| `investigation.md` | 1 | the consolidated findings ([investigate.md](investigate.md) has the outline) |
| `raw/*.txt` | 1 | untouched probe output (route lists, schema dumps, API responses, dependency trees) |
| `decisions.md` | 2 onward | `D1…Dn`: the question, the answer and the source: user / assumed / derived. Assumptions are listed so the user can override them at Review |
| `plan.md` | 3 | the plan ([plan.md](plan.md) has the template, including the requirements matrix) |
| `review.md` | 4 | the reviewer findings and how each was resolved |
| `log.md` | 5 | each build step: files touched, commands run and their results, deviations; a link to the feature doc |
| `state.json` | always | `{"slug","title","phase","waiting","step","resumeAt","updated"}` |

The record explains how the work was decided. The permanent explanation of how the finished
feature works lives in `docs/features/<feature>.md` (the `feature-docs` skill), written at the end
and kept current afterwards.

`state.json` values:
- `phase`: `investigate` | `questions` | `plan` | `review` | `build` | `qa` | `done` | `paused`
- `waiting`: `null` | `"answers"` | `"approval"` | `"prerequisites"`
- `step`: build progress such as `"2/4"`, else `null`
- `resumeAt`: set only when `phase` is `paused`
- `updated`: an ISO timestamp read from the clock (`Get-Date -Format s` / `date -Iseconds`), never estimated

**Update `state.json` at every phase change and every STOP, before you ask.** Status-bar or HUD
tools may read it.

**Write gate.** During `investigate`, `questions`, `plan` and `review`, write only to
`docs/pipeline/**`, the session scratchpad and memory. Source code is read-only until the plan is
approved. Bash stays read-only by discipline in those phases.

## Phase flow

1. **Investigate** (auto). Follow [investigate.md](investigate.md): run the cheap whole-repo probes
   yourself, then launch the `codebase-mapper` agent (one per area when the feature spans
   unrelated areas) in the background, and consolidate `investigation.md` as they return.
   A change of about 5 files or fewer that the record or a quick read already maps takes the
   **light track**: no agent, the short plan, the reviewer still runs.
2. **Questions**. Follow [questions.md](questions.md). Ask only what changes the build and can't be
   settled from evidence. If nothing needs asking, record the assumptions in `decisions.md` and
   continue without stopping. Otherwise set `waiting: "answers"` and STOP with `AskUserQuestion`.
   Post the prerequisites Investigate found (credentials, accounts, env values) now, not at Review.
3. **Plan** (auto). Write `plan.md` from [plan.md](plan.md). The requirements matrix lists every
   requirement and acceptance criterion from the brief and investigation.
4. **Review**. Run the `plan-reviewer` agent on the record ([review.md](review.md)). Fix every
   BLOCKER and MAJOR in `plan.md` and log the resolution in `review.md`. Then set
   `waiting: "approval"` and **STOP**:
   - a plain-English summary in at most 12 lines: what the user of the product will see, what gets
     built, the risky parts, the assumptions taken;
   - the prerequisites only the user can provide;
   - `AskUserQuestion`: Approve and build (Recommended) / Approve, commit after each step / Revise /
     Stop here.
5. **Build** (auto). Only after approval. Follow [build.md](build.md). Stop mid-build only for a
   deviation from the approved plan, a question that changes the build, or an outward-facing or
   destructive action the plan didn't name.
6. **Document and hand off**. Set `phase: "qa"`. Load the `feature-docs` skill and write (or
   update) `docs/features/<feature>.md` from the final code: what it does, a walkthrough, a
   diagram, the code walkthrough by layer, the services, data, contracts and config it uses, and
   how to test and extend it. Update `docs/features/README.md` and the CONTEXT.md features index.
   **A feature isn't done until its doc is.** Then hand over the QA script from `plan.md`, say
   plainly what was and wasn't verified, and set `phase: "done"`. Save or refresh one short
   `project` memory that points to `docs/pipeline/<slug>/` and the feature doc, and add its line
   to `MEMORY.md`.

## Skills — load per phase

Load these with the Skill tool on entering each phase, and again after a compaction. Name them in
every agent brief. CLAUDE.md's skills table maps the subject (language, framework, layer) to skills.

| Phase | Always | Plus, by subject |
|---|---|---|
| Start | `project-context` | |
| 1 Investigate | `fullstack-dev-skills:architecture-designer` | the language / framework skills from CLAUDE.md |
| 2 Questions | (none: [questions.md](questions.md) is the method) | product or domain heavy: `mattpocock-skills:domain-modeling` (terms, glossary, ADRs) |
| 3 Plan | `review-ready-code`, `mattpocock-skills:codebase-design` | API: `fullstack-dev-skills:api-designer`, `postman:api-engineer`; DB: `postgres-pro` / `sql-pro`; UI: `human-interface`, `frontend-design`; MCP: `mcp-server-dev:build-mcp-server` |
| 4 Review | everything Plan loaded, plus `fullstack-dev-skills:security-reviewer` | |
| 5 Build | `review-ready-code`, the subject skills from Plan | tests first where practical: `mattpocock-skills:tdd` |
| QA | `fullstack-dev-skills:test-master`, `feature-docs` | web UI: `browser-automation`; E2E: `fullstack-dev-skills:playwright-expert`; API: `postman:api-testing`, `postman:api-documentation` |

After Build, run `/code-review` (or the `pr-review-toolkit` agents) on the changed files, and fix what
it confirms before QA.

## Standing rules — these override speed

- **CLAUDE.md is law**: its layout, its reference implementation, one owner per responsibility,
  server authority, the naming and style rules.
- **Outward actions need a yes every time**: deploys, pushes, opening PRs, migrations against a
  shared database, sending emails or webhooks, paid API calls, changing cloud resources. A yes from
  an earlier task doesn't carry over.
- **Additive by default.** Never replace or remove an existing feature, endpoint, column or config
  entry the plan didn't name. Breaking an API contract needs a decision `Dn` and a migration path.
- **Requirements are 1:1 by default.** Dropping or bending a requirement needs a decision `Dn`.
  "Close enough" is not a decision.
- **Batch tool calls.** Send independent reads and probes in one message. Save probe output to
  `raw/` and read it once.
- **One pipeline per session.** The record makes `resume` safe in a fresh session.
- **Honesty.** Say "verified" only about checks you actually ran, with the command.
- **Language.** User-facing summaries are plain English and short. The detail lives in the record.

# Claude dev kit

A drop-in set of rules, skills, agents and a feature pipeline for software, web and app projects.

## Install into a project

Copy these two items from `claude-dev-kit/` into the project's root folder (the one with
`package.json`, the `.sln`, or `pyproject.toml`):

- `CLAUDE.md`
- `.claude/`

If the project already has a `CLAUDE.md`, don't overwrite it. Paste the kit's content below the
existing rules instead. If it already has a `.claude/` folder, merge: copy `skills/` and `agents/`
in, and merge `settings.json` by hand (keep both sets of keys).

Then open the project in Claude Code and:
1. Fill in the **This project** block at the top of `CLAUDE.md`, or ask Claude to run the
   `project-context` skill and fill it in for you. It also creates `docs/CONTEXT.md`.
2. If Claude Code asks to install plugins from the project settings, accept. That only happens on
   a machine that doesn't have them yet.

For a multi-part repo (for example web + API + AI service in one folder), install the kit once at
the root. A part that needs different rules can have its own short `CLAUDE.md` in its folder.

## What's inside

| Path | What it does |
|---|---|
| `CLAUDE.md` | The always-on rules: ownership, naming, authority, testing, process, and which skill to load for which work |
| `.claude/skills/dev-pipeline/` | `/dev-pipeline <request>`: Investigate → Questions → Plan → Review (stops for your approval) → Build → QA. Records live in `docs/pipeline/<slug>/` |
| `.claude/skills/project-context/` | Starting or picking up a project: ask first, peer vs autonomous mode, `docs/CONTEXT.md` |
| `.claude/skills/review-ready-code/` | Code a reviewer or interviewer can read cold, and the tells of AI-generated code to remove |
| `.claude/skills/feature-docs/` | Every finished feature gets `docs/features/<feature>.md`: what it does, a walkthrough, a diagram, how the code and services work together, how to test and extend it |
| `.claude/skills/human-interface/` | UI that looks designed rather than generated: design tokens, every UI state, accessibility, browser checks |
| `.claude/agents/codebase-mapper.md` | Read-only agent that maps where a feature plugs in |
| `.claude/agents/plan-reviewer.md` | Read-only agent that reviews a pipeline plan cold |
| `.claude/settings.json` | The plugins this kit expects, so a teammate's Claude Code offers to install them |

## Using the pipeline

```
/dev-pipeline add CSV export to the orders page, filtered by date range
/dev-pipeline status
/dev-pipeline resume orders-csv-export
```

It asks questions only when an answer changes the build, always stops for approval before
writing code, and finishes by documenting the feature in `docs/features/`. Features built
outside the pipeline are documented the same way (CLAUDE.md requires it). Add `docs/pipeline/` to `.gitignore` if you don't want the records in the repo.
Committing them gives reviewers the reasoning behind a change.

## Updating the kit

Edit the files in `claude-dev-kit/`, then copy them into projects again. Projects keep their own
filled-in `CLAUDE.md` "This project" block, so copy only the sections below it.

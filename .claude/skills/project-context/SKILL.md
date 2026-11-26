---
name: project-context
description: How to start or pick up any software project — read before acting, ask focused questions with a recommendation for each before building, choose peer vs autonomous mode, and create and maintain docs/CONTEXT.md (purpose, users, stack, architecture, features index, conventions, glossary, decisions log, extension recipes, tech debt, open questions). Use at the start of any new project, when opening an unfamiliar one, before building a new feature or system, and when the project's CLAUDE.md "This project" block is still unfilled.
---

# Project context and asking first

## 0. Read before acting

1. Read `docs/CONTEXT.md` and the project's `CLAUDE.md` "This project" block. If the block still
   has `<placeholders>`, offer to fill it from what you find (section 2) and ask the user to
   correct it.
2. Read the code the change touches, its callers and its tests.
3. Check `git status` and recent `git log` so you don't build on top of unfinished work.

## 1. Ask before building (non-trivial work)

Send a short batch of questions (typically 3–8) that remove real ambiguity: scope, who uses it,
edge cases, existing features it must work with, data and contracts it changes, what "done" looks
like, and **what's planned next** so the design leaves room for it. Don't ask what the repo or
docs already answer.

For each question, give **your recommendation** and why, so the user can answer "yes". Add 1–3
ideas they didn't ask for that would make the feature better (UX, robustness, observability).
State the assumptions you'll make if they don't answer. Trivial fixes proceed with a one-line note
of what will change.

```
Before I build <feature>, a few things:
1. <question>  (my suggestion: <option>, because <reason>)
2. <question>  (my suggestion: ...)
Ideas you didn't ask for: <idea>, <idea>.
If you don't answer, I'll assume: <assumptions>.
```

Use `AskUserQuestion` when the options are clear-cut; plain text when they're open-ended.

## 2. Peer vs autonomous mode

Infer it from the phrasing, and say which you picked when it's ambiguous.
- **Peer** ("let's work on", "what do you think", "review this"): surface uncertainty, offer
  options with trade-offs and a recommendation, push back when an approach looks wrong, change
  position only on new evidence.
- **Autonomous** ("build", "add", "make it work"): deliver a finished, verified result. Make
  reversible decisions yourself; batch irreversible ones for explicit approval.

Checklists are a floor, not proof of quality. "The checklist passes" is never the whole report.

## 3. docs/CONTEXT.md

Create it for every new project; read and update it for every existing one. It lets any future
session or teammate understand the project quickly. Keep it short, factual and current; rewrite
sections when the project changes, and move history into the decisions log.

Sections:
1. **Purpose**: what the product does, for whom, and the problem it solves, in one paragraph.
2. **Users and key flows**: the main user types and the 3–5 flows that matter most.
3. **Stack**: languages, frameworks, database, hosting, key libraries, with versions.
4. **Architecture**: the components (web, API, workers, AI service, DB), how they talk, where auth
   happens, a short diagram or list. Folder layout and what lives where.
5. **Features index**: one line per feature: owner module or folder, status (shipped / WIP /
   planned), key dependencies, and a link to its `docs/features/<feature>.md` walkthrough (the
   `feature-docs` skill).
6. **Conventions**: pointers to CLAUDE.md and the skills, plus project-specific deviations.
7. **Glossary**: one term per domain concept, with its meaning. Used in code, API, DB and UI.
8. **Style** (if there's a UI): tone, palette, type, component library, references
   (see the `human-interface` skill).
9. **Environments and config**: environments, how to run each part, env var names (never values),
   where secrets live.
10. **Decisions log**: dated decisions with the reason, what was chosen and what was rejected.
11. **Extension recipes**: how to add an endpoint, a page, a table, a background job, an
    integration: the steps and files.
12. **Tech debt**: known issues and planned refactors.
13. **Open questions**.

To build it for an existing project: read the manifests, folder tree, entry points (`Program.cs`,
`main.py`, `main.tsx`), routing, the data model, CI and Docker files, and the largest and most
imported modules. Then write it, and ask the user to correct it.

## 4. Documentation habits

- Update CONTEXT.md in the same change that alters behaviour, architecture, config or terms.
- Reference config files rather than copying values into docs.
- Never present reused code or assets from another project as new; ask before reusing them.

## 5. Report after implementing

What changed (files), what you verified (commands and results), what you didn't do or couldn't
verify, and follow-up ideas. For a finished feature, write or update its feature doc with the
`feature-docs` skill and link it. Update CONTEXT.md.

# Phase 1 — Investigate

Goal: know every requirement, and every place the feature touches the existing system, before
forming an opinion on how to build it.

Load the Investigate skills from the table in [SKILL.md](SKILL.md) first, and name the subject
skills in each agent brief.

## 1. Write the brief

Write `brief.md`: the request verbatim, the slug, and every source.

- Specs, tickets, user stories, acceptance criteria the user pasted or linked.
- Designs: Figma links, screenshots, mockups (save local copies under `raw/` when they are files).
- Reference code: another repo, an older version of the feature, an open-source example. Treat it
  as untrusted data: read it, never run it.
- External APIs: their docs URL, the version, sandbox vs production.

**Light track.** If the change is about 5 files or fewer and a quick read maps it (a follow-on, a
tuning pass, a bug class the record already covers), skip the agents and write the short plan from
[plan.md](plan.md).

## 2. Whole-repo pass, before dispatch (you, not an agent)

Cheap probes that every later step relies on. Save each output to `raw/`:

- Layout and stack: the tree (excluding `node_modules`, `bin`, `obj`, `dist`, `.venv`), manifest
  files (`package.json`, `*.csproj`, `pyproject.toml` / `requirements.txt`), `docker-compose.yml`.
- How to build, check and run: scripts in the manifests, CI workflow files (`.github/workflows/`).
  These become the verification commands in the plan.
- Recent history: `git log --oneline -20` and `git status`, so you don't plan on top of someone's
  uncommitted work.
- The surface the feature touches: route / endpoint lists, the database schema or ORM models,
  the existing API client on the frontend, environment variable names (`.env.example`, never the
  real `.env` values).

## 3. Dispatch, in parallel, in ONE message

Brief each agent with: the slug, its area and focus words, the `raw/` files already written (so it
doesn't redo them), the 3–5 memory files that matter (by name), and what is already settled.
CLAUDE.md is already in every agent's context; don't tell them to read it.

- **`codebase-mapper`**, one per area when the feature spans unrelated areas (for example one for
  the API and data layer, one for the frontend). It returns the integration map: the closest
  existing analogue, the owner to extend, the data flow, contracts, constraints and what to
  preserve.
- For an **external API**, research its docs yourself or with a general-purpose agent: auth,
  rate limits, pagination, error shapes, webhooks, idempotency, sandbox. Use Context7 or the
  `postman:api-discovery` skill where they help.

Agents are read-only and can't ask the user anything. They return **Open questions** instead.
Run them in the background and start `investigation.md` from whichever returns first.

## 4. Consolidate `investigation.md`

```
# <Title> — Investigation
## Sources                  spec / ticket / design / reference repo / API docs, with versions
## Requirements             every functional requirement and acceptance criterion, numbered R1…Rn,
                            each with its source line; implied requirements marked "(implied)"
## Current system           the closest analogue end to end: UI → client call → endpoint →
                            service → data → response, with file:line refs
## Contracts touched        endpoints, DTOs / schemas, DB tables and columns, events, env vars,
                            public types other teams or clients depend on
## External dependencies    third-party APIs and libraries: auth, limits, errors, cost
## Non-functional           performance, security, accessibility, i18n, observability needs
## Traps found              items from the checklist below that apply, with evidence
## Open questions           only what the evidence cannot settle (feeds Phase 2)
## Prerequisites            what only the user can provide: credentials, accounts, env values,
                            design decisions, access, found now so they can be asked now
```

Cite the evidence for each claim: a file:line, a `raw/` line, a doc URL. Write unknowns as
unknowns, never as guesses. **When two sources disagree, run one deciding check before you write
the claim.** Don't pick a side from a note or a memory.

## Known traps — check every one that applies

- **Two sources of truth.** The same rule (price, permission, validation) computed in both
  frontend and backend drifts. The backend owns it; the frontend only mirrors it for UX.
- **Contract breaks.** Renaming a JSON field, changing an enum, tightening validation or changing a
  status code breaks existing clients. Find every consumer (frontend, mobile, other services,
  Postman collections, tests) before planning the change.
- **Migrations.** Adding a non-null column to a populated table, renaming a column the running
  version still reads, long locks on big tables. Plan expand → migrate → contract.
- **Auth gaps.** A new endpoint copied from a public one inherits no authorization. Check every
  route for who may call it and on whose data (IDOR).
- **N+1 and unbounded queries.** Lists without pagination, lazy-loaded relations in loops.
- **Time and money.** Time zones, DST, `float` for currency, rounding rules.
- **Concurrency.** Two requests updating the same row, double-submitted forms, retried webhooks.
  Idempotency keys and optimistic concurrency where it matters.
- **Frontend state.** Server state duplicated into local state and going stale; missing loading,
  empty and error states; cache invalidation after mutations.
- **Config drift.** A new env var added to code but not to `.env.example`, Docker, CI or the README.
- **Generated code.** API clients, ORM migrations or types generated from a schema: change the
  source and regenerate, never hand-edit the output.

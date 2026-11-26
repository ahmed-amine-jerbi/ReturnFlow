# Phase 3 — Plan

Write `docs/pipeline/<slug>/plan.md` in this shape. Every section is required. A section that
doesn't apply says why in one line.

Load the Plan skills from the table in [SKILL.md](SKILL.md) before writing. A plan that breaks a
loaded skill says why in a decision `Dn`.

Write bullets, not prose. Cite `investigation.md` and `raw/` lines instead of restating them. After
review, edit the lines a finding touches; never rewrite the whole plan.

**Light track**: the plan is only Summary, Decisions, Files (new / modified, with the functions
touched), Requirements rows for what changes, Build steps with their verification, QA script,
Rollback and Documentation (often an update to an existing feature doc). The plan-reviewer still
runs.

```
# <Title> — Plan

## Summary
3–5 plain-English lines: what the product's users see, when, who can use it, and what changes for
existing behaviour.

## Decisions
The D-numbers this plan rests on (link decisions.md). Assumptions are marked "(assumed)".

## Topology
- Layers and authority
    Client   = presentation and intent: UI, local UI state, input validation for UX only
    Server   = authority: authentication, authorization, validation, business rules, state changes
    Data     = schema, constraints, indexes, migrations
    External = third-party calls, behind one adapter the rest of the code calls
    Config   = tunables in the project's config; secrets in env / secret store; new keys listed
- Data flow: UI action → client call (existing API client) → endpoint → service / handler → data
  access → response shape → UI state update / cache invalidation
- Owner: the existing module that owns this responsibility. Name the closest analogue and why this
  follows it. Extending the owner is the default; a new file needs the CLAUDE.md "Before creating a
  file" answers.
- Contracts: every endpoint, DTO, schema, event or public type added or changed, with its exact
  shape. Changes are additive unless a decision says otherwise.
- Files
    New:      path · layer · what imports it · why no existing file can hold it
    Modified: path · the exact functions / types / tables touched · additive or replacing (replacing needs a decision)

## Scalability
- The next feature of this kind costs: <one new handler / component / config entry>. Where it goes,
  and what does NOT change.
- What is generic (shared component, base handler, existing service) versus specific to this feature.
- Growth: data volume, request rate, concurrent users; where it breaks first.

## Efficiency
- Queries: count per request, indexes used or added, pagination, no N+1.
- Network: request count per screen, payload size, caching and invalidation.
- Frontend: re-render scope, bundle impact of new dependencies, lazy loading.
- Background work: what runs synchronously vs in a job, and why.

## Requirements matrix
One row for every requirement and acceptance criterion in investigation.md (R1…Rn).

| R# | Requirement (source) | Implementation (file → function / component) | Test (file → test name, or QA step) | Deviation (Dn) |
|---|---|---|---|---|

Not one row may be silently dropped. Anything not built carries a decision.

## Security
Who can call each new endpoint and on whose data; input validation; output encoding; secrets;
logging of sensitive data; rate limiting where abuse is plausible.

## Failure modes
Network failure and timeouts, external API errors and rate limits, concurrent edits, retries and
double submits, partial failures mid-operation (what is rolled back), invalid or hostile input,
large data, slow devices, expired sessions.

## Build steps
Each step touches 5 files or fewer and ends with its own verification. Order them so the app still
builds and runs after every step (data and contracts first, then server, then client).
1. …  — verify: <exact commands, and what to look for>

## Prerequisites — user actions
What only the user can provide, with exact instructions, and whether it was already asked at the
end of Investigate. Anything Claude can do itself is a build step, not a prerequisite.

## QA script
What the user does, step by step, and what to look for, keyed to requirement rows. Include the
config keys that tune behaviour and the file each lives in. Name every other feature the change
touches and how it is tested.

## Rollback
How to revert each step: git revert of which commits, down-migrations, feature flag off, config
values to restore.

## Documentation
The feature doc to write or update (`docs/features/<feature>.md`, per the `feature-docs` skill),
the flows its walkthrough and diagram will cover, and any other docs that change: README run
steps, `.env.example`, OpenAPI / Postman collection, CONTEXT.md sections.
```

Write the plan for a reader who wasn't in the conversation: the plan-reviewer agent, a teammate,
and a future resumed session. Use file paths, symbol names and requirement numbers, never "the
thing we discussed".

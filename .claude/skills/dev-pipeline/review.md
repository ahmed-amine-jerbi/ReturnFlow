# Phase 4 — Review

## Run it

Launch the `plan-reviewer` agent with the record folder path and the list of skills the subject
needs (from the table in [SKILL.md](SKILL.md)), and nothing else. It must judge the record cold.
That fresh context is the point: it catches what the planner was primed to miss.

Then, for each finding:

- **BLOCKER / MAJOR**: edit the affected lines of `plan.md` in place. If the fix needs a user
  decision, it joins the stop below.
- **MINOR / NIT**: fix it if cheap, otherwise note it.
- **Disputed**: write why in `review.md`, with evidence. Don't drop a finding silently.

Write `review.md`: each finding, its severity, and its resolution (fixed / accepted / disputed plus
why).

## Rubric (the reviewer applies this; pass it yourself before the reviewer runs)

**A. Requirements**
- Every requirement and acceptance criterion from the investigation has a row.
- Every row has an implementation and a test or QA step.
- Every deviation has a decision.

**B. CLAUDE.md**
- Follows the project layout and the reference implementation's shape.
- One owner per responsibility: no new single-purpose wrapper, service or hook when an existing
  owner can hold the code.
- Names, one term per concept, no magic values, secrets in env, comments only for why.
- Generated files are regenerated, not hand-edited.

**C. Topology and authority**
- Each piece is in the right layer. Business rules and authorization live on the server.
- Extends the closest existing analogue instead of building a parallel system.
- External services sit behind one adapter.

**D. Contracts**
- Every changed endpoint, DTO, schema and public type is listed with its shape.
- Changes are additive, or versioned with a migration path and a decision.
- Every consumer of a changed contract is found and updated or unaffected.

**E. Scalability and efficiency**
- The next feature of this kind costs only its specific files.
- No N+1 queries, unbounded lists or per-render network calls. Indexes exist for new filters.
- New dependencies are justified.

**F. Security and failure modes**
- Authorization on every new route, on the right data.
- Input validated at the boundary, output encoded, secrets not logged.
- Failure modes have a defined behaviour; partial failures roll back or are retried safely.

**G. Safety**
- Additive to existing features unless a decision says otherwise.
- Migrations are reversible or the plan says why not.
- Rollback is written down. Outward-facing actions are named and will be asked for.

**H. Testability**
- Each build step has concrete verification commands.
- The QA script covers every requirement row.
- Every feature the change interacts with has a test step.
- The plan states what can't be verified locally.

**I. Documentation**
- The plan names the feature doc to write or update, and the flows it will walk through.
- Every other doc the change affects is listed: README, `.env.example`, API spec, CONTEXT.md.

## STOP — approval

Set `state.json` to `phase: "review", waiting: "approval"`. Then post:

1. A plain-English summary in at most 12 lines: what users see, what gets built (the number of new
   and changed files), the risky bits, and the assumptions (`Dn (assumed)`) the user may want to
   override.
2. The prerequisites still outstanding, if any.
3. `AskUserQuestion` with one question: *Approve and build (Recommended)* / *Approve, commit after
   each step* / *Revise* (they say what) / *Stop here* (`phase: "paused", resumeAt: "review"`).

A message `Approved: build it.` or `Revise the plan: …` from a HUD button counts as the matching
answer.

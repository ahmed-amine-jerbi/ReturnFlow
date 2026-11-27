# Phase 2 — Questions

The rule: *ask smart questions only when you need to know. This is not a quota.* Zero questions is
a valid outcome.

## Ask only if all three hold

1. **It changes the build.** It changes what gets built, how, or what the product's users experience.
2. **It can't be settled from evidence**: the spec, the codebase, CLAUDE.md, CONTEXT.md, memory, an
   existing analogue's convention, or an earlier decision in `decisions.md`.
3. **A wrong guess is costly.** It would be visible to users, hard to undo, or touch security, data,
   money or a public contract.

Anything that fails the test becomes an **assumption**. Write it as `Dn (assumed): …, because …` in
`decisions.md` and surface it in the Review summary, where the user can override it at no cost. A
good default with a stated reason beats a question.

## How to ask

- Use `AskUserQuestion` with up to 4 questions per call and 2 to 4 options each. Put the recommended
  option first and label it `(Recommended)`. Each option's description states the consequence, not
  a restatement of the label.
- **Lead with the evidence** in the question text, e.g. "The spec says refunds within 30 days, but
  `Order` has no delivered date, only `CreatedAt`. Count from which?"
- Use the `preview` field when options differ structurally: an API shape, a table schema, a UI
  layout, a code split. Keep previews short.
- Use `multiSelect` for "which of these to include".
- Ask the questions that block the most first. If one answer changes the next question, ask the
  first, wait, then ask the next round. Never pad a round.
- Before asking, set `state.json` `waiting: "answers"`. Afterwards, record every answer as `Dn (user)`.
- The user often answers in "Other" with free text that partly overrides the options. Read it
  literally and do what it says. If it's still ambiguous, ask one follow-up and only one.

## Not questions

- **Prerequisites** the user must provide (API keys, an account, access to a repo, a design file).
  Post them **as soon as Investigate finds them**: with the questions if this phase stops, or as a
  short non-blocking note if it doesn't, so the user handles them during plan and review. Set
  `waiting: "prerequisites"` only when nothing else is pending.
- **Pure engineering choices** that CLAUDE.md or an existing analogue already settles. Follow the
  analogue.

## Question bank — areas worth probing

Pull from this bank only where the evidence leaves a gap.

| Area | Ask when | Typical options |
|---|---|---|
| Scope | the request could mean a small or a large feature | MVP now / full spec / MVP plus named follow-ups |
| Users and permissions | it's unclear who may see or do it | everyone signed in / owner only / role X / admin only |
| Data | new data must be stored or an existing shape changes | new table / column on X / derived, not stored; retention and deletion |
| Contract | an existing API or schema changes | additive (new field / endpoint) / versioned / breaking with migration |
| Edge states | empty, failure or partial states aren't specified | the behaviour for each, shown as options |
| External service | a third-party call can fail or be slow | retry + queue / fail visibly / degrade gracefully |
| Consistency | two users or retries can collide | last write wins / optimistic concurrency / idempotency key |
| UX | the design is silent about layout, copy or flow | follow the closest existing screen / new pattern (preview) |
| Performance | lists or jobs can grow large | paginate / stream / background job |
| Rollout | it replaces something live | replace / behind a feature flag / new route alongside |
| Testing depth | the risk profile is unclear | unit only / unit + integration / + E2E for the happy path |

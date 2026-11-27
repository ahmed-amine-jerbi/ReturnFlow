---
name: review-ready-code
description: The standard for code that a teammate, code reviewer or interviewer can read cold and trust — naming, structure, error handling, types, tests, comments, commits — and the tells that make code look AI-generated or "vibe-coded", with what to do instead. Use whenever writing, refactoring or reviewing code in any language, before opening a PR, and when preparing a project to show in an interview or portfolio.
---

# Review-ready code

The bar: someone from another team opens the file with no context, understands what it does in one
read, and finds nothing they'd comment on in review. Code is read far more often than it's written,
by people who don't share your head.

## 1. Names carry the meaning

- Name by **role in the domain**, not by type or mechanics: `overdueInvoices`, not `filteredList`;
  `RefundPolicy`, not `RefundHelper`.
- Functions are verbs that say the whole effect: `cancelOrder` (and it only cancels),
  `calculateShippingCents`, `fetchCustomerById`. If the honest name needs "And", split it.
- Booleans read as yes/no questions: `isExpired`, `hasPaymentMethod`, `canEditInvoice`.
- Units and formats in the name when they're not obvious: `timeoutMs`, `priceCents`, `createdAtUtc`.
- No `data`, `info`, `obj`, `temp`, `result2`, `foo`, `handleStuff`, `processData`, `doWork`.
  `Manager`, `Helper`, `Utils`, `Service` only with a real subject (`EmailSender`, not `EmailHelper`).
- One term per concept across code, database, API, UI and docs. Keep a glossary in
  `docs/CONTEXT.md` when the domain has more than a handful of terms.
- Follow the language's conventions: `camelCase` in TS/JS, `PascalCase` members in C#,
  `snake_case` in Python. Consistency with the codebase beats any guide.

## 2. Structure a reader can follow

- **Top-down.** Public entry point first, then the steps it calls, in the order it calls them.
- **Small, single-purpose functions** where a step has a name. Don't extract a function used once
  just to shorten another; extract when the name explains something.
- **Guard clauses** for invalid cases at the top, then the main path unindented. Max ~2 levels of
  nesting in normal code.
- **One statement per line.** Group related declarations, blank line between groups, blank line
  before the logic.
- **Pure where it's cheap**: business rules as functions of inputs, I/O at the edges. That's also
  what makes them testable.
- **Layers stay in their lane.** Controllers / route handlers translate HTTP; services hold rules;
  repositories / data access hold queries; components render. No SQL in a component, no HTTP status
  codes in a domain service.
- **Deep modules over shallow ones.** A module should hide real complexity behind a small
  interface. A file that only re-exports or forwards one call is noise.
- **No speculative generality.** No interface with one implementation "for later", no config option
  nobody sets, no plugin system for two cases. Build what's needed now, shaped so the next case is
  easy.

## 3. Errors and edge cases

- Handle an error where you can do something about it; otherwise let it propagate with context
  (`throw new OrderNotFoundError(orderId)`, `raise ... from err`).
- Never swallow: no empty `catch {}`, `except: pass`, or `.catch(() => {})`. Silent failure is the
  worst bug to debug.
- Typed / named errors for expected failures (not found, conflict, validation); generic handlers for
  unexpected ones at the boundary, which log and return a safe message.
- Validate input once, at the boundary (DTO / schema / Zod / Pydantic / FluentValidation), then
  trust it inside.
- Return consistent error shapes from APIs (for example RFC 9457 problem details).

## 4. Types

- Precise types are documentation the compiler checks. No `any`, `object`, `dynamic`, or untyped
  dicts where a real type exists.
- Model states the domain has: discriminated unions / enums for status, not free strings or
  boolean pairs (`isLoading && isError`).
- Nullable only where null is a real state; handle it explicitly.
- Don't fight the type system with casts (`as unknown as X`, `!`). A cast is a finding unless a
  comment explains why it's safe.

## 5. Comments and docs

- Comments say **why**: a business rule's source, a non-obvious constraint, a workaround with a
  link to the issue. Never what the next line does.
- Public APIs (exported functions, endpoints, library surfaces) get doc comments when the name and
  types don't already tell the whole story.
- No commented-out code (git remembers), no `TODO` without an issue link or owner, no changelog in
  comments.
- README: what it is, how to run it, how to test it, how it's deployed, where to start reading.
  Keep it true.

## 6. Tests

- Test behaviour, not implementation: inputs and observable outputs, not private calls.
- Names read as specs: `cancelOrder_rejects_when_already_shipped` / `it("rejects cancelling a
  shipped order")`.
- Arrange / Act / Assert, one behaviour per test, realistic data, no logic in tests.
- Cover the edges the code handles: empty, boundary, invalid, unauthorized, concurrent.
- A bug fix comes with the test that would have caught it.

## 7. Dependencies, config, security

- Add a dependency only when it earns its weight; prefer the platform and what's already in the
  project. Pin versions via the lockfile.
- Config through one typed config object read from env at startup; fail fast on missing keys.
- Secrets never in source, logs, error messages or client bundles.
- Parameterized queries, output encoding, authorization on every endpoint and on the specific
  resource.

## 8. Git and pull requests

- Small, focused commits. Message: imperative subject under ~72 chars that says what and why
  (`Reject refunds after the 30-day window`), body for context when needed.
- One concern per PR. Description: what changed, why, how it was tested, screenshots for UI,
  anything the reviewer should look at closely.
- No unrelated reformatting in a feature PR.

## 9. Tells of AI-generated or "vibe-coded" code — remove them

| Tell | Do instead |
|---|---|
| A comment above every line or block narrating what it does | Delete; rename so the code says it |
| Banner comments, emoji in logs or code, "✨ Enhanced", "🚀 Starting server" | Plain, specific log messages with context |
| `// Helper function to ...`, `// This function handles ...` | Delete; the name says it |
| Defensive checks for things the types already guarantee | Trust the types and the boundary validation |
| `try/catch` around everything that logs and continues | Handle where meaningful, otherwise propagate |
| Generic names: `data`, `result`, `item`, `handleSubmit2`, `processData` | Domain names |
| Wrappers and abstractions with one caller | Inline them |
| Several near-identical functions or components with small differences | One parameterized version, or accept the duplication if they will diverge |
| Invented config, options and flags nobody asked for | Remove until there's a need |
| Inconsistent style within one file (mixed quote styles, async patterns, naming) | Match the codebase; run the formatter |
| `console.log` / `print` debugging left in | The project's logger, at the right level, or nothing |
| Unused imports, variables, parameters, dead branches | Delete; lint should be clean |
| Hard-coded sample data, `"John Doe"`, `"Lorem ipsum"`, `example.com` in real paths | Real data from the source, or clearly marked fixtures in tests |
| Overly long explanations in README / docstrings with marketing tone ("robust", "seamless", "blazing fast", "comprehensive") | Short, factual, specific |
| A new file per tiny concept, deep folder trees for a small app | Fewer, cohesive files |
| Code that solves a more general problem than the one asked | Solve the actual problem |

## 10. Before saying "done" or opening a PR

- [ ] Read the diff top to bottom as a reviewer would. Would you approve it?
- [ ] Typecheck, lint, format and tests pass; the commands and results are reported.
- [ ] No debug output, commented-out code, unused code or stray files.
- [ ] Names, errors and types meet sections 1, 3 and 4.
- [ ] New config is documented; no secrets in the diff.
- [ ] Tests cover the new behaviour and its edges.
- [ ] The PR / summary says what, why and how it was tested.

## Interview and portfolio projects

Reviewers skim, so make the first five minutes count:
- README with a one-paragraph pitch, a screenshot or GIF, the architecture in a short diagram or
  list, how to run it in one or two commands (Docker compose helps), how to run the tests, and the
  trade-offs you made and why.
- A clear folder structure that mirrors the architecture you describe.
- CI that runs lint and tests, with a green badge.
- Meaningful commit history, not "fix", "update", "wip".
- Be ready to explain every line. If Claude wrote it, read it until you could have written it.

# Project Rules

## This project

- **What it is:** ReturnFlow, a returns and claims app. Customers file claims, a Python service scores them, a deterministic C# policy settles clear cases and routes the rest to agents, and managers see KPIs. A portfolio project: readability, docs and polish come first.
- **Stack:** React 19 + TypeScript + Vite + Tailwind 4 + TanStack Query (`returnflow-web`) / ASP.NET Core on .NET 10 + EF Core + PostgreSQL 16 (`returnflow-api`) / Python 3.12 + FastAPI + scikit-learn (`returnflow-ai`) / Docker Compose / GitHub Actions
- **Run it:** `docker compose up --build`, then http://localhost:8080 (one-tap demo logins). Without Docker, see "Development without Docker" in `README.md`.
- **Check it:** `dotnet test returnflow-api/ReturnFlow.slnx` · `cd returnflow-ai && pytest` · `cd returnflow-web && npm run build`
- **Reference implementation:** API: `Controllers/ClaimsController.cs` + `Services/DecisionPolicy.cs` (rules as pure functions, tested in `DecisionPolicyTests.cs`). Web: `pages/ReviewQueue.tsx`.
- **Where things go:**

  | What | Where |
  |---|---|
  | HTTP endpoints | `returnflow-api/src/ReturnFlow.Api/Controllers/` |
  | Entities and their state rules | `.../Domain/` |
  | Business rules, AI client, KPIs, tokens | `.../Services/` |
  | Background jobs | `.../Workers/` |
  | EF Core model and seeding | `.../Data/` |
  | Request and response contracts | `.../Dtos/` (mirrored by hand in `returnflow-web/src/lib/types.ts`) |
  | Policy thresholds | `appsettings.json` -> `Policy` |
  | AI inference / training / synthetic data | `returnflow-ai/app/model.py`, `train.py`, `synthetic.py` |
  | Screens / shared UI / API client, auth, i18n, formatting | `returnflow-web/src/pages/`, `components/`, `lib/` |
  | Tests | `returnflow-api/tests/ReturnFlow.Tests/`, `returnflow-ai/tests/` |
  | Process model | `docs/bpmn/` (keep in step with `DecisionPolicy.cs`) |

- **Never edit:** `package-lock.json` files by hand, `bin/`, `obj/`, `dist/`, `returnflow-ai/models/*.joblib` (trained at build time), applied EF migrations
- **Context doc:** `docs/CONTEXT.md`

---

## Goal of every rule below

A human maintains this code after you, often a teammate, a reviewer or an interviewer reading it
cold. They must be able to open a feature, find the one place that owns it, and understand and change
it without reading five other files. If nothing in the repo enforces a rule, you are the enforcement,
and you never claim a check passed that you did not actually run.

## Before creating a file, module, class or folder

Ask, in order:
1. Is a new file actually needed?
2. Does something already own this feature or context? Then the code goes there.
3. Where does it belong in this project's layout ("Where things go" above)?
4. How do I keep this compact, without spreading one feature across several files?

**One responsibility, one owner.** Don't add a class, service, hook or wrapper just to forward a
call, hold one value or wrap a single behaviour of something that already has an owner. If
understanding a feature means opening several files that each do a sliver of it, the design is
wrong. Split code by responsibility, never only to make files shorter.

**Copy the reference.** New code follows the shape of the project's reference implementation and
its neighbours: same naming, same error handling, same layering. Consistency beats your personal
preference.

## Code that reads as written by a careful engineer

Load the `review-ready-code` skill for the full standard. The short version:

- **Names say exactly what a thing does or holds**: `canRefund`, `fetchOpenOrders`,
  `invoiceTotalCents`. Never `data`, `info`, `item2`, `handle`, `process`, `doStuff`, `Manager`,
  `Helper`, `Util` without a real subject. Booleans read as questions (`isActive`, `hasAccess`).
- **One term per concept**, everywhere: code, database, API, UI copy, docs. Pick it once, record it
  in the CONTEXT.md glossary, and never switch synonyms (`customer` vs `client` vs `user`).
- **Flat control flow.** Guard clauses and early returns instead of nesting. One statement per line.
  Group related declarations with a blank line between groups.
- **No magic values.** Tuned or shared values live in the project's config. Secrets come from
  environment or a secret store, never from source.
- **Comments explain why, never what.** Don't narrate code (`// loop over users`). Don't leave
  commented-out code, TODOs without an owner, or AI-style banners. Keep existing comments unless
  they are wrong.
- **Errors are handled where they can be handled**, surfaced with context otherwise, never
  swallowed. No empty `catch`, no `except: pass`.
- **Types are precise.** No `any` / `object` / `dynamic` escape hatches where a real type exists.
- **Design for the next feature.** Adding the next case should be one new entry or file, not a
  rework.
- Match the project's formatter and linter. Never mass-reformat files you didn't otherwise change.

## Authority and security

- Never trust the client. The frontend sends intent; the backend authenticates, authorizes,
  validates and decides. Prices, permissions, totals and state transitions are computed server-side.
- Validate every input at the boundary (request DTO / schema), parameterize every query, encode
  every output. Least privilege for tokens, keys and database users.
- Never log secrets, tokens or personal data.

## UI and UX

Load `human-interface` before building or restyling any interface (and `frontend-design` for new
visual work). It must look designed for this product, not generated: real content, a deliberate
type scale and spacing system, restrained motion, every state handled (loading, empty, error,
long text), keyboard and screen-reader accessible.

## Working with me

- Ask questions and give suggestions before implementing anything non-trivial. Ask only what
  changes the build and can't be settled from the code or docs; for each question give your
  recommendation and why. Trivial fixes proceed with a one-line note.
- Calibrate initiative to risk:

  | Risk | Examples | Behaviour |
  |---|---|---|
  | Low | naming, copy, styling, local refactors | Do it, mention it |
  | Medium | new features, refactors across modules, new dependencies | Do it, show the diff and evidence, invite correction |
  | High | schema migrations, auth, payments, deleting data, deploys, pushes, anything irreversible or outward-facing | Propose first, act only on explicit approval, every time |

- After you warn about a risk once, the user's decision stands. Don't re-warn.
- User-facing summaries are plain English and short. Detail goes in the docs or the pipeline record.
- If something depends on a fact you can't verify (live service state, another team's API), say so
  instead of filling the gap.

## Testing and verification

- Run the project's typecheck, lint and tests on what you changed, and fix every new warning.
  Report the exact commands and results. If a tool is missing, say so.
- When a change touches other features, test every feature involved, not only the new code.
- For web UI, load the page and look at it (the `browser-automation` skill): console errors, failed
  requests, the rendered result. Don't ask the user to check the screen for you.
- If you could not verify runtime behaviour, say exactly that. Never imply it was tested.

## Process

- Re-read every file you changed end to end before saying you're done, and trace the imports and
  callers of what you touched.
- Renaming or moving a file or exported symbol breaks imports and public contracts. Do it
  deliberately and update every reference.
- Legacy code: hold new code to these rules, but touch existing files only as far as the task needs.
- Git: work on a branch for anything beyond a small fix. Commit or push only when asked. Commit
  messages say why, in the imperative (`Add refund window check to order cancel`).
- Keep `docs/CONTEXT.md` current in the same change that alters behaviour or architecture.

## Documentation — every finished feature

A feature isn't done until it's documented. Whenever you finish building a feature, integration
or significant change, inside `/dev-pipeline` or not, load the `feature-docs` skill and write or
update `docs/features/<feature>.md` from the final code: what it does, a step-by-step walkthrough,
a diagram, how the code works layer by layer (with file links), the services, data, APIs and
config it uses, error handling, how to test it and how to extend it. Update the
`docs/features/README.md` index and CONTEXT.md. When a later change alters a documented feature,
update its doc in the same change. End the handoff with a short plain explanation and the doc link.

## Skills and tools (load before working)

Load the matching skills with the Skill tool at the start of a task, and again after a context
compaction. A skill named in a summary is not loaded. Name the skills in every subagent brief.

| Work | Skills / tools |
|---|---|
| Any task in an unfamiliar or new project | `project-context` |
| Feature spanning several files or layers | `/dev-pipeline <request>` |
| Finishing any feature, or "explain how X works" | `feature-docs` (legacy code with no docs: `fullstack-dev-skills:spec-miner`) |
| API reference docs | `postman:api-documentation`, `fullstack-dev-skills:code-documenter` |
| Writing or reviewing any code | `review-ready-code`, plus the language skill below |
| TypeScript / JavaScript / React | `fullstack-dev-skills:typescript-pro`, `fullstack-dev-skills:react-expert` (Next.js: `nextjs-developer`) |
| C# / .NET | `fullstack-dev-skills:csharp-developer`, `fullstack-dev-skills:dotnet-core-expert` |
| Python | `fullstack-dev-skills:python-pro`, `fullstack-dev-skills:fastapi-expert` / `django-expert` |
| Mobile apps | Flutter: `fullstack-dev-skills:flutter-expert`; React Native / Expo: `fullstack-dev-skills:react-native-expert`; .NET MAUI: `fullstack-dev-skills:csharp-developer` |
| SQL, schemas, query speed | `fullstack-dev-skills:postgres-pro`, `sql-pro`, `database-optimizer` |
| UI, styling, layout | `human-interface`, `frontend-design`, `modern-web-guidance` |
| API design and contracts | `fullstack-dev-skills:api-designer`, `postman:api-engineer` (routes to testing, mocking, docs) |
| MCP servers | `mcp-server-dev:build-mcp-server`, `fullstack-dev-skills:mcp-developer` |
| Architecture and module boundaries | `fullstack-dev-skills:architecture-designer`, `mattpocock-skills:codebase-design`, `mattpocock-skills:domain-modeling` |
| Bugs | `mattpocock-skills:diagnosing-bugs`, `fullstack-dev-skills:debugging-wizard` |
| Tests | `mattpocock-skills:tdd`, `fullstack-dev-skills:test-master`, `playwright-expert` |
| Security | `fullstack-dev-skills:secure-code-guardian`, `security-reviewer`, `/security-review` |
| Docker, CI, deploys | `fullstack-dev-skills:devops-engineer` |
| Library or framework API questions | Context7 docs lookup before relying on memory |
| Review before a PR | `/code-review`, `/pr-review-toolkit:review-pr`, `/simplify` |
| Commits and PRs | `/commit-commands:commit`, `/commit-commands:commit-push-pr` |
| Keeping this file current | `/claude-md-management:revise-claude-md` |

The rules in this file win over a skill when the two disagree. Note the conflict when you hit one.

# ReturnFlow: project context

## 1. Purpose

ReturnFlow manages e-commerce returns and claims. A customer files a claim, a Python service
scores it (category, risk, confidence, reasons), and a deterministic C# policy either settles it
automatically or sends it to an agent. The AI advises but never decides alone, and a customer can
always ask for a person to look again. Managers see KPIs on how much is automated and how fast
claims are settled. It is a portfolio project built on synthetic data.

## 2. Users and key flows

| Role | Flows |
|---|---|
| Customer | File a return with one claim, follow its progress, ask for a human review of an automatic decision |
| Agent | Work the review queue (claims sent to a person, or sent back for review), approve or reject with a comment |
| Manager, Admin | KPI dashboard for D1, D7 and lifetime, compared with the previous period |

The flows that matter most:
1. Claim filed -> queued -> AI analysis -> policy -> auto-approve / auto-reject / agent queue.
2. Customer requests a human review of an automatic decision -> claim reopens -> agent decides.
3. Agent decides a queued claim.
4. Manager reads KPIs.

## 3. Stack

| Part | Folder | Stack |
|---|---|---|
| Web and mobile (PWA) | `returnflow-web` | React 19, TypeScript 7, Vite 8, Tailwind 4, TanStack Query 5, Recharts 3 |
| API and worker | `returnflow-api` | .NET 10, ASP.NET Core controllers, EF Core 10 + Npgsql, JWT bearer, BCrypt, Scalar |
| AI service | `returnflow-ai` | Python 3.12, FastAPI, scikit-learn, Pydantic 2 |
| Database | | PostgreSQL 16 |
| Delivery | | Docker Compose, nginx (serves the web build and proxies `/api`), GitHub Actions |

## 4. Architecture

```
browser ──> nginx (web, :8080) ──/api──> ASP.NET Core API ──> PostgreSQL
                                             │
                                             ├─ ClaimQueue (in-memory channel)
                                             └─ ClaimWorker ──HTTP──> FastAPI AI service
```

- Auth happens in the API: JWT bearer with `sub`, `email`, `role` claims. Roles are checked with
  `[Authorize(Roles = ...)]`, and ownership is checked per resource in the controllers.
- Filing a claim returns at once. The worker analyzes it in the background
  (`Workers/ClaimWorker.cs` -> `Services/ClaimProcessor.cs`). On startup the worker re-queues
  claims still in `Analyzing`.
- If the AI service is down or returns an error, the claim goes to an agent.
- The web app polls: every 2.5 s while a claim is analyzing, every 5 s on the review queue,
  every 10 s on the dashboard.

## 5. Features index

| Feature | Owner | Status | Doc |
|---|---|---|---|
| Auth (register, login, demo logins) | `AuthController`, `TokenService`, `web/lib/auth.tsx` | shipped | to write |
| Returns and claims filing | `ReturnsController`, `web/pages/NewReturn.tsx`, `MyReturns.tsx` | shipped | to write |
| Automated triage (AI + policy + worker) | `ClaimProcessor`, `DecisionPolicy`, `ClaimWorker`, `returnflow-ai` | shipped | to write |
| Human review queue and decisions | `ClaimsController`, `web/pages/ReviewQueue.tsx` | shipped | to write |
| Human review on request | `ReturnClaim.RequestHumanReview`, `ClaimsController.RequestReview` | shipped | to write |
| KPI dashboard | `StatsService`, `web/pages/Dashboard.tsx` | shipped | to write |
| Localization (en, fr, de) | `web/lib/i18n.tsx` | shipped | to write |
| Audit log | `Domain/AuditLog.cs` (written by controllers and processor) | shipped, write-only | to write |

## 6. Conventions

Rules: `CLAUDE.md`. Skills: `review-ready-code`, `human-interface`, `feature-docs`, `dev-pipeline`
(in `.claude/skills/`). Language conventions: PascalCase members in C#, camelCase in TS, and
snake_case in Python except the Pydantic schemas, which currently use camelCase field names to
match the JSON contract.

## 7. Glossary

| Term | Meaning |
|---|---|
| Return (`ReturnRequest`) | A customer's request to send back one order item. Holds order reference, purchase date, item. |
| Claim (`ReturnClaim`) | What the customer asks for on a return: a description and an amount. Today the UI files exactly one per return. |
| Analysis | The AI service's output for a claim: category, risk score, confidence, reasons, recommendation. Advisory only. |
| Recommendation | `Approve` or `Review`. The AI never recommends rejection. |
| Policy (`DecisionPolicy`) | The deterministic rules that route a claim: auto-approve, auto-reject, or human. |
| Route | The policy's output: `AutoApprove`, `AutoReject`, `Human`. |
| Routing reason | The sentence explaining why the policy chose a route. Shown to customers and agents. |
| Processing state | Where a claim is in the workflow: `Analyzing`, `AutoDecided`, `NeedsHuman`, `HumanReviewRequested`, `HumanDecided`. |
| Status | The claim's outcome: `Open`, `Approved`, `Rejected`. |
| Decision | One approve/reject verdict, by `Auto` or `Human`. A reopened claim has more than one. |
| Human review (request) | A customer or staff member asking for a person to re-decide an automatic decision. |
| Spot-check | A random share (`SpotCheckRate`) of would-be auto-approvals sent to a person to measure the AI. |
| Staff | Agent, Manager or Admin. |
| D1 / D7 / Lifetime | KPI ranges: last 24 hours, last 7 days, all time. D1 and D7 compare with the period before. |

## 8. Style

Plain, calm operations tool. Tailwind slate neutrals with an indigo accent, emerald / amber / rose
for outcome and risk. System font stack. Mobile first: bottom tab bar and bottom sheets on phones,
top nav and centered dialogs from `sm` up. Shared primitives live in `components/ui.tsx`.

## 9. Environments and config

| Setting | Where | Notes |
|---|---|---|
| `ConnectionStrings:Default` | `appsettings.json`, env `ConnectionStrings__Default` | |
| `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` | `appsettings.json`, env `Jwt__Key` (compose: `JWT_KEY`) | Startup refuses the public dev key outside Development |
| `Seed:DemoData`, `Seed:Password` | env `Seed__*` (compose: `DEMO_MODE`, `DEMO_PASSWORD`) | Demo users and data are only seeded in Development |
| `Policy:*` | `appsettings.json` | Thresholds for automation |
| `Ai:BaseUrl` | env `Ai__BaseUrl` | |
| `Cors:Origins` | config | Only needed if web and API are on different origins |
| `API_URL` | web dev server env | Target of the Vite `/api` proxy |

Secrets live in `.env` (from `.env.example`), never in the repo. Compose runs the API in
`Development` so the demo works out of the box.

## 10. Decisions log

| Date | Decision | Why | Rejected |
|---|---|---|---|
| 2026-10-21 | React + TS / .NET / Python / PostgreSQL | Market research, see `docs/TECH_CHOICES.md` | Single-language stack |
| 2026-10-21 | AI advises, deterministic policy decides, AI never rejects | Explainable, testable, safe fallback when the model is wrong or down | Letting the model decide |
| 2026-10-21 | In-memory queue + background worker | Enough for one instance; filing never waits on the AI | RabbitMQ / Redis for a demo |
| 2026-10-21 | Synthetic training data | No real claims available; metrics are labelled as synthetic | |
| 2026-11-26 | Refactor in place rather than rewrite; publish as one new GitHub repo for the whole stack | Code is small, tested and working; portfolio audience values readability and docs | Full rewrite; one repo per service |

## 11. Extension recipes

- **Endpoint:** add an action to the controller that owns the resource, a request/response class
  in `Dtos/`, the matching type in `web/src/lib/types.ts` and a call in `web/src/lib/api.ts`.
- **Policy rule:** add a step to `DecisionPolicy.Evaluate` in the right order, a threshold in
  `PolicyOptions` + `appsettings.json`, a test in `DecisionPolicyTests`, the BPMN model, and the
  web translation of its routing reason.
- **Table / column:** entity in `Domain/`, configuration in `AppDbContext.OnModelCreating`.
  There are no migrations yet (the schema is created with `EnsureCreated`).
- **Page:** a component in `web/src/pages/`, a tab entry in `App.tsx`, strings in all three
  languages in `lib/i18n.tsx`.

## 12. Tech debt

See the refactor audit (2026-11-26). Short list:
- No concurrency token on `ReturnClaim`: an agent decision and an automatic one can overwrite each other.
- No EF migrations; the schema comes from `EnsureCreated`.
- AI reasons and routing reasons are English sentences that the web app turns back into
  translation keys with regexes (`translateAnalysisText`).
- Error responses mix `{ error }` objects and problem details.
- Floating NuGet versions (`*`, `10.*`) and unpinned Python requirements.
- No web lint or tests, no API integration tests, no Python lint.

## 13. Open questions

- Should the decision policy thresholds in `PolicyOptions` defaults match `appsettings.json`
  (they differ today: 150 vs 600 amount, 0.70 vs 0.65 confidence)?

# returnflow-api

REST API for e-commerce **returns and claims management** with AI-assisted automation: JWT authentication, role-based access, PostgreSQL, a background worker that calls the AI service, a unit-tested decision policy, and KPI endpoints.

Part of the ReturnFlow stack. For the full system, run the compose file one folder up. This README covers the API on its own.

## Run it alone

```bash
docker compose up -d db                      # PostgreSQL only
dotnet run --project src/ReturnFlow.Api      # http://localhost:5080, docs at /scalar
dotnet test                                  # unit tests + integration tests (need Docker)
```

Without the AI service running, new claims are sent to an agent (the API falls back safely). Start `returnflow-ai` to see automatic decisions.

## Roles

`Customer`, `Agent`, `Manager`, `Admin`. Demo users (Development with demo mode only) are `customer@`, `agent@`, `manager@` and `admin@returnflow.local`, all with the demo password.

## Endpoints

| Method | Route | Who | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | anyone | Create a customer account |
| POST | `/api/auth/login` | anyone | Get a JWT |
| GET | `/api/config` | anyone | Demo mode info for the login page |
| POST | `/api/returns` | Customer | Create a return (order reference, purchase date, reason) |
| GET | `/api/returns` | any logged-in user | Own returns (customer) or all (staff) |
| POST | `/api/returns/{id}/claims` | Customer (owner) | File a claim; analysis runs in the background |
| GET | `/api/claims` | Agent, Manager, Admin | Claims with context; filter by `status` or `processing` |
| GET | `/api/claims/{id}` | owner or staff | One claim with AI insight and decision history |
| POST | `/api/claims/{id}/request-review` | owner or staff | Ask a person to review an automatic decision |
| PATCH | `/api/claims/{id}/decision` | Agent, Manager, Admin | Approve or reject |
| GET | `/api/stats?range=d1\|d7\|lifetime` | Manager, Admin | KPIs with previous-period comparison |
| GET | `/health` | anyone | Health check |

## Key design points

- `Domain/ReturnClaim.cs` holds the business rules. The policy can only decide a claim while it is being analyzed, and an agent only once it is waiting for one (routed by the policy, or sent back for review). A review reopens an automatically decided claim and keeps the full decision history.
- `ReturnClaim.Version` maps to PostgreSQL's `xmin`, so two writes to the same claim can't overwrite each other: an agent gets 409, and the worker drops its stale result.
- `Services/DecisionPolicy.cs` is pure code with unit tests. The AI advises, this decides.
- `Workers/ClaimWorker.cs` processes a queue so filing a claim never waits for the AI, and resumes unfinished claims after a restart.
- `Services/StatsService.cs` computes D1, D7 and lifetime KPIs plus the previous period.

## Configuration

`Policy:*` thresholds, `Ai:BaseUrl`, `Jwt:*`, `Seed:DemoData`, `Seed:Password`, and optional `Cors:Origins` (only needed when the web app is on another origin). Use environment variables or `dotnet user-secrets` for secrets.

## Database

The schema comes from EF Core migrations in `src/ReturnFlow.Api/Data/Migrations`, applied at startup. After changing the model:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/ReturnFlow.Api --output-dir Data/Migrations -- --environment Development
```

New rows and their audit entries are written in one transaction (`AppDbContext.AddWithAuditAsync`).

## Tests

- `tests/ReturnFlow.Tests`: unit tests for the decision policy and the claim's state rules.
- `tests/ReturnFlow.IntegrationTests`: the real API against PostgreSQL in a throwaway container (Testcontainers). They cover migrations, the agent-decision rules, concurrency conflicts, audit rows, the AI-down fallback and ownership checks.

## License

MIT

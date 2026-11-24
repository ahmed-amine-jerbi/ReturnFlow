# returnflow-api

REST API for e-commerce **returns and claims management** with AI-assisted automation: JWT authentication, role-based access, PostgreSQL, a background worker that calls the AI service, a unit-tested decision policy, and KPI endpoints.

Part of the ReturnFlow stack. For the full system, run the compose file one folder up. This README covers the API on its own.

## Run it alone

```bash
docker compose up -d db                      # PostgreSQL only
dotnet run --project src/ReturnFlow.Api      # http://localhost:5080, docs at /scalar
dotnet test
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

- `Domain/ReturnClaim.cs` holds the business rules: a claim is decided only while open, and a human review reopens an automatically decided claim while keeping the full decision history.
- `Services/DecisionPolicy.cs` is pure code with unit tests. The AI advises, this decides.
- `Workers/ClaimWorker.cs` processes a queue so filing a claim never waits for the AI, and resumes unfinished claims after a restart.
- `Services/StatsService.cs` computes D1, D7 and lifetime KPIs plus the previous period.

## Configuration

`Policy:*` thresholds, `Ai:BaseUrl`, `Jwt:*`, `Seed:DemoData`, `Seed:Password`, and optional `Cors:Origins` (only needed when the web app is on another origin). Use environment variables or `dotnet user-secrets` for secrets.

## Database

With no EF Core migrations in the project, the schema is created automatically on first run (fine for the demo). When you change the model, either reset the database or add migrations (`dotnet ef migrations add Name --project src/ReturnFlow.Api`), which are then applied at startup.

## License

MIT

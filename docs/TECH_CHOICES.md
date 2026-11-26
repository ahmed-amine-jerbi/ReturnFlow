# Why this stack

ReturnFlow has three jobs with different needs: a business API that must be correct and
auditable, a model that must be explainable, and a UI that customers use on their phones. Each
part uses the tool that suits its job, and they talk over plain HTTP and JSON.

| Part | Choice | Why it fits this product | What it costs |
|---|---|---|---|
| Business API | C# / ASP.NET Core on .NET 10, EF Core | Strong typing for the claim state machine, a mature auth stack (JWT bearer, role checks), and a background worker in the same process (`BackgroundService` + `Channel<T>`), so filing a claim never waits on the AI. | Heavier than a minimal Node or Python API for a small service. |
| Decision policy | Plain C# in `DecisionPolicy.cs` | The rules that move money are deterministic, unit-tested and readable by a non-ML reviewer. The model can be wrong or offline and the policy still behaves. | Thresholds have to be tuned by hand. |
| AI service | Python, FastAPI, scikit-learn | Logistic regression gives a risk score whose per-feature contributions become human-readable reasons. FastAPI + Pydantic validate the input contract for free. A separate service can be retrained and redeployed without touching the API. | A second runtime and a network hop. The API falls back to an agent when the service is down. |
| Database | PostgreSQL | Relational data with real constraints (users, returns, claims, decisions, audit log) and decimals for money. | |
| Web and mobile | React, TypeScript, Vite, Tailwind, TanStack Query | One responsive codebase that installs as a PWA, so customers on phones and agents on desktops share it. TanStack Query handles the polling while a claim is being analyzed. | No native app features beyond what a PWA offers. |
| Delivery | Docker Compose, nginx, GitHub Actions | `docker compose up` runs the whole stack on one port. nginx serves the web build and proxies `/api`, so the browser never needs CORS. CI tests every part on each push. | Compose is for local and demo use, not a production deployment. |

## Alternatives considered

- **One language for everything** (for example Python for API and model, or TypeScript end to
  end). Simpler to run, but it would either weaken the typed state machine or move the model to
  a weaker ML ecosystem.
- **Letting the model decide.** Rejected: an auto-reject from a model trained on synthetic data
  isn't defensible to a customer. The model only recommends `Approve` or `Review`.
- **A message broker between API and AI.** RabbitMQ or Redis streams would let workers scale out,
  but one instance doesn't need it. The in-memory queue recovers pending claims from the database
  on restart.

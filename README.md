# Spot4Hire

A venue & space rental platform backend, built as a portfolio project to
showcase clean, maintainable, idiomatic .NET. Owners list **venues** and the
bookable **units** inside them; customers discover nearby venues (including a
natural-language **AI assistant**) and **book** units for a time window without
double-booking. The whole system is orchestrated locally with **.NET Aspire**.

> **Status:** actively developed. The Web API, AI assistant, and integration
> tests are in place. RAG and the extraction of the AI assistant into its own
> microservice are in progress; see [In-progress work](#in-progress-work).

---

## Tech stack

| Area            | Choice |
|-----------------|--------|
| Runtime         | .NET 10 (C#) |
| Orchestration   | .NET Aspire (AppHost + ServiceDefaults) |
| API             | ASP.NET Core Web API + OpenAPI, [Scalar](https://scalar.com) reference UI |
| Data            | EF Core 10 (SQL Server) + NetTopologySuite for geospatial queries |
| Identity        | ASP.NET Core Identity API endpoints, roles (Admin / Owner / Customer) |
| Validation      | FluentValidation (assembly-scanned, wired via a global action filter) |
| Filtering/paging| [Gridify](https://alirezanet.github.io/Gridify/) on every list endpoint |
| Caching / geo   | Redis (output caching now; geo/vector read-model planned) |
| AI              | `Microsoft.Extensions.AI` (`IChatClient`) + Ollama (local, GPU) |
| Seeding         | Bogus (Development only) |
| Testing         | xUnit v3 + Testcontainers (real SQL Server in Docker) |
| Frontend        | React 19 + Vite 8 + TypeScript (`frontend/`) |

---

## Solution layout

```
Spot4Hire/
├─ Spot4Hire.AppHost/          # Aspire orchestrator that wires up SQL, Redis, Ollama, Backend
├─ Spot4Hire.ServiceDefaults/  # Shared Aspire defaults (telemetry, health, resilience)
├─ Backend/                    # The Web API (domain, EF Core, controllers, AI assistant)
│  ├─ Controllers/             #   Venues, Units, OpeningHours, Bookings, Users, Assistant
│  ├─ Domain/                  #   Entities + interfaces (ISoftDeletable, IAuditable)
│  ├─ Data/                    #   AppDbContext, migrations, seeding
│  ├─ Dtos/  Mapping/  Validators/
│  ├─ Services/                #   Business logic (Result pattern), incl. Services/Assistant
│  ├─ Common/                  #   Result, paging, Gridify mappers, rate-limit policies
│  ├─ Filters/  OpenApi/  Authorization/
├─ Backend.Tests/             # Integration tests (Testcontainers + xUnit v3)
├─ Spot4Hire.Server/          # Web host (Aspire-enabled); frontend host / BFF surface
└─ frontend/                  # React + Vite + TypeScript client
```

---

## Features

**Catalog: venues, units & opening hours.** Full CRUD for venues, the units
inside them, and per-venue opening hours. Deleting a venue **cascade
soft-deletes** its units. Bookings live under units, so their routes nest
naturally (`/api/venues/{venueId}/units/{unitId}/bookings`).

**Geospatial "nearby" search.** Venue coordinates are stored as EF Core
NetTopologySuite geography points (SRID 4326), so "find venues near me" is a
real spatial distance query, and the response includes the distance in meters.

**Bookings without double-booking.** Creating a booking runs inside a
**serializable transaction** that rejects any overlap on the same unit, so two
customers can't book the same slot. Bookings carry a start/end time and expire
naturally, so they don't need soft delete.

**Identity & roles.** Authentication uses the built-in ASP.NET Core Identity
API endpoints (`MapIdentityApi`). Three roles, **Admin**, **Owner**, and
**Customer**, gate behaviour: owners manage only their own venues; only an
admin can reassign a venue's owner (and the assignee must hold the Owner role);
the last admin is protected and an admin can't delete itself.

**Admin user management.** Admin-only user CRUD, including a force-set password
for a user.

**Filtering, sorting & paging everywhere.** Every list endpoint accepts Gridify
query strings, e.g. `?filter=name=*coffee,rating>4&orderBy=name&page=1&pageSize=20`.
Filterable/sortable fields are **whitelisted** via central Gridify mappers
(unmapped fields are ignored, sensitive fields like password hashes are never
exposed), and the allowed fields are documented per endpoint in the OpenAPI /
Scalar docs.

**Cross-cutting concerns.** Soft delete (global query filter) and audit
(created/updated stamps) are handled centrally in `AppDbContext`. Service
methods return a `Result` / `Result<T>` that controllers map to the right HTTP
status. Requests are validated by FluentValidation through a single global
action filter (validators are discovered by assembly scan, no per-controller
wiring). Rate limiting is applied via named policies (the AI assistant has its
own stricter policy).

**API docs.** OpenAPI document + operation transformers add bearer-auth support
and document the Gridify query parameters; the interactive reference is served
by Scalar.

**Mapping.** DTO ↔ entity mapping is done with hand-written extension methods
(deliberately no mapping library; evaluated AutoMapper and Mapperly and found
they cluttered the project).

**Dev seeding & tests.** In Development the database is seeded with Bogus
(customers, owners, an admin, and venues with units + opening hours). The core
booking logic is covered by integration tests that spin up a **real SQL Server**
in Docker via Testcontainers under xUnit v3.

---

## AI calling

Spot4Hire ships a natural-language assistant that answers questions like
*"find me a coffee spot near here that's open now"* by calling the same domain
services the API uses, with no separate data path, so the assistant can never return
something a normal query couldn't.

**How it works**

- **Endpoint:** `POST /api/assistant/ask` (`AssistantController`). It is authorized
  and sits behind its own rate-limit policy because inference is expensive.
- **Chat client:** the assistant talks to an `IChatClient` from
  `Microsoft.Extensions.AI`, configured with `UseFunctionInvocation()` so the
  model can call tools and the framework runs the tool loop automatically. A
  single `GetResponseAsync` call drives the whole multi-step tool conversation.
- **Model:** [Ollama](https://ollama.com) running **locally on the machine** (so
  it reuses the installed GPU and already-downloaded models). It is wired in as
  an Aspire connection string (`ConnectionStrings:chat`) rather than a
  container, via `CommunityToolkit.Aspire.OllamaSharp`.
- **Tools:** five **read-only** functions (`AIFunctionFactory`) wrap the venue,
  unit, and booking services: `SearchVenues`, `FindNearbyVenues`,
  `GetVenueDetails`, `GetUnits`, `CheckAvailability`. Errors are returned to the
  model as data rather than thrown, so it can recover conversationally.

**Design notes**

- **Location privacy.** The caller's latitude/longitude are passed in the
  request and held in a per-request scoped `AssistantContext`. The coordinates
  are **never shown to the model**. The system prompt only tells it *whether* a
  location is available, and the nearby-search tool reads the coordinates
  server-side. So the LLM can do proximity search without ever seeing where the
  user is.
- **Tool parameters are non-nullable by design.** OllamaSharp can't parse
  nullable JSON-schema arrays, so tool parameters avoid nullable types.
- Temperature is kept low (0.2) for deterministic, grounded answers, and time is
  resolved through `TimeProvider` against the venue time zone (`Asia/Jakarta`)
  so "open now" is correct.

---

## Multi-service orchestration with Aspire

`Spot4Hire.AppHost` is the single entry point for local development. It
provisions the infrastructure, starts the app, and wires the connection strings
so nothing has to be configured by hand:

```csharp
var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("sql", password: sqlPassword, port: 14330)
    .WithDataVolume()                        // data survives restarts
    .WithLifetime(ContainerLifetime.Persistent);
var db = sql.AddDatabase("spot4hiredb");

var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent);

// Ollama runs as the app already installed on this machine (GPU + models reused),
// exposed to the API as ConnectionStrings:chat.
var chat = builder.AddConnectionString("chat");

builder.AddProject<Projects.Spot4Hire_Backend>("backend")
    .WithReference(db)
    .WithReference(redis)
    .WithReference(chat)
    .WaitFor(db)
    .WaitFor(redis);
```

What this gives you:

- **SQL Server** in a container on a fixed port (14330), with a persistent data
  volume and a persistent container lifetime so your data and schema survive
  between runs. The DB password is an Aspire **secret parameter** stored in User
  Secrets, never in source.
- **Redis** as a persistent container (output caching today; the planned AI
  read-model tomorrow).
- **Ollama** referenced as a connection string so the API uses the local GPU
  install rather than a container.
- The **Backend** API, started only after SQL and Redis are ready
  (`WaitFor`), with all references injected.

`Spot4Hire.ServiceDefaults` supplies the shared Aspire wiring (OpenTelemetry,
health checks, HTTP resilience) that each service opts into.

### Running it locally

Prerequisites: .NET 10 SDK, Docker Desktop, and [Ollama](https://ollama.com)
installed and running (pull a chat model, e.g. `ollama pull llama3.1`). Node 20+
for the frontend.

```bash
# 1) Set the SQL password secret for the AppHost (once)
cd Spot4Hire.AppHost
dotnet user-secrets set "Parameters:sql-password" "<a-strong-password>"

# 2) Run everything through Aspire
dotnet run
#    → opens the Aspire dashboard; SQL + Redis containers start, the API waits
#      for them, then boots. API docs are served by Scalar.

# 3) Frontend (separate terminal)
cd ../frontend
npm install
npm run dev
```

Database migrations are applied on startup; in Development the schema is seeded
with sample data via Bogus.

---

## In-progress work

These are deliberate next steps for the project, documented here as a roadmap,
not yet shipped.

### 1. Decoupling the AI Assistant into its own microservice

The assistant is CPU/GPU-heavy and scales differently from the CRUD API, so the
plan is to extract `AssistantController` + `Services/Assistant` into a **separate
service** that can be scaled (and GPU-scheduled) independently of the main API.

- The assistant service will read venue data from a **Redis read-model** instead
  of hitting SQL Server directly, keeping inference latency low and isolating it
  from the transactional store.
- **Redis geospatial** (`GEOSEARCH`) will back "nearby venue" lookups for the AI
  path, denormalized from the catalog.
- The catalog service will **publish events** when venues change so the Redis
  read-model stays in sync (event-driven, eventually consistent).

### 2. RAG (Retrieval-Augmented Generation)

Beyond structured tool calls, the assistant will retrieve relevant context:
venue descriptions, amenities, policies, and reviews, then ground its answers on it.

- Content is embedded and stored in a **vector index** (Redis Stack vector
  search is the leading candidate, so the geo read-model and the vector store
  share infrastructure).
- At query time the assistant retrieves the top-k relevant chunks and passes
  them to the model alongside the existing function-calling tools, so answers
  combine live availability (tools) with rich descriptive context (RAG).

### 3. Infrastructure as Code (Terraform)

The Aspire topology maps cleanly onto cloud primitives; Terraform will provision
the deployed infrastructure (Azure Container Apps is the leading target): the
API, the AI service, managed SQL, and managed Redis, so environments are
reproducible.

---

## Notes

- `appsettings.Development.json` is committed on purpose: it holds only the
  local connection strings (localhost SQL and the local Ollama endpoint) and no
  secrets. Real secrets (the SQL password) live in **User Secrets**.
- This is a personal portfolio project; issues and suggestions are welcome.

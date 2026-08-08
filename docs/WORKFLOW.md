# FieldOps Workflows

This document describes the **CI/CD pipeline**, **local automation**, and the **domain job lifecycle** used in FieldOps.

---

## 1. CI/CD workflow (GitHub Actions)

**File:** [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)

### Triggers

| Event | Runs |
|---|---|
| Pull request → `main` | Build, test, Docker **build only** (no push) |
| Push → `main` | Build, test, Docker build **+ push** |
| Tag `v*` (e.g. `v1.2.0`) | Build, test, Docker build **+ push** (semver tags) |
| Manual (`workflow_dispatch`) | Same as push |

### Pipeline diagram

```mermaid
flowchart TD
  trigger[Push / PR / Tag] --> testJob[Job: Build and Test]
  testJob --> restore[dotnet restore]
  restore --> build[dotnet build Release]
  build --> tests[dotnet test]
  tests -->|success| dockerJob[Job: Docker Build and Push]
  tests -->|failure| stop[Pipeline stops]
  dockerJob --> buildImage[docker build]
  buildImage -->|PR| noPush[Image not pushed]
  buildImage -->|main or tag| login[Login Docker Hub]
  login --> push[Push mirzasaikatahmmed/fieldops-api]
```

### Jobs

#### Job A — Build & Test
1. Checkout repository  
2. Setup .NET 8  
3. `dotnet restore FieldOps.sln`  
4. `dotnet build -c Release`  
5. `dotnet test -c Release` (unit + integration / Testcontainers)

#### Job B — Docker Build & Push  
**Depends on:** Job A success  

1. Checkout  
2. Docker Buildx  
3. Login to Docker Hub *(skipped on PRs)*  
4. Tag metadata:
   - `latest` on `main`
   - `sha-<commit>`
   - Semver from `v*` tags  
5. Build from root `Dockerfile`  
6. Push only when **not** a pull request  

**Published image:** `mirzasaikatahmmed/fieldops-api`

### Required secrets

Repo → **Settings → Secrets and variables → Actions**

| Secret | Description |
|---|---|
| `DOCKERHUB_USERNAME` | Docker Hub username |
| `DOCKERHUB_TOKEN` | Docker Hub access token (not account password) |

### Dependabot

[`.github/dependabot.yml`](../.github/dependabot.yml) opens weekly PRs for:

- NuGet packages  
- GitHub Actions versions  
- Docker base images (monthly)

---

## 2. Local automation workflow

**Entry point:** [`Makefile`](../Makefile)  
**Scripts:** [`scripts/`](../scripts/)

### Quick commands

| Command | Workflow |
|---|---|
| `make ci` | Restore → build → test (mirrors CI Job A) |
| `make preflight` | Format verify + `make ci` (pre-push gate) |
| `make up` | Compose pull/up + wait until Swagger is up |
| `make smoke` | Curl happy-path against running API |
| `make demo` | `up` + `smoke` + interview talking points |
| `make migrate` | Apply EF Core migrations |
| `make down` / `make logs` | Stop stack / stream API logs |
| `make docker-build` | Build `mirzasaikatahmmed/fieldops-api:latest` locally |

### Local run flow

```mermaid
flowchart LR
  A[make up] --> B[Postgres + MinIO + API]
  B --> C[Wait for /swagger]
  C --> D[make smoke]
  D --> E[Register / users / job / dashboard]
```

### Script map

| Script | Role |
|---|---|
| `scripts/dev-up.sh` | Copy `.env` if missing, `compose pull/up`, poll API readiness |
| `scripts/smoke-test.sh` | Automated API path: register → technician → customer → template → job → dashboard → comment → search |
| `scripts/interview-demo.sh` | Runs up + smoke and prints talking points |
| `scripts/migrate.sh` | `dotnet ef database update` |
| `scripts/preflight.sh` | `dotnet format --verify-no-changes` then `make ci` |

### Typical developer loop

```bash
# First time
cp .env.example .env
make up

# Every change
make preflight        # before push
# or just:
make ci

# Interview / demo
make demo
```

### Compose image

`docker-compose.yml` runs the API from:

`mirzasaikatahmmed/fieldops-api:latest`

Rebuild and retag locally with `make docker-build` if you need a local image.

---

## 3. Domain job workflow (business)

Real-world field inspection flow implemented by the API:

```mermaid
sequenceDiagram
  participant Admin as CompanyAdmin/Dispatcher
  participant Tech as Technician
  participant API as FieldOps API
  participant Hub as SignalR Hub
  participant Store as MinIO
  participant PDF as QuestPDF

  Admin->>API: Create template + customer + job assign
  Tech->>API: PATCH status InProgress
  API->>Hub: JobStatusChanged
  Tech->>API: POST checklist responses
  Tech->>API: Presign + upload photo/signature
  Tech->>Store: PUT file via presigned URL
  Tech->>API: Confirm upload
  Tech->>API: POST complete
  API->>PDF: Generate report
  PDF->>Store: Upload PDF
  API->>Hub: JobStatusChanged Completed
  Admin->>API: GET report URL
```

### Status transitions

```
Scheduled → InProgress → Completed
     ↘           ↘
      Cancelled   Cancelled
```

Invalid transitions (e.g. `Completed` → `Scheduled`) are rejected in BLL.

### AI assistants (optional LLM)

Configured under `Ai` in `appsettings.json` / env (`Ai__ApiKey`, `Ai__BaseUrl`, `Ai__Model`). When `Ai:ApiKey` is empty, `StubLlmClient` returns deterministic demo text (no outbound HTTP).

| Endpoint | Roles | Behavior |
|---|---|---|
| `POST /api/jobs/{id}/ai-summary` | CompanyAdmin, Dispatcher, Technician (own job) | Builds job context → LLM summary → persists `Job.AiSummary` / `AiSummaryGeneratedAt` (included in PDF when present) |
| `POST /api/ai/ask` | CompanyAdmin, Dispatcher | Answers from compact tenant job/dashboard JSON only |
| `GET /api/ai/risk-hints?limit=20` | CompanyAdmin, Dispatcher | BLL rule score (`Low`/`Medium`/`High`) + one-line LLM recommendation |

```mermaid
flowchart LR
  api[API /api/ai and /api/jobs] --> aisvc[AiAssistantService]
  aisvc --> jobs[IJobRepository]
  aisvc --> llm[ILlmClient]
  llm --> live[OpenAiCompatibleLlmClient]
  llm --> stub[StubLlmClient]
```

### Multi-tenant request path

```mermaid
flowchart TD
  req[HTTP request + JWT] --> auth[JWT auth]
  auth --> tenant[TenantProvider reads company_id]
  tenant --> bll[BLL service]
  bll --> dal[DAL repository]
  dal --> ef[EF Core + global query filter]
  ef --> db[(PostgreSQL tenant rows)]
```

---

## 4. Interview talking points (workflow)

When asked “how does your pipeline / workflow work?”:

1. **PR gate:** every PR builds and runs tests; image is built but not published.  
2. **Main gate:** tests must pass before Docker Hub gets a new `latest` image.  
3. **Local parity:** `make ci` mirrors the GitHub test job.  
4. **Domain workflow:** dispatcher creates jobs; technician completes checklist + signature; PDF is generated; SignalR notifies the company group in real time.  
5. **Tenancy:** JWT `company_id` + EF query filters enforce isolation without leaking cross-tenant 404s as “exists elsewhere.”

---

## 5. Related docs

- Root overview & API notes: [`README.md`](../README.md)  
- CI badge: shown at top of README when Actions is enabled on GitHub  

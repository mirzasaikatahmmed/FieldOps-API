# FieldOps

Multi-tenant field service and inspection management backend built with **.NET 8** Minimal APIs, PostgreSQL, SignalR, QuestPDF, and MinIO-compatible object storage.

## Architecture

| Project | Responsibility |
|---|---|
| `COMMON` | Entities, enums, shared interfaces (`ITenantProvider`, `IStorageService`, `IPdfService`), `Result<T>`, pagination helpers |
| `DAL` | `AppDbContext`, Fluent API configs, EF migrations, repositories, data seeder |
| `BLL` | Business services, FluentValidation validators, DTOs, JWT/auth, MinIO + QuestPDF implementations |
| `API` | Thin Minimal API endpoints, SignalR hub, middleware, Swagger, hosted SLA checker |
| `Tests` | Unit + integration tests (`WebApplicationFactory` + Testcontainers Postgres) |

Reference direction (no cycles): `COMMON` ← `DAL` ← `BLL` ← `API`

## Prerequisites

- .NET 8 SDK
- Docker + Docker Compose

## Quick start (Docker)

```bash
cp .env.example .env
docker compose up -d --build
```

API: http://localhost:5000  
Swagger: http://localhost:5000/swagger  
MinIO console: http://localhost:9001  
SignalR hub: `/hubs/job-status?access_token=<jwt>`

### Pull pre-built image from Docker Hub

After CI has published an image:

```bash
export DOCKERHUB_IMAGE=YOUR_DOCKERHUB_USERNAME/fieldops-api:latest
docker compose up -d
```

### Seeded SuperAdmin

| Field | Value |
|---|---|
| Email | `superadmin@fieldops.local` |
| Password | `SuperAdmin123!` |

Configured in `API/appsettings.Development.json` under `Seed:SuperAdmin`.

## Local development (API on host)

```bash
cp .env.example .env
docker compose up -d postgres minio
docker compose run --rm minio-init

# optional: apply migrations manually
export DOTNET_ROOT=/usr/lib/dotnet   # if needed on Ubuntu package installs
dotnet ef database update --project DAL --startup-project API

dotnet run --project API
```

Migrations also apply automatically on API startup (skipped when `ASPNETCORE_ENVIRONMENT=Testing`).

## Happy-path curl flow

```bash
BASE=http://localhost:5000

# 1) Register company + admin
curl -s -X POST "$BASE/api/auth/register-company" \
  -H 'Content-Type: application/json' \
  -d '{
    "companyName":"Acme Field",
    "adminFullName":"Ada Admin",
    "adminEmail":"ada@acme.test",
    "password":"Password123!"
  }' | tee /tmp/fieldops-auth.json

TOKEN=$(python3 -c "import json;print(json.load(open('/tmp/fieldops-auth.json'))['accessToken'])")

# 2) Create technician
curl -s -X POST "$BASE/api/users" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{
    "fullName":"Tom Tech",
    "email":"tom@acme.test",
    "password":"Password123!",
    "role":"Technician"
  }'

# 3) Create customer
curl -s -X POST "$BASE/api/customers" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{
    "name":"Jane Customer",
    "phone":"555-0100",
    "email":"jane@example.com",
    "address":"123 Main St"
  }'

# 4) Create job template
curl -s -X POST "$BASE/api/job-templates" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{
    "name":"AC Maintenance Checklist",
    "fields":[
      {"label":"Unit operational?","fieldType":"Boolean","sortOrder":0,"isRequired":true},
      {"label":"Notes","fieldType":"Text","sortOrder":1,"isRequired":false}
    ]
  }'

# 5) Create job (use IDs from previous responses)
# POST /api/jobs  → then technician login, PATCH status, POST responses,
# POST signature (presign → upload to MinIO → confirm), POST complete, GET report
```

## Roles

- `SuperAdmin` — platform owner (seeded), manages companies via `/api/companies`
- `CompanyAdmin` — users, templates, company-wide jobs/reports, dashboard
- `Dispatcher` — create/assign jobs, dashboard
- `Technician` — assigned jobs only (enforced in BLL)

## Additional API features

| Area | Endpoints |
|---|---|
| Dashboard | `GET /api/dashboard` — status counts, today’s jobs, SLA breaches, technician workload |
| Companies | `GET/POST /api/companies`, `PATCH /api/companies/{id}/activate\|deactivate` (SuperAdmin) |
| Comments | `GET/POST /api/jobs/{id}/comments` |
| Search | `GET /api/jobs?search=&customerId=&templateId=…`, `GET /api/customers?search=` |
| Password | `POST /api/auth/change-password`, `/forgot-password`, `/reset-password` |

Forgot-password issues a one-hour token that is **logged** via `INotificationService` (no real email).

## Multi-tenancy

JWT claim `company_id` is read by `ITenantProvider`. EF Core global query filters scope tenant entities. Cross-tenant access returns `404`. SuperAdmin has null `company_id` (filter inactive); use `IgnoreQueryFilters()` only in explicit SuperAdmin/cross-tenant DAL methods (e.g. SLA checker).

## Tests

```bash
dotnet test
```

Unit tests cover status transitions, JWT claims, and tenant query filters. Integration tests spin up Postgres via Testcontainers and exercise register → job → complete → report.

## CI/CD (GitHub Actions → Docker Hub)

Workflow: `.github/workflows/docker-publish.yml`

| Event | Behavior |
|---|---|
| Pull request to `main` | Build image only (no push) |
| Push to `main` | Build and push `latest` + `sha-<commit>` |
| Tag `v*` (e.g. `v1.0.0`) | Build and push version tags |
| Manual (`workflow_dispatch`) | Build and push |

### Required GitHub secrets

In the repo: **Settings → Secrets and variables → Actions**

| Secret | Value |
|---|---|
| `DOCKERHUB_USERNAME` | Your Docker Hub username |
| `DOCKERHUB_TOKEN` | Docker Hub [Access Token](https://hub.docker.com/settings/security) (not your password) |

Published image: `DOCKERHUB_USERNAME/fieldops-api`

### Create a Docker Hub access token

1. Docker Hub → Account Settings → Security → New Access Token
2. Permission: Read, Write, Delete (or Read & Write)
3. Paste the token into the `DOCKERHUB_TOKEN` GitHub secret

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL |
| `Jwt:Secret` | HMAC signing key (≥32 chars) |
| `Storage:*` | MinIO/S3 endpoint, keys, bucket, public URL |
| `Seed:SuperAdmin:*` | Bootstrap SuperAdmin credentials |

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

"$ROOT/scripts/dev-up.sh"
"$ROOT/scripts/smoke-test.sh" http://localhost:5000

cat <<'EOF'

────────────────────────────────────────
Interview talking points (FieldOps)
────────────────────────────────────────
• Multi-tenant SaaS: CompanyId claim + EF global query filters
• Layered architecture: COMMON → DAL → BLL → API (no circular refs)
• Real-time: SignalR hub /hubs/job-status (WebSockets + JWT)
• Domain workflow: template checklist → assign → responses → signature → PDF
• Object storage: MinIO/S3 presigned uploads (API never proxies file bytes)
• Background work: SLA breach checker every 15 minutes
• Auth: Identity + JWT access/refresh, password reset token stub
• Quality: xUnit + Testcontainers integration tests, GitHub Actions CI

Open Swagger and walk the flow live:
  http://localhost:5000/swagger
EOF

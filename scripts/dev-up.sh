set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

if [[ ! -f .env ]]; then
  cp .env.example .env
  echo "Created .env from .env.example"
fi

echo "==> Pulling images"
docker compose pull

echo "==> Starting services"
docker compose up -d

echo "==> Waiting for API"
for i in $(seq 1 60); do
  if curl -sf http://localhost:5000/swagger/index.html >/dev/null 2>&1; then
    echo "API is ready on http://localhost:5000"
    echo "Swagger: http://localhost:5000/swagger"
    exit 0
  fi
  sleep 2
done

echo "API did not become ready in time. Check: docker compose logs api"
exit 1

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

export DOTNET_ROOT="${DOTNET_ROOT:-/usr/lib/dotnet}"
export PATH="$DOTNET_ROOT:$PATH:${HOME}/.dotnet/tools"

if ! command -v dotnet >/dev/null; then
  echo "dotnet SDK not found"
  exit 1
fi

if ! command -v dotnet-ef >/dev/null 2>&1 && ! dotnet ef --version >/dev/null 2>&1; then
  echo "Installing dotnet-ef tool..."
  dotnet tool install -g dotnet-ef --version 8.0.11 || true
  export PATH="$PATH:${HOME}/.dotnet/tools"
fi

echo "==> Applying migrations (DAL → API)"
dotnet ef database update --project DAL/FieldOps.DAL.csproj --startup-project API/FieldOps.API.csproj
echo "Migrations applied."

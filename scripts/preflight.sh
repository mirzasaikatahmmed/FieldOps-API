set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

export DOTNET_ROOT="${DOTNET_ROOT:-/usr/lib/dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

echo "==> Format check"
dotnet format FieldOps.sln --verify-no-changes --verbosity quiet || {
  echo "Code needs formatting. Run: dotnet format FieldOps.sln"
  exit 1
}

echo "==> CI (restore/build/test)"
make ci

echo "Preflight passed."

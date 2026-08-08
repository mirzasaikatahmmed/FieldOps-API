set -euo pipefail

BASE="${1:-http://localhost:5000}"
EMAIL="smoke-$(date +%s)@demo.fieldops"
PASSWORD="Password123!"

need() { command -v "$1" >/dev/null || { echo "Missing dependency: $1"; exit 1; }; }
need curl
need python3

json_get() {
  python3 -c "import json,sys; d=json.load(sys.stdin); print(d$1)"
}

echo "==> Health / Swagger"
curl -sf "$BASE/swagger/index.html" >/dev/null
echo "OK  Swagger"

echo "==> Register company"
REG=$(curl -sf -X POST "$BASE/api/auth/register-company" \
  -H 'Content-Type: application/json' \
  -d "{\"companyName\":\"Smoke Co\",\"adminFullName\":\"Smoke Admin\",\"adminEmail\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")
TOKEN=$(printf '%s' "$REG" | json_get "['accessToken']")
COMPANY=$(printf '%s' "$REG" | json_get "['companyId']")
echo "OK  Registered company $COMPANY"

auth=(-H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json')

echo "==> Create technician"
TECH=$(curl -sf -X POST "$BASE/api/users" "${auth[@]}" \
  -d "{\"fullName\":\"Smoke Tech\",\"email\":\"tech-$(date +%s)@demo.fieldops\",\"password\":\"$PASSWORD\",\"role\":\"Technician\"}")
TECH_ID=$(printf '%s' "$TECH" | json_get "['id']")
echo "OK  Technician $TECH_ID"

echo "==> Create customer"
CUST=$(curl -sf -X POST "$BASE/api/customers" "${auth[@]}" \
  -d '{"name":"Smoke Customer","phone":"555-0100","email":"cust@demo.fieldops","address":"1 Demo St"}')
CUST_ID=$(printf '%s' "$CUST" | json_get "['id']")

echo "==> Create job template"
TMPL=$(curl -sf -X POST "$BASE/api/job-templates" "${auth[@]}" \
  -d '{"name":"Smoke Checklist","fields":[{"label":"OK?","fieldType":"Boolean","sortOrder":0,"isRequired":true},{"label":"Notes","fieldType":"Text","sortOrder":1,"isRequired":false}]}')
TMPL_ID=$(printf '%s' "$TMPL" | json_get "['id']")

echo "==> Create job"
SCHED=$(python3 -c "from datetime import datetime,timedelta,timezone; print((datetime.now(timezone.utc)+timedelta(hours=3)).strftime('%Y-%m-%dT%H:%M:%SZ'))")
JOB=$(curl -sf -X POST "$BASE/api/jobs" "${auth[@]}" \
  -d "{\"customerId\":\"$CUST_ID\",\"jobTemplateId\":\"$TMPL_ID\",\"assignedTechnicianId\":\"$TECH_ID\",\"title\":\"Smoke Job\",\"scheduledAt\":\"$SCHED\",\"notes\":\"automation\"}")
JOB_ID=$(printf '%s' "$JOB" | json_get "['id']")
echo "OK  Job $JOB_ID"

echo "==> Dashboard"
curl -sf "$BASE/api/dashboard" -H "Authorization: Bearer $TOKEN" >/dev/null
echo "OK  Dashboard"

echo "==> Comment"
curl -sf -X POST "$BASE/api/jobs/$JOB_ID/comments" "${auth[@]}" \
  -d '{"body":"Smoke comment from automation"}' >/dev/null
echo "OK  Comment"

echo "==> Search"
curl -sf "$BASE/api/jobs?search=Smoke" -H "Authorization: Bearer $TOKEN" >/dev/null
echo "OK  Search"

echo
echo "Smoke test passed."
echo "  API:      $BASE"
echo "  Admin:    $EMAIL / $PASSWORD"
echo "  Job ID:   $JOB_ID"
echo "  Swagger:  $BASE/swagger"

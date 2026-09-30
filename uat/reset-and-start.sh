#!/usr/bin/env bash
# Rebuilds the UAT database from scratch and (re)starts the app against it.
#   uat/reset-and-start.sh ["<connection string>"]
# The database name must contain "UAT" (the seeder refuses anything else).
set -euo pipefail
cd "$(dirname "$0")/.."
CONN="${1:-Server=localhost,1433;Database=NaijaPrimeSchool_UAT;User Id=sa;Password=Test@Pass123;TrustServerCertificate=True}"
PORT="${UAT_PORT:-5080}"

# Stop the instance this script started last time.
PIDFILE=uat/results/app.pid
if [ -f "$PIDFILE" ]; then
  pkill -P "$(cat "$PIDFILE")" 2>/dev/null || true
  kill "$(cat "$PIDFILE")" 2>/dev/null || true
  rm -f "$PIDFILE"; sleep 2
fi
if curl -s -o /dev/null "http://localhost:$PORT/"; then
  echo "Something is already listening on port $PORT; stop it first." >&2; exit 1
fi

dotnet build uat/seed -v q -nologo | grep -E "error|Warn|Build succeeded" || true
dotnet build src/NaijaPrimeSchool.Web -v q -nologo | grep -E " error |Build succeeded" || true
dotnet run --no-build --project uat/seed -- "$CONN" --reset | grep -v '^warn'

mkdir -p uat/results
# Short reminder timings so UAT can watch email/SMS go out (Log providers write them to app.log).
ConnectionStrings__DefaultConnection="$CONN" ASPNETCORE_ENVIRONMENT=Development \
  Notifications__UnreadGraceMinutes=1 Notifications__Messages__GraceMinutes=1 Notifications__DispatchIntervalSeconds=10 \
  nohup dotnet run --no-build --no-launch-profile --project src/NaijaPrimeSchool.Web --urls "http://localhost:$PORT" \
  > uat/results/app.log 2>&1 &
echo $! > "$PIDFILE"
for _ in $(seq 1 60); do
  if curl -s -o /dev/null -w '%{http_code}' "http://localhost:$PORT/Account/Login" | grep -q 200; then echo "App running on http://localhost:$PORT"; exit 0; fi
  sleep 2
done
echo "App did not start; see uat/results/app.log" >&2; exit 1

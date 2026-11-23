#!/usr/bin/env bash
# Starts everything a round in the browser needs: SQL Server and
# Azurite, the AppHost and the frontend dev server. Stop it again
# with scripts/play-stop.sh.
#
# It needs no .env and no appsettings.Development.json: the
# infrastructure runs as a compose project of its own ("oma-play",
# with its own volumes), and the AppHost gets its settings from
# environment variables. Set MSSQL_SA_PASSWORD to use another
# password than the one from .env.example, and OMA_PLAY_DIR for
# another place than $XDG_RUNTIME_DIR/oma-play for the AppHost's
# pid file and log (CI collects the log from there).
#
# The AppHost binds the same silo ports as the tests that boot the
# host, so stop it before running those.
set -euo pipefail

cd "$(dirname "$0")/.."

project=oma-play
state_dir="${OMA_PLAY_DIR:-${XDG_RUNTIME_DIR:-/tmp}/oma-play}"
pid_file="$state_dir/apphost.pid"
log_file="$state_dir/apphost.log"
api_url=http://localhost:5000
frontend_url=http://localhost:5173

export MSSQL_SA_PASSWORD="${MSSQL_SA_PASSWORD:-Change-me-123}"

apphost_running() {
  [[ -f "$pid_file" ]] && kill -0 "$(<"$pid_file")" 2>/dev/null
}

# Polls a URL until it answers with a success status.
wait_for() {
  local url=$1 seconds=$2
  for ((i = 0; i < seconds; i++)); do
    curl --silent --fail --output /dev/null "$url" && return 0
    sleep 1
  done
  return 1
}

mkdir -p "$state_dir"

echo "==> Infrastructure (SQL Server, Azurite)"
docker compose -p "$project" up -d --wait

echo "==> AppHost"
if apphost_running; then
  echo "already running (pid $(<"$pid_file"))"
else
  if curl --silent --output /dev/null "$api_url/health"; then
    echo "Something else already listens on $api_url." >&2
    exit 1
  fi

  # Built in the foreground, so a compile error shows up here and
  # not in the log file.
  dotnet build src/AppHost --nologo --verbosity quiet

  # A session of its own: play-stop.sh ends `dotnet run` and the
  # host it started by signalling the whole process group.
  ConnectionStrings__Sql="Server=localhost,1433;Database=OrleansMonsterArena;User Id=sa;Password=$MSSQL_SA_PASSWORD;TrustServerCertificate=True" \
  ConnectionStrings__TableStorage="UseDevelopmentStorage=true" \
  Cors__AllowedOrigins__0="$frontend_url" \
    setsid dotnet run --no-build --project src/AppHost \
    >"$log_file" 2>&1 </dev/null &
  echo $! >"$pid_file"
fi

echo "==> Frontend"
if [[ ! -d frontend/node_modules ]]; then
  docker compose -f frontend/compose.yaml run --rm frontend npm ci
fi
docker compose -f frontend/compose.yaml up -d frontend

echo "==> Waiting for the AppHost"
if ! wait_for "$api_url/health/ready" 120; then
  echo "The AppHost did not become ready. Last log lines:" >&2
  tail -n 30 "$log_file" >&2
  exit 1
fi

echo "==> Waiting for the frontend"
if ! wait_for "$frontend_url" 60; then
  echo "The frontend did not answer. Last log lines:" >&2
  docker compose -f frontend/compose.yaml logs --tail 30 frontend >&2
  exit 1
fi

cat <<EOF

Ready. Play at $frontend_url
  API:         $api_url
  AppHost log: $log_file
  Stop:        scripts/play-stop.sh
EOF

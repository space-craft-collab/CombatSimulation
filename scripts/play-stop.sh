#!/usr/bin/env bash
# Stops what scripts/play-start.sh started: the frontend dev
# server, the AppHost and the infrastructure containers.
#
#   scripts/play-stop.sh          # keep the data for the next round
#   scripts/play-stop.sh --wipe   # also delete the oma-play volumes
set -euo pipefail

cd "$(dirname "$0")/.."

project=oma-play
# Must match play-start.sh, OMA_PLAY_DIR included.
state_dir="${OMA_PLAY_DIR:-${XDG_RUNTIME_DIR:-/tmp}/oma-play}"
pid_file="$state_dir/apphost.pid"

down_args=()
case "${1:-}" in
  "") ;;
  --wipe) down_args+=(--volumes) ;;
  *)
    echo "Usage: $0 [--wipe]" >&2
    exit 2
    ;;
esac

# compose.yaml insists on the variable even to stop the containers;
# its value does not matter here.
export MSSQL_SA_PASSWORD="${MSSQL_SA_PASSWORD:-unused}"

echo "==> Frontend"
docker compose -f frontend/compose.yaml down

echo "==> AppHost"
if [[ -f "$pid_file" ]]; then
  pid=$(<"$pid_file")
  # Only a process that still is the AppHost's `dotnet run`; the
  # pid may have been reused since.
  if ps -o args= -p "$pid" 2>/dev/null | grep -q 'src/AppHost'; then
    # play-start.sh made it a process group leader, so the group
    # is `dotnet run` plus the host it started.
    if [[ "$(ps -o pgid= -p "$pid" | tr -d ' ')" == "$pid" ]]; then
      target="-$pid"
    else
      target="$pid"
    fi

    kill -TERM -- "$target" 2>/dev/null || true
    for ((i = 0; i < 20; i++)); do
      kill -0 -- "$target" 2>/dev/null || break
      sleep 1
    done
    if kill -0 -- "$target" 2>/dev/null; then
      echo "still running after 20 s, killing it"
      kill -KILL -- "$target" 2>/dev/null || true
    fi
    echo "stopped (pid $pid)"
  else
    echo "not running"
  fi
  rm -f "$pid_file"
else
  echo "not running"
fi

echo "==> Infrastructure"
docker compose -p "$project" down "${down_args[@]}"

echo
echo "Stopped."

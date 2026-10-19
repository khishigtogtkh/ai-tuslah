#!/usr/bin/env bash
set -euo pipefail
task_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$task_root"
export DOTNET_ROOT="$task_root/.tools/dotnet"
export DOTNET_CLI_HOME="$task_root/.tools/cli-home"
export NUGET_PACKAGES="$task_root/.tools/nuget"
export PATH="$DOTNET_ROOT:$task_root/.tools/ef:$PATH"
export AI_AGENT_DATA_DIR="${AI_AGENT_DATA_DIR:-$task_root/.data}"
umask 077
if [[ -f .env ]]; then
  set -a
  source .env
  set +a
fi
if [[ $# -eq 0 ]]; then
  exec dotnet run --project backend/src/AiAgent.Api --launch-profile http
fi
exec dotnet "$@"

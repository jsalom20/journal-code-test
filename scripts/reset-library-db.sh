#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DB_PATH="${REPO_ROOT}/src/apps/server/Library.Api/library.dev.db"
API_DIR="${REPO_ROOT}/src/apps/server/Library.Api"

rm -f "${DB_PATH}" "${DB_PATH}-shm" "${DB_PATH}-wal"

cd "${API_DIR}"
dotnet run -- --seed-only

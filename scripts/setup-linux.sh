#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK is required but was not found."
  exit 1
fi

echo "Restoring dependencies..."
dotnet restore "${REPO_ROOT}/GogGameDownloader.sln"

echo "Building project..."
dotnet build "${REPO_ROOT}/src/GogGameDownloader.csproj" -c Release --no-restore

echo "Setup complete."

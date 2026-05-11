#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

REQUIRED_DOTNET_MAJOR=10
REQUIRED_DOTNET_CHANNEL="${REQUIRED_DOTNET_MAJOR}.0"
DOTNET_INSTALL_DIR="${REPO_ROOT}/.dotnet"
DOTNET_CMD=""

resolve_dotnet_command() {
  if command -v dotnet >/dev/null 2>&1; then
    echo "dotnet"
    return
  fi
  local local_dotnet="${DOTNET_INSTALL_DIR}/dotnet"
  if [ -x "${local_dotnet}" ]; then
    echo "${local_dotnet}"
    return
  fi
  echo ""
}

get_installed_sdk_majors() {
  local dotnet_cmd="$1"
  "${dotnet_cmd}" --list-sdks 2>/dev/null | grep -oE '^[0-9]+' || true
}

install_dotnet_sdk() {
  echo ".NET SDK ${REQUIRED_DOTNET_CHANNEL} not found. Installing into ${DOTNET_INSTALL_DIR}..."
  mkdir -p "${DOTNET_INSTALL_DIR}"
  local installer_path
  installer_path="$(mktemp /tmp/dotnet-install-XXXXXX.sh)"
  trap 'rm -f "${installer_path}"' EXIT

  if command -v curl >/dev/null 2>&1; then
    curl -fsSL "https://dot.net/v1/dotnet-install.sh" -o "${installer_path}"
  elif command -v wget >/dev/null 2>&1; then
    wget -qO "${installer_path}" "https://dot.net/v1/dotnet-install.sh"
  else
    echo "Neither curl nor wget found. Please install one and retry." >&2
    exit 1
  fi

  chmod +x "${installer_path}"
  bash "${installer_path}" --channel "${REQUIRED_DOTNET_CHANNEL}" --install-dir "${DOTNET_INSTALL_DIR}" --no-path
  rm -f "${installer_path}"
  trap - EXIT
}

DOTNET_CMD="$(resolve_dotnet_command)"

if [ -n "${DOTNET_CMD}" ]; then
  SDK_MAJORS="$(get_installed_sdk_majors "${DOTNET_CMD}")"
else
  SDK_MAJORS=""
fi

if ! echo "${SDK_MAJORS}" | awk -v req="${REQUIRED_DOTNET_MAJOR}" '$1 >= req {found=1} END {exit !found}'; then
  install_dotnet_sdk
  local_dotnet="${DOTNET_INSTALL_DIR}/dotnet"
  if [ ! -x "${local_dotnet}" ]; then
    echo "Automatic .NET SDK installation completed, but dotnet could not be found. Try opening a new shell." >&2
    exit 1
  fi
  DOTNET_CMD="${local_dotnet}"
  SDK_MAJORS="$(get_installed_sdk_majors "${DOTNET_CMD}")"
  if ! echo "${SDK_MAJORS}" | awk -v req="${REQUIRED_DOTNET_MAJOR}" '$1 >= req {found=1} END {exit !found}'; then
    echo "This project targets .NET ${REQUIRED_DOTNET_MAJOR}.0, but setup could not locate .NET SDK ${REQUIRED_DOTNET_MAJOR} or newer after installation." >&2
    exit 1
  fi
fi

echo "Restoring dependencies..."
"${DOTNET_CMD}" restore "${REPO_ROOT}/GogGameDownloader.sln"

echo "Building project..."
"${DOTNET_CMD}" build "${REPO_ROOT}/src/GogGameDownloader.csproj" -c Release --no-restore

echo "Setup complete."

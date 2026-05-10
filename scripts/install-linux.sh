#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
PUBLISH_DIR="${REPO_ROOT}/dist/linux-x64"
INSTALL_DIR="${HOME}/.local/share/GogGameDownloader"
BIN_DIR="${HOME}/.local/bin"
LAUNCHER_PATH="${BIN_DIR}/gog-game-downloader"

echo "Publishing linux-x64 build..."
dotnet publish "${REPO_ROOT}/src/GogGameDownloader.csproj" \
  -c Release \
  -r linux-x64 \
  --self-contained false \
  -o "${PUBLISH_DIR}"

echo "Installing to ${INSTALL_DIR}..."
mkdir -p "${INSTALL_DIR}" "${BIN_DIR}"
find "${INSTALL_DIR}" -mindepth 1 -delete
cp -a "${PUBLISH_DIR}/." "${INSTALL_DIR}/"

cat > "${LAUNCHER_PATH}" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
exec "${HOME}/.local/share/GogGameDownloader/GogGameDownloader" "$@"
EOF

chmod +x "${LAUNCHER_PATH}"

echo "Install complete."
echo "Run with: gog-game-downloader"

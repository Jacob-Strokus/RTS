#!/usr/bin/env bash
set -euo pipefail

UNITY_PATH="${UNITY_PATH:-}"
PROJECT_PATH="${PROJECT_PATH:-"$(cd "$(dirname "$0")/.." && pwd)/unity_project"}"
NORUN="${NORUN:-}" # set to 1 to skip run

resolve_unity() {
  if [[ -n "$UNITY_PATH" && -x "$UNITY_PATH" ]]; then echo "$UNITY_PATH"; return; fi
  if [[ -n "${UNITY_EDITOR_PATH:-}" && -x "${UNITY_EDITOR_PATH}" ]]; then echo "${UNITY_EDITOR_PATH}"; return; fi
  # Try common Linux path via Unity Hub
  for p in \
    "/opt/unityhub/editor" \
    "$HOME/.local/share/unity3d/Hub/Editor" \
    "$HOME/Unity/Hub/Editor"; do
    if [[ -d "$p" ]]; then
      cand=$(find "$p" -maxdepth 2 -type f -name Unity -path "*/Editor/Unity" 2>/dev/null | sort -r | head -n1)
      if [[ -n "$cand" ]]; then echo "$cand"; return; fi
    fi
  done
  echo "Unity editor not found. Set UNITY_EDITOR_PATH or UNITY_PATH" >&2
  exit 1
}

UNITY_EXE=$(resolve_unity)
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BUILD_DIR="$REPO_ROOT/build"
mkdir -p "$BUILD_DIR"
LOG_FILE="$BUILD_DIR/cli-build.log"

"$UNITY_EXE" \
  -batchmode -nographics -quit \
  -projectPath "$PROJECT_PATH" \
  -executeMethod FrontierAges.EditorTools.Build.StandaloneBuilder.BuildWindows64 \
  -logFile "$LOG_FILE"

LATEST="$REPO_ROOT/bin/windows-x64-latest/FrontierAges.exe"
if [[ ! -f "$LATEST" ]]; then
  echo "Build exe not found at $LATEST" >&2
  echo "See log: $LOG_FILE" >&2
  exit 1
fi

echo "Build success → $LATEST"
if [[ -z "$NORUN" ]]; then
  # On WSL/Linux, try to launch via powershell.exe if on Windows filesystem
  if command -v powershell.exe >/dev/null 2>&1; then
    powershell.exe -NoProfile -Command "Start-Process -FilePath '$LATEST' -WorkingDirectory '$(dirname "$LATEST")'" || true
  fi
fi

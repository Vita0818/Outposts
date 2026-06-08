#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_PATH="$PROJECT_DIR/Kikaria.xcodeproj"
SCHEME="Kikaria"
CONFIGURATION="${CONFIGURATION:-Debug}"
DERIVED_DATA_PATH="${DERIVED_DATA_PATH:-$PROJECT_DIR/.build/DerivedData}"

choose_iphone_simulator() {
  local devices selected

  devices="$(xcrun simctl list devices available)"
  selected="$(
    printf '%s\n' "$devices" |
      awk '
        /-- iOS / { in_ios = 1; next }
        /^-- / { in_ios = 0 }
        in_ios && /^[[:space:]]*iPhone/ && /\(Booted\)/ { print; exit }
      '
  )"

  if [[ -z "$selected" ]]; then
    selected="$(
      printf '%s\n' "$devices" |
        awk '
          /-- iOS / { in_ios = 1; next }
          /^-- / { in_ios = 0 }
          in_ios && /^[[:space:]]*iPhone/ { print; exit }
        '
    )"
  fi

  if [[ -z "$selected" ]]; then
    return 1
  fi

  printf '%s\n' "$selected" | sed -E 's/.*\(([0-9A-Fa-f-]{36})\).*/\1/'
}

if [[ ! -d "$PROJECT_PATH" ]]; then
  echo "error: Xcode project not found at $PROJECT_PATH" >&2
  exit 1
fi

DESTINATION="${DESTINATION:-}"
SIMULATOR_ID="${SIMULATOR_ID:-}"

if [[ -z "$DESTINATION" ]]; then
  if [[ -z "$SIMULATOR_ID" ]]; then
    if ! SIMULATOR_ID="$(choose_iphone_simulator)"; then
      echo "error: No available iPhone simulator found." >&2
      exit 1
    fi
  fi

  DESTINATION="platform=iOS Simulator,id=$SIMULATOR_ID"
fi

echo "Building $SCHEME ($CONFIGURATION)"
echo "Project: $PROJECT_PATH"
echo "Destination: $DESTINATION"
echo "DerivedData: $DERIVED_DATA_PATH"

xcodebuild \
  -project "$PROJECT_PATH" \
  -scheme "$SCHEME" \
  -configuration "$CONFIGURATION" \
  -destination "$DESTINATION" \
  -derivedDataPath "$DERIVED_DATA_PATH" \
  build

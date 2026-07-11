#!/usr/bin/env bash
#
# Builds EsewaBridge.xcframework from EsewaBridge.swift.
#
# MUST be run on macOS with Xcode installed. It compiles the Swift shim
# against the vendored EsewaSDK.framework for both device (arm64) and
# simulator (arm64 + x86_64) and packages the result as an xcframework that
# the .NET iOS binding references.
#
# Usage:  ./build-shim.sh
# Output: ../EsewaBridge.xcframework  (next to EsewaSDK.xcframework)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
NATIVE_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
SDK_XCFRAMEWORK="${NATIVE_DIR}/EsewaSDK.xcframework"
OUT_XCFRAMEWORK="${NATIVE_DIR}/EsewaBridge.xcframework"
BUILD_DIR="${SCRIPT_DIR}/.build"
MODULE_NAME="EsewaBridge"
MIN_IOS="13.0"

rm -rf "${BUILD_DIR}" "${OUT_XCFRAMEWORK}"
mkdir -p "${BUILD_DIR}"

# $1 = arch triple target, $2 = sdk name, $3 = output slice dir
build_slice() {
  local target="$1" sdk="$2" slice="$3"
  local out="${BUILD_DIR}/${slice}"
  mkdir -p "${out}"

  local sdk_path
  sdk_path="$(xcrun --sdk "${sdk}" --show-sdk-path)"
  local fw_search="${SDK_XCFRAMEWORK}/$(fw_slice "${slice}")"

  echo ">> Building ${MODULE_NAME} for ${target} (${sdk})"
  xcrun swiftc \
    -emit-library -emit-module -static \
    -module-name "${MODULE_NAME}" \
    -target "${target}" \
    -sdk "${sdk_path}" \
    -F "${fw_search}" \
    -emit-objc-header -emit-objc-header-path "${out}/${MODULE_NAME}-Swift.h" \
    -o "${out}/lib${MODULE_NAME}.a" \
    "${SCRIPT_DIR}/EsewaBridge.swift"

  # Assemble a .framework for this slice.
  local fw="${out}/${MODULE_NAME}.framework"
  mkdir -p "${fw}/Headers" "${fw}/Modules"
  cp "${out}/lib${MODULE_NAME}.a" "${fw}/${MODULE_NAME}"
  cp "${out}/${MODULE_NAME}-Swift.h" "${fw}/Headers/"
  cp "${out}/${MODULE_NAME}.swiftmodule" "${fw}/Modules/" 2>/dev/null || true
  cat > "${fw}/Modules/module.modulemap" <<EOF
framework module ${MODULE_NAME} {
  umbrella header "${MODULE_NAME}-Swift.h"
  export *
}
EOF
  cat > "${fw}/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleIdentifier</key><string>com.plugin.esewa.${MODULE_NAME}</string>
  <key>CFBundleName</key><string>${MODULE_NAME}</string>
  <key>CFBundleExecutable</key><string>${MODULE_NAME}</string>
  <key>MinimumOSVersion</key><string>${MIN_IOS}</string>
</dict></plist>
EOF
}

# Map our slice name to the matching EsewaSDK.xcframework folder.
fw_slice() {
  case "$1" in
    device)    echo "ios-arm64" ;;
    simulator) echo "ios-arm64_x86_64-simulator" ;;
  esac
}

build_slice "arm64-apple-ios${MIN_IOS}"           "iphoneos"        "device"
build_slice "arm64-apple-ios${MIN_IOS}-simulator" "iphonesimulator" "simulator"

echo ">> Packaging ${OUT_XCFRAMEWORK}"
xcodebuild -create-xcframework \
  -framework "${BUILD_DIR}/device/${MODULE_NAME}.framework" \
  -framework "${BUILD_DIR}/simulator/${MODULE_NAME}.framework" \
  -output "${OUT_XCFRAMEWORK}"

echo ">> Done: ${OUT_XCFRAMEWORK}"

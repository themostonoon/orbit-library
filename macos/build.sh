#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/build/macos"
APP="$OUT/Orbit Library.app"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
SDK="$(xcrun --sdk macosx --show-sdk-path)"
SOURCES=("$ROOT/macos/Core.swift" "$ROOT/macos/Catalog.swift" "$ROOT/macos/LibraryModel.swift" "$ROOT/macos/Interface.swift" "$ROOT/macos/Main.swift")
for ARCH in arm64 x86_64; do
    xcrun swiftc -parse-as-library -swift-version 5 -O -whole-module-optimization -sdk "$SDK" -target "$ARCH-apple-macos13.0" -framework AppKit -framework SwiftUI -framework GameController "${SOURCES[@]}" -o "$OUT/OrbitLibrary-$ARCH"
done
lipo -create "$OUT/OrbitLibrary-arm64" "$OUT/OrbitLibrary-x86_64" -output "$APP/Contents/MacOS/OrbitLibrary"
cp "$ROOT/macos/Info.plist" "$APP/Contents/Info.plist"
cp -R "$ROOT/assets" "$APP/Contents/Resources/"
xcrun swiftc -swift-version 5 "$ROOT/macos/BuildIcon.swift" -o "$OUT/build-icon"
"$OUT/build-icon" "$OUT/Orbit.iconset"
iconutil -c icns "$OUT/Orbit.iconset" -o "$APP/Contents/Resources/Orbit.icns"
codesign --force --deep --sign - "$APP"
codesign --verify --deep --strict "$APP"
lipo "$APP/Contents/MacOS/OrbitLibrary" -verify_arch arm64 x86_64
cp "$ROOT/macos/START-HERE-MAC.txt" "$OUT/START-HERE-MAC.txt"
cd "$OUT"
ditto -c -k --sequesterRsrc --keepParent "Orbit Library.app" OrbitLibrary-Mac.zip
zip -q OrbitLibrary-Mac.zip START-HERE-MAC.txt
shasum -a 256 OrbitLibrary-Mac.zip > OrbitLibrary-Mac.zip.sha256
echo "Universal Mac package: $OUT/OrbitLibrary-Mac.zip"

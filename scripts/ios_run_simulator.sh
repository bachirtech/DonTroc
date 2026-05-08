#!/bin/bash
# ============================================================
# DonTroc — Build & Run iOS Simulator
# ============================================================
# Compile l'app pour le simulateur Apple Silicon (arm64),
# l'installe sur le simulateur démarré et la lance.
# Aucun certificat / provisioning requis (signature désactivée).
# ============================================================
set -e
cd "$(dirname "$0")/.."

BUNDLE_ID="com.bachirdev.dontroc"
APP_PATH="DonTroc/bin/Debug/net9.0-ios/iossimulator-arm64/DonTroc.app"
SIM_NAME="${1:-iPhone 17}"   # Override: ./ios_run_simulator.sh "iPhone 16 Pro"

echo "🔨 Build iOS Debug pour le simulateur ($SIM_NAME)…"
dotnet build DonTroc/DonTroc.csproj \
    -f net9.0-ios -c Debug \
    -p:RuntimeIdentifier=iossimulator-arm64 \
    -p:EnableCodeSigning=false \
    -clp:ErrorsOnly

[ -d "$APP_PATH" ] || { echo "❌ App introuvable : $APP_PATH"; exit 1; }

echo "📲 Démarrage du simulateur $SIM_NAME…"
xcrun simctl boot "$SIM_NAME" 2>/dev/null || true
open -a Simulator

echo "📦 Installation…"
xcrun simctl install booted "$APP_PATH"

echo "🚀 Lancement de $BUNDLE_ID…"
xcrun simctl launch --console-pty booted "$BUNDLE_ID"


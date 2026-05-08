#!/bin/bash
# ============================================================
# DonTroc — Build iOS pour appareil physique (Debug)
# ============================================================
# Prérequis :
#   1. Certificat "Apple Development" installé dans le keychain ✅
#   2. Provisioning Profile de Development incluant l'UDID
#      de votre iPhone, téléchargé depuis :
#         https://developer.apple.com/account/resources/profiles/list
#      Puis double-cliqué pour l'installer dans
#         ~/Library/MobileDevice/Provisioning Profiles/
#   3. iPhone connecté en USB et déverrouillé
# ============================================================
set -e
cd "$(dirname "$0")/.."

# Identifiants extraits du keychain :
TEAM_ID="ZF7KX4SVSJ"
CODESIGN_KEY="Apple Development: bassiroubalde91@yahoo.com (85FYUJRVMP)"

# Le profile sera détecté automatiquement par MSBuild si son AppID
# correspond à com.bachirdev.dontroc et qu'il contient le cert ci-dessus.

echo "🔨 Build iOS Debug pour appareil (ios-arm64)…"
dotnet build DonTroc/DonTroc.csproj \
    -f net9.0-ios -c Debug \
    -p:RuntimeIdentifier=ios-arm64 \
    -p:EnableCodeSigning=true \
    -p:CodesignKey="$CODESIGN_KEY" \
    -p:CodesignProvision="Automatic" \
    -p:_DeploymentTargetiOSTeamID="$TEAM_ID" \
    -clp:ErrorsOnly

APP_PATH="DonTroc/bin/Debug/net9.0-ios/ios-arm64/DonTroc.app"
[ -d "$APP_PATH" ] || { echo "❌ App introuvable : $APP_PATH"; exit 1; }

echo "✅ App signée prête : $APP_PATH"
echo ""
echo "📲 Pour déployer sur l'iPhone :"
echo "   1. Brancher l'iPhone en USB et le déverrouiller"
echo "   2. Faire confiance à l'ordinateur sur l'iPhone"
echo "   3. Lancer : xcrun devicectl device install app --device <UDID> $APP_PATH"
echo ""
echo "💡 Lister les appareils connectés :"
echo "   xcrun devicectl list devices"


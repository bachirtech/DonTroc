#!/bin/bash
# ============================================================
# DonTroc — Build iOS Release + IPA pour App Store
# ============================================================
# Prérequis :
#   1. Certificat "Apple Distribution" installé dans le keychain
#      (différent de "Apple Development" !)
#      Création via Xcode > Settings > Accounts > Manage Certificates > +
#      → "Apple Distribution"
#   2. Provisioning Profile "App Store" pour com.bachirdev.dontroc
#      téléchargé depuis developer.apple.com et installé.
#   3. App enregistrée sur App Store Connect avec le même Bundle ID.
# ============================================================
set -e
cd "$(dirname "$0")/.."

TEAM_ID="ZF7KX4SVSJ"

# Vérifier qu'un cert Distribution est bien présent
if ! security find-identity -v -p codesigning | grep -q "Apple Distribution"; then
    echo "❌ Aucun certificat 'Apple Distribution' trouvé dans le keychain."
    echo "   Créer via Xcode > Settings > Accounts > Manage Certificates > + Apple Distribution"
    exit 1
fi

CODESIGN_KEY=$(security find-identity -v -p codesigning | grep "Apple Distribution" | head -1 | awk -F'"' '{print $2}')
echo "✅ Certificat de distribution : $CODESIGN_KEY"

echo "🔨 Build + Archive iOS Release (IPA)…"
dotnet publish DonTroc/DonTroc.csproj \
    -f net9.0-ios -c Release \
    -p:RuntimeIdentifier=ios-arm64 \
    -p:BuildIpa=true \
    -p:ArchiveOnBuild=true \
    -p:EnableCodeSigning=true \
    -p:CodesignKey="$CODESIGN_KEY" \
    -p:CodesignProvision="Automatic" \
    -p:_DeploymentTargetiOSTeamID="$TEAM_ID" \
    -clp:ErrorsOnly

IPA_PATH=$(find DonTroc/bin/Release/net9.0-ios -name "*.ipa" | head -1)
[ -f "$IPA_PATH" ] || { echo "❌ IPA non généré"; exit 1; }

echo ""
echo "✅ IPA généré : $IPA_PATH"
ls -lh "$IPA_PATH"
echo ""
echo "📤 Pour uploader sur App Store Connect :"
echo "   xcrun altool --upload-app -f \"$IPA_PATH\" -t ios -u APPLE_ID -p APP_SPECIFIC_PASSWORD"
echo "   ou via Transporter.app (Mac App Store)"


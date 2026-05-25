#!/bin/bash
# ============================================================
# DonTroc — Build iOS Release + IPA pour App Store
# ============================================================
# Prérequis :
#   1. Certificat "Apple Distribution" installé dans le keychain
#   2. Provisioning Profile "App Store" pour com.bachirdev.dontroc
#   3. App enregistrée sur App Store Connect avec le même Bundle ID.
#
# 🍎 Particularité : Apple refuse l'upload si l'icône App Store
#    1024×1024 contient un canal alpha. Resizetizer (MAUI) produit
#    TOUJOURS des PNG RGBA. Ce script effectue donc DEUX PASSES :
#       1) build (sans IPA) → génère les PNG d'icônes
#       2) sips PNG→BMP→PNG → aplatit l'alpha + supprime Assets.car
#       3) publish IPA → actool recompile Assets.car avec PNG opaques
# ============================================================
set -e
cd "$(dirname "$0")/.."

TEAM_ID="ZF7KX4SVSJ"
TFM="net9.0-ios"
RID="ios-arm64"
CONFIG="Release"
PROJ="DonTroc/DonTroc.csproj"

OBJ_DIR="DonTroc/obj/$CONFIG/$TFM/$RID"
BIN_APP="DonTroc/bin/$CONFIG/$TFM/$RID/DonTroc.app"

# Vérifier qu'un cert Distribution est bien présent
if ! security find-identity -v -p codesigning | grep -q "Apple Distribution"; then
    echo "❌ Aucun certificat 'Apple Distribution' trouvé dans le keychain."
    echo "   Créer via Xcode > Settings > Accounts > Manage Certificates > + Apple Distribution"
    exit 1
fi

CODESIGN_KEY=$(security find-identity -v -p codesigning | grep "Apple Distribution" | head -1 | awk -F'"' '{print $2}')
echo "✅ Certificat de distribution : $CODESIGN_KEY"

# ------------------------------------------------------------
# Passe 1 : build (sans IPA) pour générer les PNG d'icônes
# ------------------------------------------------------------
echo ""
echo "🔨 [1/3] Build iOS Release (génération des assets)…"
dotnet build "$PROJ" \
    -f "$TFM" -c "$CONFIG" \
    -p:RuntimeIdentifier="$RID" \
    -p:BuildIpa=false \
    -p:ArchiveOnBuild=false \
    -clp:ErrorsOnly

# ------------------------------------------------------------
# Passe 2 : aplatir l'alpha de TOUS les PNG d'icônes iOS
# ------------------------------------------------------------
echo ""
echo "🍎 [2/3] Aplatissement de l'alpha des icônes (App Store)…"

flatten_png() {
    # Aplatit le canal alpha d'un PNG via un aller-retour PNG → JPEG → PNG.
    # Sur macOS récent, sips conserve l'alpha lors d'une conversion BMP, mais
    # la conversion via JPEG (format qui n'a pas d'alpha) garantit le retrait.
    # Qualité JPEG forcée à 100 (best) pour conserver la netteté de l'icône.
    local f="$1"
    [ -f "$f" ] || return 0
    local tmp="${f}.tmp.jpg"
    sips -s format jpeg -s formatOptions best "$f" --out "$tmp" > /dev/null
    sips -s format png "$tmp" --out "$f" > /dev/null
    rm -f "$tmp"
}

ICON_DIRS=(
    "$OBJ_DIR/resizetizer/r/Assets.xcassets/appicon.appiconset"
    "$OBJ_DIR/actool/cloned-assets/Assets.xcassets/appicon.appiconset"
)

count=0
for d in "${ICON_DIRS[@]}"; do
    if [ -d "$d" ]; then
        for f in "$d"/*.png; do
            [ -f "$f" ] || continue
            flatten_png "$f"
            count=$((count + 1))
        done
    fi
done
echo "   → $count PNG d'icônes aplatis."

# Vérification : aucun PNG d'icône ne doit conserver d'alpha
remaining=0
for d in "${ICON_DIRS[@]}"; do
    [ -d "$d" ] || continue
    for f in "$d"/*.png; do
        [ -f "$f" ] || continue
        if sips -g hasAlpha "$f" 2>/dev/null | grep -q "hasAlpha: yes"; then
            echo "   ⚠️  Alpha encore présent : $f"
            remaining=$((remaining + 1))
        fi
    done
done
if [ "$remaining" -gt 0 ]; then
    echo "❌ $remaining PNG ont encore un canal alpha — abandon."
    exit 1
fi
echo "   ✅ Tous les PNG d'icônes sont opaques (RGB sans alpha)."

# Forcer la recompilation de Assets.car par actool
# (sinon il sera réutilisé avec les anciens PNG transparents)
rm -f "$BIN_APP/Assets.car"
rm -rf "$OBJ_DIR/actool/compiled"
rm -rf "$OBJ_DIR/actool/output"
echo "   🗑  Assets.car supprimé → actool va recompiler."

# ------------------------------------------------------------
# Passe 3 : publish IPA signé
# ------------------------------------------------------------
echo ""
echo "🚀 [3/3] Publish IPA signé pour App Store…"
dotnet publish "$PROJ" \
    -f "$TFM" -c "$CONFIG" \
    -p:RuntimeIdentifier="$RID" \
    -p:BuildIpa=true \
    -p:ArchiveOnBuild=true \
    -p:EnableCodeSigning=true \
    -p:CodesignKey="$CODESIGN_KEY" \
    -p:CodesignProvision="Automatic" \
    -p:CodesignEntitlements="Platforms/iOS/Entitlements.Release.plist" \
    -p:_DeploymentTargetiOSTeamID="$TEAM_ID" \
    -clp:ErrorsOnly

IPA_PATH=$(find DonTroc/bin/Release/$TFM -name "*.ipa" | head -1)
[ -f "$IPA_PATH" ] || { echo "❌ IPA non généré"; exit 1; }

# Vérification finale dans le bundle .app empaqueté
echo ""
echo "🔍 Vérification finale des PNG d'icônes dans l'IPA…"
TMP_VERIFY=$(mktemp -d)
unzip -q "$IPA_PATH" -d "$TMP_VERIFY"
APP_IN_IPA=$(find "$TMP_VERIFY/Payload" -maxdepth 1 -name "*.app" -type d | head -1)
fail=0
for f in "$APP_IN_IPA"/AppIcon*.png "$APP_IN_IPA"/appicon*.png; do
    [ -f "$f" ] || continue
    if sips -g hasAlpha "$f" 2>/dev/null | grep -q "hasAlpha: yes"; then
        echo "   ❌ alpha détecté : $(basename "$f")"
        fail=$((fail + 1))
    fi
done
rm -rf "$TMP_VERIFY"
if [ "$fail" -gt 0 ]; then
    echo "❌ Des PNG d'icône ont encore un alpha dans l'IPA — Apple refusera."
    exit 1
fi
echo "   ✅ Aucune icône PNG avec alpha dans l'IPA."

echo ""
echo "✅ IPA généré : $IPA_PATH"
ls -lh "$IPA_PATH"
echo ""
echo "📤 Pour uploader sur App Store Connect :"
echo "   xcrun altool --upload-app -f \"$IPA_PATH\" -t ios -u APPLE_ID -p APP_SPECIFIC_PASSWORD"
echo "   ou via Transporter.app (Mac App Store)"


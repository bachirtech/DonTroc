#!/usr/bin/env zsh
# =============================================================================
# ios_release_device.sh — Build Release iOS + déploiement direct sur device
# =============================================================================
# But : Reproduire le crash Release (LLVM AOT, SdkOnly linker, trimming)
#       en déployant directement sur l'iPhone physique via USB, sans TestFlight.
#
# Pré-requis :
#   - iPhone connecté en USB + approuvé ("Trust this computer")
#   - Certificat "Apple Development" valide dans Keychain
#   - ios-deploy installé : brew install ios-deploy
#
# Usage :
#   chmod +x scripts/ios_release_device.sh
#   ./scripts/ios_release_device.sh
# =============================================================================

set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
CSPROJ="$PROJECT_DIR/DonTroc/DonTroc.csproj"
APP_BUNDLE="$PROJECT_DIR/DonTroc/bin/ReleaseDevice/net9.0-ios/ios-arm64/DonTroc.app"

echo ""
echo "╔══════════════════════════════════════════════════════════╗"
echo "║  DonTroc — Build Release → Device physique              ║"
echo "║  Optimisations Release actives (LLVM, SdkOnly, Trim)   ║"
echo "║  Signature : Apple Development (installation directe)   ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo ""

# ─── 1. Vérifier que ios-deploy est installé ───────────────────
if ! command -v ios-deploy &>/dev/null; then
    echo "❌ ios-deploy non trouvé. Installer avec :"
    echo "   brew install ios-deploy"
    exit 1
fi

# ─── 2. Vérifier qu'un iPhone est connecté ─────────────────────
echo "🔍 Recherche d'un iPhone connecté..."
if ! ios-deploy -c --timeout 5 &>/dev/null; then
    echo "❌ Aucun iPhone détecté. Connecte ton iPhone en USB et approuve la connexion."
    exit 1
fi
echo "✅ iPhone détecté"

# ─── 3. Build Release avec DeployToDevice=true ─────────────────
echo ""
echo "🔨 Build Release (LLVM + SdkOnly + Trimming actifs)..."
echo "   Durée estimée : 5-10 minutes (AOT LLVM complet)"
echo ""

cd "$PROJECT_DIR"

dotnet build "$CSPROJ" \
    -f net9.0-ios \
    -c ReleaseDevice \
    -r ios-arm64 \
    2>&1 | tee /tmp/dontroc_release_device_build.log

echo ""
echo "✅ Build terminé"

# ─── 4. Vérifier que le .app existe ────────────────────────────
if [ ! -d "$APP_BUNDLE" ]; then
    echo "❌ Bundle .app introuvable : $APP_BUNDLE"
    echo "   Consulte le log : /tmp/dontroc_release_device_build.log"
    exit 1
fi

echo "📦 Bundle : $APP_BUNDLE"

# ─── 5. Déployer sur l'iPhone ─────────────────────────────────
echo ""
echo "📲 Déploiement sur l'iPhone..."
echo "   (Les logs s'affichent en temps réel — cherche l'exception native)"
echo ""

ios-deploy \
    --bundle "$APP_BUNDLE" \
    --debug \
    --no-wifi \
    2>&1

echo ""
echo "✅ Déploiement terminé"
echo "   Log de build complet : /tmp/dontroc_release_device_build.log"


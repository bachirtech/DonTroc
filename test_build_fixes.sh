#!/bin/bash

# Script de compilation et test MAUI DonTroc
# Résout les exceptions: XA8000, XABBA7023, XA4314, XA8044

set -e

PROJECT_DIR="/Users/aa1/RiderProjects/DonTroc"
FRAMEWORK="net8.0-android"
CONFIG="Release"

echo "🔧 === NETTOYAGE DES CACHES ===" 
rm -rf "$PROJECT_DIR/DonTroc/bin" 2>/dev/null || true
rm -rf "$PROJECT_DIR/DonTroc/obj" 2>/dev/null || true
echo "✅ Répertoires bin/obj supprimés"

echo ""
echo "📦 === RESTAURATION DES PACKAGES NUGET ==="
cd "$PROJECT_DIR/DonTroc"
dotnet restore DonTroc.csproj
echo "✅ Packages restaurés"

echo ""
echo "🔨 === COMPILATION DEBUG (Test) ==="
dotnet build DonTroc.csproj -f net8.0-android -c Debug --no-restore 2>&1 | tee build_debug_test.log || {
    echo "❌ Erreur en Debug"
    exit 1
}
echo "✅ Compilation Debug réussie"

echo ""
echo "🚀 === COMPILATION RELEASE (Production) ==="
echo "Paramètres de signature:"
echo "  - AndroidSigningKeyAlias: dontroc"
echo "  - AndroidSigningKeyPass: [configuré]"
echo "  - AndroidSigningStorePassword: [configuré]"

dotnet publish DonTroc.csproj \
    -f net8.0-android \
    -c Release \
    --no-restore \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyAlias=dontroc \
    2>&1 | tee build_release_test.log || {
    echo "❌ Erreur en Release"
    echo "Voir build_release_test.log pour plus de détails"
    exit 1
}
echo "✅ Compilation Release réussie"

echo ""
echo "✨ === BUILD COMPLET RÉUSSI ==="
echo "📍 Artefacts générés:"
echo "   - Debug APK: bin/Debug/net8.0-android/DonTroc.apk"
echo "   - Release APK: bin/Release/net8.0-android/*.apk"

exit 0


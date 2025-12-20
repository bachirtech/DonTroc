#!/bin/bash
set -e

echo "=============================================="
echo "  DonTroc - Build AAB Signé"
echo "  .NET 9 MAUI - Android API 35"
echo "=============================================="

PROJECT_DIR="/Users/aa1/RiderProjects/DonTroc/DonTroc"
OUTPUT_DIR="/Users/aa1/RiderProjects/DonTroc/output"
KEYSTORE="/Users/aa1/RiderProjects/DonTroc/keystore/dontroc-release.keystore"
ALIAS="dontroc"
PASS="DonTroc2024!1007"

cd "$PROJECT_DIR"

echo ""
echo "1. Nettoyage complet..."
rm -rf bin obj 2>/dev/null || true

echo ""
echo "2. Restauration des packages..."
/usr/local/share/dotnet/dotnet restore

echo ""
echo "3. Compilation Release avec signature..."
/usr/local/share/dotnet/dotnet publish \
    -f net9.0-android \
    -c Release \
    -p:AndroidPackageFormat=aab \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore="$KEYSTORE" \
    -p:AndroidSigningKeyAlias="$ALIAS" \
    -p:AndroidSigningStorePass="$PASS" \
    -p:AndroidSigningKeyPass="$PASS"

echo ""
echo "4. Recherche du fichier AAB..."
mkdir -p "$OUTPUT_DIR"

AAB_FILE=$(find "$PROJECT_DIR/bin/Release" -name "*.aab" -type f 2>/dev/null | head -1)

if [ -n "$AAB_FILE" ] && [ -f "$AAB_FILE" ]; then
    cp "$AAB_FILE" "$OUTPUT_DIR/"
    FINAL_AAB="$OUTPUT_DIR/$(basename "$AAB_FILE")"
    echo ""
    echo "=============================================="
    echo "✅ BUILD RÉUSSI!"
    echo "=============================================="
    echo "📁 Fichier AAB: $FINAL_AAB"
    echo "📏 Taille: $(du -h "$FINAL_AAB" | cut -f1)"
    ls -la "$FINAL_AAB"
else
    echo ""
    echo "❌ Aucun fichier AAB trouvé"
    echo "Recherche dans bin/Release/..."
    find "$PROJECT_DIR/bin" -name "*.aab" -o -name "*.apk" 2>/dev/null
fi

echo ""
echo "=============================================="


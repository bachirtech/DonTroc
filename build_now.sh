#!/bin/bash
set -e

cd /Users/aa1/RiderProjects/DonTroc

echo "🚀 Build release signé AAB pour Google Play Console..."

KEYSTORE_PATH="/Users/aa1/RiderProjects/DonTroc/keystore/dontroc-release.keystore"
KEY_ALIAS="dontroc"
STORE_PASSWORD='DonTroc2024!1007'
KEY_PASSWORD='DonTroc2024!1007'

dotnet publish DonTroc/DonTroc.csproj \
    -f net8.0-android \
    -c Release \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore="$KEYSTORE_PATH" \
    -p:AndroidSigningKeyAlias="$KEY_ALIAS" \
    -p:AndroidSigningStorePassword="$STORE_PASSWORD" \
    -p:AndroidSigningKeyPassword="$KEY_PASSWORD" \
    -p:AndroidPackageFormat=aab

echo ""
echo "✅ Build terminé!"
echo "📱 Fichier AAB disponible dans:"
ls -la DonTroc/bin/Release/net8.0-android/*.aab 2>/dev/null || echo "Cherche le fichier..."
find DonTroc -name "*.aab" -type f 2>/dev/null


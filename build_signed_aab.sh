#!/bin/bash
# Script de build AAB signé pour DonTroc - API 35

set -e  # Arrêter en cas d'erreur

cd /Users/aa1/RiderProjects/DonTroc/DonTroc

# Variables de signature
KEYSTORE_PATH="../keystore/dontroc-release.keystore"
KEY_ALIAS="dontroc"
STORE_PASS="DonTroc2024!1007"
KEY_PASS="DonTroc2024!1007"

echo "🧹 Nettoyage complet des anciens builds..."
rm -rf bin/Release obj/Release ../output/release-aab/*

echo "📦 Vérification de la configuration..."
grep "AndroidTargetSdkVersion" DonTroc.csproj | head -2

echo ""
echo "🔨 Lancement du build AAB signé avec API 35..."
echo "================================================"

# Build AAB signé
dotnet publish -f net9.0-android -c Release \
    -p:AndroidPackageFormat=aab \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore="$KEYSTORE_PATH" \
    -p:AndroidSigningKeyAlias="$KEY_ALIAS" \
    -p:AndroidSigningStorePass="$STORE_PASS" \
    -p:AndroidSigningKeyPass="$KEY_PASS" \
    -p:AndroidTargetSdkVersion=35 \
    -p:TargetSdkVersion=35 \
    -o ../output/release-aab

# Afficher le résultat
echo ""
echo "=== Résultat du build ==="
if ls ../output/release-aab/*.aab 1> /dev/null 2>&1; then
    echo "✅ Build AAB réussi!"
    ls -lh ../output/release-aab/*.aab
    
    # Vérifier le targetSdkVersion dans le manifest généré
    echo ""
    echo "📋 Vérification du targetSdkVersion dans le manifest généré..."
    AAB_FILE=$(ls ../output/release-aab/*-Signed.aab | head -1)
    if [ -f "$AAB_FILE" ]; then
        # Extraire et vérifier le manifest
        unzip -p "$AAB_FILE" base/manifest/AndroidManifest.xml 2>/dev/null | grep -a "targetSdkVersion" || echo "ℹ️  Impossible de lire le manifest (fichier binaire)"
    fi
else
    echo "❌ Fichier AAB non trouvé"
    exit 1
fi



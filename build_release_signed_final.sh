#!/bin/bash

# Script de build release signé pour DonTroc
# Expert en déploiement Google Play Console

echo "🚀 Début du build release signé pour Google Play Console..."

# Nettoyage du projet
echo "🧹 Nettoyage du projet..."
dotnet clean

# Variables de signature
KEYSTORE_PATH="/Users/aa1/RiderProjects/DonTroc/keystore/dontroc-release.keystore"
KEY_ALIAS="dontroc"
STORE_PASSWORD="DonTroc2024!1007"
KEY_PASSWORD="DonTroc2024!1007"

# Vérification de l'existence du keystore
if [ ! -f "$KEYSTORE_PATH" ]; then
    echo "❌ Erreur: Le keystore n'a pas été trouvé à $KEYSTORE_PATH"
    exit 1
fi

echo "✅ Keystore trouvé: $KEYSTORE_PATH"

# Build release signé
echo "🔨 Compilation du build release signé..."
dotnet publish DonTroc/DonTroc.csproj \
    -f net8.0-android \
    -c Release \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore="$KEYSTORE_PATH" \
    -p:AndroidSigningKeyAlias="$KEY_ALIAS" \
    -p:AndroidSigningStorePassword="$STORE_PASSWORD" \
    -p:AndroidSigningKeyPassword="$KEY_PASSWORD" \
    -p:AndroidPackageFormat=aab \
    -v minimal

# Vérification du résultat
AAB_FILE="DonTroc/bin/Release/net8.0-android/com.bachirdev.dontroc.aab"
if [ -f "$AAB_FILE" ]; then
    echo "✅ Build réussi! Fichier AAB créé:"
    echo "📱 $AAB_FILE"
    echo "📊 Taille: $(du -h "$AAB_FILE" | cut -f1)"
    echo ""
    echo "🎉 Votre build release signé est prêt pour Google Play Console!"
    echo "📋 Prochaines étapes:"
    echo "   1. Uploadez le fichier AAB sur Google Play Console"
    echo "   2. Le problème de permission CAMERA devrait être résolu"
    echo "   3. Conservez le fichier mapping.txt pour le débogage"
else
    echo "❌ Erreur: Le fichier AAB n'a pas été créé"
    exit 1
fi

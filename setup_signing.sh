#!/bin/bash

# Script de configuration des variables d'environnement pour la signature
# Ce script charge les mots de passe depuis le fichier signing.properties

SIGNING_FILE="./keystore/signing.properties"

if [ ! -f "$SIGNING_FILE" ]; then
    echo "❌ Fichier de configuration non trouvé : $SIGNING_FILE"
    echo "🔧 Exécutez d'abord : ./generate_keystore.sh"
    exit 1
fi

echo "🔐 Configuration des variables d'environnement pour la signature..."

# Charger les variables depuis le fichier
source "$SIGNING_FILE"

# Exporter les variables d'environnement
export AndroidSigningStorePassword="$AndroidSigningStorePassword"
export AndroidSigningKeyPassword="$AndroidSigningKeyPassword"

echo "✅ Variables d'environnement configurées"
echo ""
echo "🚀 Vous pouvez maintenant compiler en mode Release :"
echo "   dotnet build -c Release -f net8.0-android"
echo "   dotnet publish -c Release -f net8.0-android"
echo ""
echo "📱 Pour générer un AAB (Android App Bundle) :"
echo "   dotnet publish -c Release -f net8.0-android -p:AndroidPackageFormat=aab"
echo ""
echo "🔗 L'APK/AAB signé sera dans : ./bin/Release/net8.0-android/"

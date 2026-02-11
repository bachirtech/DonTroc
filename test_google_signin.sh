#!/bin/bash

# Script de test pour Google Sign-In
# Ce script compile et lance l'application en mode debug avec les logs visibles

echo "🔧 Nettoyage des fichiers de build précédents..."
cd /Users/aa1/RiderProjects/DonTroc

# Nettoyer
rm -rf DonTroc/bin DonTroc/obj 2>/dev/null

echo "📦 Restauration des packages..."
dotnet restore DonTroc/DonTroc.csproj

echo "🔨 Compilation en mode Debug..."
dotnet build DonTroc/DonTroc.csproj -c Debug -f net9.0-android

if [ $? -eq 0 ]; then
    echo "✅ Compilation réussie!"
    echo ""
    echo "📱 Pour tester l'application :"
    echo "   1. Connectez un appareil Android ou lancez un émulateur"
    echo "   2. Exécutez: dotnet build DonTroc/DonTroc.csproj -c Debug -f net9.0-android -t:Run"
    echo ""
    echo "📋 Pour voir les logs en temps réel :"
    echo "   adb logcat | grep -E '(GoogleAuthService|AuthService|MainActivity|DonTroc)'"
    echo ""
    echo "🔍 Vérifications importantes :"
    echo "   - Google Sign-In activé dans Firebase Console (Authentication > Sign-in method)"
    echo "   - Empreintes SHA-1 ajoutées dans Firebase Console"
    echo "   - google-services.json à jour avec les oauth_client"
else
    echo "❌ Erreur de compilation!"
    exit 1
fi


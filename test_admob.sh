#!/bin/bash

# Script de test de l'intégration AdMob pour DonTroc
# Usage: ./test_admob.sh

echo "🧪 Test de l'intégration AdMob native Android"
echo "=============================================="
echo ""

cd /Users/aa1/RiderProjects/DonTroc

echo "📦 Étape 1: Nettoyage du projet..."
dotnet clean DonTroc/DonTroc.csproj

echo ""
echo "📥 Étape 2: Restauration des packages NuGet..."
dotnet restore DonTroc/DonTroc.csproj

echo ""
echo "🔨 Étape 3: Compilation pour Android (Debug)..."
dotnet build DonTroc/DonTroc.csproj -f net8.0-android -c Debug

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Compilation réussie !"
    echo ""
    echo "📱 Pour déployer sur votre téléphone Android:"
    echo "   1. Connectez votre téléphone en USB"
    echo "   2. Activez le débogage USB"
    echo "   3. Lancez depuis Rider ou exécutez:"
    echo "      dotnet build -t:Run -f net8.0-android"
    echo ""
    echo "🔍 Pour voir les logs AdMob:"
    echo "   adb logcat | grep -i 'admob\\|DonTroc'"
else
    echo ""
    echo "❌ Erreur de compilation. Vérifiez les erreurs ci-dessus."
    echo ""
    echo "💡 Solutions possibles:"
    echo "   - Attendez que la restauration NuGet soit complète"
    echo "   - Redémarrez Rider"
    echo "   - Exécutez: dotnet restore --force"
fi

echo ""
echo "📚 Consultez ADMOB_INTEGRATION_GUIDE.md pour plus d'infos"


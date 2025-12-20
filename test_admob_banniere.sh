#!/bin/bash

# Script de test complet pour DonTroc avec bannière AdMob

echo "🚀 =========================================="
echo "🚀 TEST COMPLET - DonTroc avec AdMob"
echo "🚀 =========================================="
echo ""

# Couleurs pour les messages
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 1. Nettoyer les builds précédents
echo "🧹 Nettoyage des builds précédents..."
dotnet clean DonTroc/DonTroc.csproj -c Debug -f net8.0-android > /dev/null 2>&1
rm -rf DonTroc/bin/Debug/net8.0-android
rm -rf DonTroc/obj/Debug/net8.0-android
echo -e "${GREEN}✅ Nettoyage terminé${NC}"
echo ""

# 2. Restaurer les packages
echo "📦 Restauration des packages NuGet..."
dotnet restore DonTroc/DonTroc.csproj > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Packages restaurés${NC}"
else
    echo -e "${RED}❌ Erreur lors de la restauration des packages${NC}"
    exit 1
fi
echo ""

# 3. Compiler le projet
echo "🔨 Compilation du projet en mode Debug..."
dotnet build DonTroc/DonTroc.csproj -c Debug -f net8.0-android
BUILD_STATUS=$?
echo ""

if [ $BUILD_STATUS -eq 0 ]; then
    echo -e "${GREEN}✅✅✅ COMPILATION RÉUSSIE ✅✅✅${NC}"
    echo ""
    echo "📱 Le projet est prêt pour le déploiement"
    echo ""
    echo "🎯 Points à vérifier lors du test :"
    echo "   1. La bannière AdMob s'affiche-t-elle ?"
    echo "   2. Les logs montrent-ils le succès du chargement ?"
    echo "   3. Les erreurs Firebase ont-elles disparu ?"
    echo ""
    echo "📊 Pour voir les logs AdMob en temps réel :"
    echo "   adb logcat | grep -E '(AdMob|BANNIÈRE|AdView)'"
    echo ""
    echo "📖 Consultez le guide complet : GUIDE_TEST_BANNIERE_ADMOB.md"
    echo ""
    
    # Détecter les appareils connectés
    DEVICES=$(adb devices | grep -w "device" | wc -l)
    if [ $DEVICES -gt 0 ]; then
        echo -e "${GREEN}📱 $DEVICES appareil(s) Android connecté(s)${NC}"
        echo ""
        echo "🚀 Pour déployer l'application maintenant :"
        echo "   dotnet build DonTroc/DonTroc.csproj -t:Run -c Debug -f net8.0-android"
    else
        echo -e "${YELLOW}⚠️  Aucun appareil Android détecté${NC}"
        echo ""
        echo "Pour connecter un appareil :"
        echo "   1. Activez le mode développeur sur votre téléphone"
        echo "   2. Activez le débogage USB"
        echo "   3. Connectez votre téléphone en USB"
        echo "   4. Exécutez : adb devices"
    fi
else
    echo -e "${RED}❌❌❌ ERREURS DE COMPILATION ❌❌❌${NC}"
    echo ""
    echo "Consultez les erreurs ci-dessus pour plus de détails"
    exit 1
fi

echo ""
echo "🚀 =========================================="
echo "🚀 FIN DU TEST"
echo "🚀 =========================================="


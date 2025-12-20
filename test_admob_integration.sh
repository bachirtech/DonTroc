#!/bin/bash

# Script de test pour vérifier l'intégration AdMob dans DonTroc
# Ce script compile l'app et affiche les logs AdMob en temps réel

echo "═══════════════════════════════════════════════════"
echo "🎯 TEST INTÉGRATION ADMOB - DonTroc"
echo "═══════════════════════════════════════════════════"
echo ""

# Couleurs pour les logs
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Aller dans le dossier du projet
cd "$(dirname "$0")/DonTroc" || exit 1

echo "📋 Étape 1: Vérification de la configuration AdMob"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Vérifier l'ID AdMob dans AndroidManifest.xml
if grep -q "com.google.android.gms.ads.APPLICATION_ID" Platforms/Android/AndroidManifest.xml; then
    echo -e "${GREEN}✅ ID AdMob trouvé dans AndroidManifest.xml${NC}"
    grep "com.google.android.gms.ads.APPLICATION_ID" Platforms/Android/AndroidManifest.xml
else
    echo -e "${RED}❌ ID AdMob manquant dans AndroidManifest.xml${NC}"
    exit 1
fi

echo ""
echo "🏗️  Étape 2: Nettoyage et compilation en mode Debug"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Nettoyer les fichiers de build
dotnet clean > /dev/null 2>&1

# Compiler en mode Debug (plus rapide et avec logs détaillés)
echo "⏳ Compilation en cours..."
dotnet build -c Debug -f net8.0-android > build_admob.log 2>&1

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Compilation réussie${NC}"
else
    echo -e "${RED}❌ Erreur de compilation${NC}"
    echo "Consultez build_admob.log pour plus de détails"
    tail -n 20 build_admob.log
    exit 1
fi

echo ""
echo "📦 Étape 3: Vérification du package AdMob dans l'APK"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Rechercher l'APK généré
APK_PATH=$(find bin/Debug/net8.0-android -name "*.apk" | head -n 1)

if [ -z "$APK_PATH" ]; then
    echo -e "${YELLOW}⚠️  APK non trouvé (normal si pas encore installé)${NC}"
else
    echo -e "${GREEN}✅ APK trouvé: $APK_PATH${NC}"
    
    # Vérifier si les classes AdMob sont présentes
    if command -v aapt &> /dev/null; then
        echo "🔍 Recherche des classes AdMob dans l'APK..."
        aapt dump badging "$APK_PATH" | grep -i "admob\|gms.ads" && echo -e "${GREEN}✅ Classes AdMob détectées${NC}" || echo -e "${YELLOW}⚠️  Classes AdMob non détectées${NC}"
    fi
fi

echo ""
echo "📱 Étape 4: Instructions pour tester sur appareil/émulateur"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "1️⃣  Démarrez votre émulateur Android ou connectez votre appareil"
echo ""
echo "2️⃣  Lancez l'application avec:"
echo -e "${BLUE}   dotnet build -c Debug -f net8.0-android -t:Run${NC}"
echo ""
echo "3️⃣  Surveillez les logs AdMob avec:"
echo -e "${BLUE}   adb logcat -s DonTroc:* Ads:* *:E${NC}"
echo ""
echo "4️⃣  Ou utilisez ce script pour voir les logs filtrés:"
echo -e "${BLUE}   ./watch_admob_logs.sh${NC}"
echo ""
echo "═══════════════════════════════════════════════════"
echo "🎯 RECHERCHEZ CES MESSAGES DANS LES LOGS:"
echo "═══════════════════════════════════════════════════"
echo ""
echo "✅ Messages de succès attendus:"
echo "   - 'SDK AdMob initialisé avec succès'"
echo "   - 'CRÉATION BANNIÈRE ADMOB'"
echo "   - 'BANNIÈRE ADMOB CHARGÉE AVEC SUCCÈS'"
echo ""
echo "❌ Messages d'erreur possibles:"
echo "   - ERROR_CODE_INVALID_REQUEST (1) = Mauvais ID AdMob"
echo "   - ERROR_CODE_NETWORK_ERROR (2) = Pas de connexion Internet"
echo "   - ERROR_CODE_NO_FILL (3) = Pas d'annonce disponible (normal)"
echo ""
echo "═══════════════════════════════════════════════════"
echo "📊 Pour obtenir votre ID d'appareil de test:"
echo "═══════════════════════════════════════════════════"
echo ""
echo "Après le premier lancement, cherchez dans les logs:"
echo -e "${YELLOW}Use RequestConfiguration.Builder().setTestDeviceIds(Arrays.asList(\"XXXXXXXX\"))${NC}"
echo ""
echo "Ajoutez cet ID dans MainActivity.cs à la ligne des testDeviceIds"
echo ""


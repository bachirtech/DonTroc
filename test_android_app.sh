#!/bin/bash

# Script de Build et Test - DonTroc Android
# Usage: ./test_android_app.sh [debug|release]

set -e  # Arrêter en cas d'erreur

# Couleurs pour l'affichage
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
CONFIG="${1:-debug}"  # debug par défaut
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CSPROJ="$PROJECT_DIR/DonTroc/DonTroc.csproj"
PACKAGE_NAME="com.bachirdev.dontroc"

echo -e "${BLUE}════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   DonTroc - Build et Test Android${NC}"
echo -e "${BLUE}   Configuration: ${CONFIG}${NC}"
echo -e "${BLUE}════════════════════════════════════════════════${NC}\n"

# 1. Vérifier les prérequis
echo -e "${YELLOW}[1/6]${NC} Vérification des prérequis..."

if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET SDK non trouvé. Installez .NET 8.0${NC}"
    exit 1
fi

if ! command -v adb &> /dev/null; then
    echo -e "${RED}❌ ADB non trouvé. Installez Android SDK Platform Tools${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Prérequis OK${NC}\n"

# 2. Nettoyer le projet
echo -e "${YELLOW}[2/6]${NC} Nettoyage du projet..."
dotnet clean "$CSPROJ" -c "$CONFIG" > /dev/null 2>&1
rm -rf "$PROJECT_DIR/DonTroc/bin/$CONFIG" "$PROJECT_DIR/DonTroc/obj/$CONFIG" 2>/dev/null || true
echo -e "${GREEN}✅ Nettoyage terminé${NC}\n"

# 3. Restaurer les packages
echo -e "${YELLOW}[3/6]${NC} Restauration des packages NuGet..."
dotnet restore "$CSPROJ" --verbosity quiet
echo -e "${GREEN}✅ Packages restaurés${NC}\n"

# 4. Build
echo -e "${YELLOW}[4/6]${NC} Compilation en mode ${CONFIG}..."
if [ "$CONFIG" == "release" ] || [ "$CONFIG" == "Release" ]; then
    dotnet build "$CSPROJ" -f net8.0-android -c Release
    APK_PATH="$PROJECT_DIR/DonTroc/bin/Release/net8.0-android/com.bachirdev.dontroc-Signed.apk"
else
    dotnet build "$CSPROJ" -f net8.0-android -c Debug
    APK_PATH="$PROJECT_DIR/DonTroc/bin/Debug/net8.0-android/com.bachirdev.dontroc-Signed.apk"
fi

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Erreur de compilation${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Compilation réussie${NC}\n"

# 5. Vérifier l'APK
echo -e "${YELLOW}[5/6]${NC} Vérification de l'APK..."
if [ ! -f "$APK_PATH" ]; then
    echo -e "${RED}❌ APK non trouvé: $APK_PATH${NC}"
    exit 1
fi

APK_SIZE=$(du -h "$APK_PATH" | cut -f1)
echo -e "${GREEN}✅ APK généré ($APK_SIZE): $APK_PATH${NC}\n"

# 6. Vérifier les appareils connectés
echo -e "${YELLOW}[6/6]${NC} Vérification des appareils Android..."
DEVICES=$(adb devices | grep -v "List" | grep "device" | wc -l)

if [ "$DEVICES" -eq 0 ]; then
    echo -e "${YELLOW}⚠️  Aucun appareil détecté${NC}"
    echo -e "${YELLOW}   - Connectez un téléphone via USB avec USB Debugging activé${NC}"
    echo -e "${YELLOW}   - Ou démarrez un émulateur Android${NC}\n"
    echo -e "${BLUE}Pour installer manuellement:${NC}"
    echo -e "   adb install -r \"$APK_PATH\"\n"
else
    echo -e "${GREEN}✅ $DEVICES appareil(s) détecté(s)${NC}\n"
    
    # Demander confirmation pour installation
    read -p "Voulez-vous installer l'application ? (o/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[OoYy]$ ]]; then
        echo -e "\n${BLUE}Installation de l'application...${NC}"
        
        # Désinstaller l'ancienne version
        echo -e "${YELLOW}Désinstallation de l'ancienne version...${NC}"
        adb uninstall "$PACKAGE_NAME" 2>/dev/null || true
        
        # Installer la nouvelle version
        echo -e "${YELLOW}Installation de la nouvelle version...${NC}"
        adb install -r "$APK_PATH"
        
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}✅ Installation réussie${NC}\n"
            
            # Demander si on veut lancer l'app
            read -p "Voulez-vous lancer l'application ? (o/N) " -n 1 -r
            echo
            if [[ $REPLY =~ ^[OoYy]$ ]]; then
                echo -e "\n${BLUE}Lancement de l'application...${NC}"
                adb shell am start -n "$PACKAGE_NAME/.MainActivity"
                
                echo -e "\n${BLUE}Surveillance des logs (Ctrl+C pour arrêter)...${NC}"
                echo -e "${YELLOW}────────────────────────────────────────────────${NC}"
                adb logcat | grep -E "DonTroc|AndroidRuntime"
            fi
        else
            echo -e "${RED}❌ Erreur lors de l'installation${NC}"
            exit 1
        fi
    fi
fi

echo -e "\n${BLUE}════════════════════════════════════════════════${NC}"
echo -e "${GREEN}✅ Build terminé avec succès !${NC}"
echo -e "${BLUE}════════════════════════════════════════════════${NC}\n"

# Afficher les commandes utiles
echo -e "${BLUE}Commandes utiles:${NC}"
echo -e "  • Installer:      ${YELLOW}adb install -r \"$APK_PATH\"${NC}"
echo -e "  • Lancer:         ${YELLOW}adb shell am start -n $PACKAGE_NAME/.MainActivity${NC}"
echo -e "  • Logs:           ${YELLOW}adb logcat | grep -E 'DonTroc|AndroidRuntime'${NC}"
echo -e "  • Désinstaller:   ${YELLOW}adb uninstall $PACKAGE_NAME${NC}"
echo -e "  • Logs fichier:   ${YELLOW}adb pull /data/data/$PACKAGE_NAME/files/DonTrocLog.txt${NC}\n"


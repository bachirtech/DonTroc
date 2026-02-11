#!/bin/bash

# =============================================================================
# Script de téléchargement du SDK IronSource pour le binding .NET MAUI
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
JARS_DIR="$SCRIPT_DIR/Jars"

# Couleurs pour l'affichage
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║      Téléchargement SDK IronSource/LevelPlay               ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Créer le dossier Jars si nécessaire
mkdir -p "$JARS_DIR"

echo -e "${YELLOW}⚠️  Le SDK IronSource n'est PAS disponible sur Maven Central public.${NC}"
echo ""
echo -e "${GREEN}📋 Instructions de téléchargement manuel :${NC}"
echo ""
echo -e "1. ${BLUE}Créez un compte Unity LevelPlay${NC}"
echo "   → https://unity.com/products/mediation"
echo ""
echo -e "2. ${BLUE}Téléchargez le SDK Android depuis le dashboard${NC}"
echo "   → https://developers.is.com/ironsource-mobile/android/android-sdk/"
echo "   → Cliquez sur 'Download SDK'"
echo "   → Téléchargez la version Android (fichier .aar)"
echo ""
echo -e "3. ${BLUE}Ou utilisez Gradle pour récupérer le SDK${NC}"
echo "   Créez un projet Android Studio temporaire avec :"
echo '   implementation "com.ironsource.sdk:mediationsdk:8.+"'
echo "   Le SDK sera téléchargé dans ~/.gradle/caches/"
echo ""
echo -e "4. ${BLUE}Copiez le fichier .aar ici :${NC}"
echo "   $JARS_DIR/mediationsdk.aar"
echo ""
echo -e "${YELLOW}Une fois le fichier copié, compilez le binding :${NC}"
echo "   cd $SCRIPT_DIR"
echo "   dotnet build"
echo ""

# Vérifier si le fichier existe déjà
if [ -f "$JARS_DIR/mediationsdk.aar" ]; then
    SIZE=$(du -h "$JARS_DIR/mediationsdk.aar" | cut -f1)
    echo -e "${GREEN}✅ SDK IronSource détecté !${NC}"
    echo -e "   📁 Fichier: $JARS_DIR/mediationsdk.aar"
    echo -e "   📊 Taille: $SIZE"
    
    # Vérifier que c'est un vrai AAR (ZIP)
    if file "$JARS_DIR/mediationsdk.aar" | grep -q "Zip archive"; then
        echo -e "   ✅ Format AAR valide"
    else
        echo -e "   ${RED}❌ Le fichier ne semble pas être un AAR valide${NC}"
        echo -e "   Supprimez-le et téléchargez la bonne version"
    fi
else
    echo -e "${RED}❌ SDK non trouvé dans $JARS_DIR${NC}"
fi

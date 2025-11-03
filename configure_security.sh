#!/bin/bash

# Script de configuration sécurisée pour DonTroc
# Ce script configure les variables d'environnement sensibles

set -e

echo "🔐 Configuration sécurisée de DonTroc"
echo "====================================="

# Vérifier si le fichier de configuration existe
SECURITY_CONFIG="./security_config.env"

if [ ! -f "$SECURITY_CONFIG" ]; then
    echo "📝 Création du fichier de configuration sécurisée..."
    
    # Demander la clé API Google Maps
    echo ""
    echo "🗺️  Configuration Google Maps API"
    echo "Entrez votre clé API Google Maps (sans guillemets) :"
    read -r GOOGLE_MAPS_KEY
    
    # Créer le fichier de configuration
    cat > "$SECURITY_CONFIG" << EOF
# Configuration sécurisée DonTroc
# ⚠️  GARDEZ CE FICHIER SÉCURISÉ ET NE LE COMMITEZ PAS !

# Google Maps API Key
GOOGLE_MAPS_API_KEY=$GOOGLE_MAPS_KEY

EOF
    
    echo "✅ Fichier de configuration créé : $SECURITY_CONFIG"
    
    # Ajouter au .gitignore si nécessaire
    if [ -f ".gitignore" ]; then
        if ! grep -q "security_config.env" .gitignore; then
            echo "security_config.env" >> .gitignore
            echo "✅ Ajouté au .gitignore"
        fi
    else
        echo "security_config.env" > .gitignore
        echo "✅ Fichier .gitignore créé"
    fi
    
else
    echo "✅ Fichier de configuration existant trouvé"
fi

# Charger les variables d'environnement
source "$SECURITY_CONFIG"

echo ""
echo "🔍 Vérification de la configuration..."

# Vérifier que la clé API est définie
if [ -z "$GOOGLE_MAPS_API_KEY" ]; then
    echo "❌ Erreur: Clé API Google Maps non définie"
    exit 1
fi

echo "✅ Clé API Google Maps configurée"

# Remplacer la clé API dans le manifest
MANIFEST_FILE="./DonTroc/Platforms/Android/AndroidManifest.xml"

if [ -f "$MANIFEST_FILE" ]; then
    # Remplacer la variable par la vraie clé pour le build
    sed -i.bak "s/\${GOOGLE_MAPS_API_KEY}/$GOOGLE_MAPS_API_KEY/g" "$MANIFEST_FILE"
    echo "✅ Clé API injectée dans AndroidManifest.xml"
else
    echo "⚠️  Fichier AndroidManifest.xml non trouvé"
fi

echo ""
echo "🎯 Configuration sécurisée terminée !"
echo "Vous pouvez maintenant effectuer votre build de production."

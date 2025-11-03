#!/bin/bash

# Script de build et signature automatique pour DonTroc
# Ce script compile et signe votre application pour la production

set -e  # Arrêter en cas d'erreur

echo "🚀 Build et signature automatique de DonTroc"
echo "============================================="

# Configuration
PROJECT_PATH="./DonTroc/DonTroc.csproj"
SIGNING_FILE="./keystore/signing.properties"
BUILD_CONFIG="Release"
TARGET_FRAMEWORK="net8.0-android"

# Vérifications préliminaires
echo "🔍 Vérifications préliminaires..."

# Vérifier que le projet existe
if [ ! -f "$PROJECT_PATH" ]; then
    echo "❌ Projet non trouvé : $PROJECT_PATH"
    exit 1
fi

# Vérifier que le keystore existe
if [ ! -f "$SIGNING_FILE" ]; then
    echo "❌ Configuration de signature non trouvée"
    echo "🔧 Exécutez d'abord : ./generate_keystore.sh"
    exit 1
fi

# Charger les variables de signature
echo "🔐 Chargement de la configuration de signature..."
source "$SIGNING_FILE"

# Exporter les variables d'environnement pour MSBuild
export AndroidSigningStorePassword="$AndroidSigningStorePassword"
export AndroidSigningKeyPassword="$AndroidSigningKeyPassword"

# Vérifier que les variables sont correctement chargées
if [ -z "$AndroidSigningStorePassword" ] || [ -z "$AndroidSigningKeyPassword" ]; then
    echo "❌ Erreur: Variables de signature non trouvées dans $SIGNING_FILE"
    exit 1
fi

echo "✅ Configuration de signature chargée"

# Nettoyer les builds précédents
echo "🧹 Nettoyage des builds précédents..."
dotnet clean "$PROJECT_PATH" -c "$BUILD_CONFIG" -f "$TARGET_FRAMEWORK"

# Restaurer les packages NuGet
echo "📦 Restauration des packages NuGet..."
dotnet restore "$PROJECT_PATH"

# Publier directement avec signature (sans build séparé pour éviter NETSDK1085)
echo "📱 Publication avec signature..."
dotnet publish "$PROJECT_PATH" \
    -c "$BUILD_CONFIG" \
    -f "$TARGET_FRAMEWORK" \
    -p:AndroidPackageFormat=aab \
    --no-restore

# Vérifier que les fichiers ont été générés
OUTPUT_DIR="./DonTroc/bin/$BUILD_CONFIG/$TARGET_FRAMEWORK/publish"
if [ -d "$OUTPUT_DIR" ]; then
    echo ""
    echo "✅ Build terminé avec succès !"
    echo "📂 Fichiers générés dans : $OUTPUT_DIR"
    echo ""
    ls -la "$OUTPUT_DIR"/*.aab 2>/dev/null || echo "   Recherche dans les sous-dossiers..."
    find "$OUTPUT_DIR" -name "*.aab" -type f 2>/dev/null || echo "   Fichier AAB recherché..."
    echo ""
    echo "🏪 Prêt pour le Google Play Store !"
    echo "   Uploadez le fichier .aab sur la Google Play Console"
else
    echo "❌ Erreur : Répertoire de sortie non trouvé"
    echo "🔍 Recherche des fichiers AAB..."
    find "./DonTroc/bin/$BUILD_CONFIG" -name "*.aab" -type f 2>/dev/null || echo "   Aucun fichier AAB trouvé"
    exit 1
fi

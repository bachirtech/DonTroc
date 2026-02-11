#!/bin/bash
# Script de build AAB signé pour DonTroc
# Cible: Android API 35 avec .NET 9 MAUI

set -e

echo "=============================================="
echo "  DonTroc - Build AAB Signé pour Google Play"
echo "  .NET 9 MAUI - Android API 35"
echo "=============================================="
echo ""

# Configuration
PROJECT_DIR="/Users/aa1/RiderProjects/DonTroc/DonTroc"
KEYSTORE_DIR="/Users/aa1/RiderProjects/DonTroc/keystore"
OUTPUT_DIR="/Users/aa1/RiderProjects/DonTroc/output"
KEYSTORE_FILE="$KEYSTORE_DIR/dontroc-release.keystore"
KEYSTORE_ALIAS="dontroc"

# Vérifier que le keystore existe
if [ ! -f "$KEYSTORE_FILE" ]; then
    echo "❌ ERREUR: Le keystore n'existe pas: $KEYSTORE_FILE"
    exit 1
fi

# Mot de passe du keystore (peut être passé en argument ou défini ici)
KEYSTORE_PASSWORD="${1:-DonTroc2024!1007}"

# Exporter les variables d'environnement
export DONTROC_KEYSTORE_PASS="$KEYSTORE_PASSWORD"
export DONTROC_KEY_PASS="$KEYSTORE_PASSWORD"

# Créer le dossier de sortie
mkdir -p "$OUTPUT_DIR"

# Nettoyer
echo "🧹 Nettoyage des builds précédents..."
cd "$PROJECT_DIR"
rm -rf bin obj
rm -f "$OUTPUT_DIR"/*.aab

# Restaurer les packages
echo "📦 Restauration des packages NuGet..."
dotnet restore

# Compiler en Release
echo "🔨 Compilation en Release avec signature..."
dotnet publish -f net9.0-android -c Release \
    -p:AndroidPackageFormat=aab \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore="$KEYSTORE_FILE" \
    -p:AndroidSigningKeyAlias="$KEYSTORE_ALIAS" \
    -p:AndroidSigningKeyPass="$KEYSTORE_PASSWORD" \
    -p:AndroidSigningStorePass="$KEYSTORE_PASSWORD" \
    -o "$OUTPUT_DIR"

# Vérifier le résultat
AAB_FILE=$(find "$OUTPUT_DIR" -name "*.aab" -type f | head -1)

if [ -f "$AAB_FILE" ]; then
    echo ""
    echo "=============================================="
    echo "✅ BUILD RÉUSSI!"
    echo "=============================================="
    echo "📁 Fichier AAB: $AAB_FILE"
    echo "📏 Taille: $(du -h "$AAB_FILE" | cut -f1)"
    echo ""
    echo "Prochaines étapes:"
    echo "1. Téléchargez ce fichier sur Google Play Console"
    echo "2. Sélectionnez 'Test interne' ou 'Production'"
    echo "=============================================="
else
    echo ""
    echo "❌ ERREUR: Aucun fichier AAB trouvé dans $OUTPUT_DIR"
    echo "Vérifiez les erreurs de compilation ci-dessus."
    exit 1
fi


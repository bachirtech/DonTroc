#!/bin/bash

# Script de configuration pour les bibliothèques Swift Firebase
# Ce script aide à résoudre les erreurs de compatibilité Swift

echo "Configuration des bibliothèques Swift pour Firebase..."

# Vérifier la présence des bibliothèques Swift
XCODE_PATH="/Applications/Xcode.app/Contents/Developer"
SWIFT_LIB_PATH="${XCODE_PATH}/Toolchains/XcodeDefault.xctoolchain/usr/lib/swift/iphoneos"

if [ ! -d "$SWIFT_LIB_PATH" ]; then
    echo "ERREUR: Bibliothèques Swift non trouvées dans $SWIFT_LIB_PATH"
    echo "Veuillez vous assurer que Xcode est correctement installé."
    exit 1
fi

echo "Bibliothèques Swift trouvées dans: $SWIFT_LIB_PATH"

# Lister les bibliothèques Swift disponibles
echo "Bibliothèques Swift disponibles:"
ls -la "$SWIFT_LIB_PATH" | grep "libswift"

# Vérifier les bibliothèques de compatibilité spécifiques
REQUIRED_LIBS=(
    "libswiftCompatibility50.a"
    "libswiftCompatibility51.a" 
    "libswiftCompatibility56.a"
    "libswiftCompatibilityConcurrency.a"
    "libswiftCompatibilityDynamicReplacements.a"
)

echo ""
echo "Vérification des bibliothèques de compatibilité requises:"
for lib in "${REQUIRED_LIBS[@]}"; do
    if [ -f "$SWIFT_LIB_PATH/$lib" ]; then
        echo "✓ $lib trouvée"
    else
        echo "✗ $lib MANQUANTE"
    fi
done

echo ""
echo "Configuration terminée."

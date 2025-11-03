#!/bin/bash

# Script de génération du Crashlytics Build ID
# Ce script génère un build ID unique pour Firebase Crashlytics

echo "🔧 Génération du Crashlytics Build ID..."

# Créer le dossier de ressources s'il n'existe pas
ANDROID_RESOURCES_DIR="Platforms/Android/Resources"
mkdir -p "$ANDROID_RESOURCES_DIR/values"

# Générer un build ID unique basé sur la date et l'heure
BUILD_ID=$(date +"%Y%m%d%H%M%S")
BUILD_VERSION="1.0.${BUILD_ID}"

# Créer le fichier de configuration Crashlytics
cat > "$ANDROID_RESOURCES_DIR/values/crashlytics.xml" << EOF
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <!-- Firebase Crashlytics Build ID -->
    <string name="com.crashlytics.android.build_id" translatable="false">${BUILD_ID}</string>
    <string name="com.google.firebase.crashlytics.build_id" translatable="false">${BUILD_ID}</string>
    
    <!-- Configuration Crashlytics -->
    <bool name="firebase_crashlytics_collection_enabled">true</bool>
    <bool name="firebase_analytics_collection_enabled">true</bool>
</resources>
EOF

echo "✅ Build ID généré: $BUILD_ID"
echo "✅ Fichier créé: $ANDROID_RESOURCES_DIR/values/crashlytics.xml"

# Créer également un fichier mapping de build
cat > "$ANDROID_RESOURCES_DIR/crashlytics-build-properties" << EOF
version_name=$BUILD_VERSION
version_code=${BUILD_ID}
build_id=${BUILD_ID}
EOF

echo "✅ Fichier de mapping créé: $ANDROID_RESOURCES_DIR/crashlytics-build-properties"
echo "🎉 Configuration Crashlytics terminée!"

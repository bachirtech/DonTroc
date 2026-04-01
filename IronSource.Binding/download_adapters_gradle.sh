#!/bin/bash
# Script pour télécharger les adaptateurs IronSource/LevelPlay via un mini-projet Gradle
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TEMP_DIR="/tmp/ironsource_adapters_download"
OUTPUT_DIR="$SCRIPT_DIR/Jars/adapters"

echo "══════════════════════════════════════"
echo "📦 Téléchargement des adaptateurs LevelPlay"
echo "══════════════════════════════════════"

# Nettoyer et créer le répertoire temporaire
rm -rf "$TEMP_DIR"
mkdir -p "$TEMP_DIR"
mkdir -p "$OUTPUT_DIR"

# Créer settings.gradle
cat > "$TEMP_DIR/settings.gradle" << 'EOF'
pluginManagement {
    repositories {
        google()
        mavenCentral()
        gradlePluginPortal()
    }
}
dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {
        google()
        mavenCentral()
        maven { url = uri("https://android-sdk.is.com/artifactory/android-sdk") }
    }
}
rootProject.name = "adapter-downloader"
EOF

# Créer build.gradle avec les bons artifacts
cat > "$TEMP_DIR/build.gradle" << 'EOF'
plugins {
    id 'com.android.library' version '8.7.3' apply false
}

configurations {
    adapterJars
}

dependencies {
    adapterJars 'com.ironsource.adapters:admobadapter:4.3.47'
    adapterJars 'com.ironsource.adapters:unityadsadapter:4.3.46'
    adapterJars 'com.ironsource.adapters:facebookadapter:4.3.47'
    adapterJars 'com.ironsource.adapters:vungleadapter:4.3.27'
}

task copyAdapters(type: Copy) {
    from configurations.adapterJars
    into file("output")
}
EOF

cd "$TEMP_DIR"

echo "📥 Résolution des dépendances via Gradle..."
gradle copyAdapters --no-daemon 2>&1

if [ -d "$TEMP_DIR/output" ] && [ "$(ls -A $TEMP_DIR/output 2>/dev/null)" ]; then
    cp "$TEMP_DIR/output/"* "$OUTPUT_DIR/"
    echo ""
    echo "✅ Adaptateurs téléchargés avec succès :"
    ls -la "$OUTPUT_DIR/"
else
    echo "❌ Aucun adaptateur téléchargé. Le repo IronSource Maven est peut-être inaccessible."
    echo ""
    echo "📋 SOLUTION ALTERNATIVE :"
    echo "   1. Allez sur https://developers.is.com/ironsource-mobile/android/mediation-networks-android/"
    echo "   2. Pour chaque réseau (AdMob, Unity Ads, Meta, Vungle):"
    echo "      - Cliquez 'Android SDK Integration' > Download Android Adapter"
    echo "      - Placez le .aar dans: $OUTPUT_DIR/"
    echo ""
    echo "   OU utilisez le LevelPlay Integration Manager dans un projet Android Studio temporaire"
fi

# Nettoyage
rm -rf "$TEMP_DIR"
echo "══════════════════════════════════════"


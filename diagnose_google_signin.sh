#!/bin/bash

# Script de diagnostic pour Google Sign-In - Erreur ApiException 10
# ================================================================

echo "🔍 Diagnostic Google Sign-In - Erreur DEVELOPER_ERROR (code 10)"
echo "================================================================"
echo ""

# Vérifier le keystore de debug
echo "📱 1. Vérification du keystore de debug..."
if [ -f ~/.android/debug.keystore ]; then
    echo "   ✅ Keystore de debug trouvé"
    echo ""
    echo "   📋 Empreinte SHA-1 du keystore de debug:"
    keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android 2>/dev/null | grep "SHA1:" | head -1
    echo ""
else
    echo "   ❌ Keystore de debug NON TROUVÉ!"
    echo "   Le keystore de debug est généralement créé automatiquement par Android SDK"
    echo ""
fi

# Comparer avec google-services.json
echo "📱 2. Empreintes SHA-1 attendues dans google-services.json:"
echo "   - Debug:   39:C4:B4:B8:DA:6D:0D:DC:FF:7A:1C:4F:47:CC:B8:6C:14:39:E6:24"
echo "   - Release: B8:9E:FC:6B:71:2A:D4:7D:D0:28:31:BF:82:86:BE:45:56:47:72:A9"
echo ""

# Vérifier si un appareil est connecté
echo "📱 3. Vérification appareil Android connecté..."
if command -v adb &> /dev/null; then
    DEVICES=$(adb devices | grep -v "List" | grep -v "^$" | wc -l)
    if [ "$DEVICES" -gt 0 ]; then
        echo "   ✅ Appareil(s) connecté(s):"
        adb devices | grep -v "List" | grep -v "^$"
        echo ""
        
        # Vérifier si l'app est installée
        echo "📱 4. Vérification de l'application installée..."
        INSTALLED=$(adb shell pm list packages | grep "com.bachirdev.dontroc" || true)
        if [ -n "$INSTALLED" ]; then
            echo "   ✅ Application installée: $INSTALLED"
            echo ""
            echo "   📋 Informations de signature de l'APK installé:"
            adb shell "dumpsys package com.bachirdev.dontroc | grep -A 2 'Signatures:'" 2>/dev/null || echo "   (Impossible de récupérer les signatures)"
        else
            echo "   ⚠️ Application NON installée sur l'appareil"
        fi
    else
        echo "   ⚠️ Aucun appareil connecté"
    fi
else
    echo "   ⚠️ adb non trouvé dans le PATH"
fi

echo ""
echo "================================================================"
echo "🔧 SOLUTIONS POSSIBLES:"
echo "================================================================"
echo ""
echo "Si l'empreinte SHA-1 du keystore de debug est DIFFÉRENTE de celle"
echo "attendue dans google-services.json, vous avez 2 options:"
echo ""
echo "OPTION A: Ajouter la nouvelle empreinte dans Firebase Console"
echo "  1. Allez sur https://console.firebase.google.com/project/dontroc-55570/settings/general"
echo "  2. Descendez jusqu'à 'Vos applications' > 'Android'"
echo "  3. Cliquez 'Ajouter une empreinte'"
echo "  4. Collez la SHA-1 de votre keystore de debug actuel"
echo "  5. Re-téléchargez google-services.json"
echo "  6. Remplacez le fichier dans DonTroc/Platforms/Android/"
echo ""
echo "OPTION B: Nettoyer et reconstruire"
echo "  cd /Users/aa1/RiderProjects/DonTroc"
echo "  rm -rf DonTroc/bin DonTroc/obj"
echo "  dotnet build DonTroc/DonTroc.csproj -c Debug -f net9.0-android"
echo ""


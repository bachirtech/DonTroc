#!/bin/bash

# Script pour surveiller les logs AdMob en temps réel
# Filtrage intelligent des messages importants

echo "🔍 Surveillance des logs AdMob - Appuyez sur Ctrl+C pour arrêter"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Vérifier qu'un appareil est connecté
if ! adb devices | grep -q "device$"; then
    echo "❌ Aucun appareil/émulateur détecté"
    echo "💡 Démarrez un émulateur ou connectez un appareil Android"
    exit 1
fi

# Nettoyer les anciens logs
adb logcat -c

# Surveiller les logs avec filtrage
adb logcat | grep --line-buffered -E "AdMob|ADMOB|Bannière|SDK AdMob|ERROR_CODE|com.google.android.gms.ads|Ads:" | while read -r line; do
    # Colorer les messages
    if echo "$line" | grep -q "✅\|succès\|SUCCESS\|Loaded"; then
        echo -e "\033[0;32m$line\033[0m"  # Vert pour succès
    elif echo "$line" | grep -q "❌\|ERROR\|FAILED\|Exception"; then
        echo -e "\033[0;31m$line\033[0m"  # Rouge pour erreurs
    elif echo "$line" | grep -q "⏳\|Loading\|Chargement"; then
        echo -e "\033[0;33m$line\033[0m"  # Jaune pour en cours
    elif echo "$line" | grep -q "🎯\|CRÉATION\|Initialize"; then
        echo -e "\033[0;34m$line\033[0m"  # Bleu pour info
    else
        echo "$line"
    fi
done


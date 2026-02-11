#!/bin/bash

# Script pour déployer les règles Firebase
# Usage: ./deploy_firebase_rules.sh

echo "🔥 Déploiement des règles Firebase..."

# Vérifier si Firebase CLI est installé
if ! command -v firebase &> /dev/null; then
    echo "❌ Firebase CLI n'est pas installé."
    echo "   Installez-le avec: npm install -g firebase-tools"
    echo "   Ou déployez manuellement via Firebase Console:"
    echo "   1. Allez sur https://console.firebase.google.com/"
    echo "   2. Sélectionnez le projet dontroc-55570"
    echo "   3. Allez dans Realtime Database > Rules"
    echo "   4. Copiez le contenu de firebase_rules.json"
    echo "   5. Cliquez sur Publish"
    exit 1
fi

# Vérifier si connecté
firebase projects:list > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "⚠️ Vous n'êtes pas connecté à Firebase CLI."
    echo "   Exécutez: firebase login"
    exit 1
fi

# Déployer les règles
echo "📤 Déploiement des règles de la base de données..."
firebase deploy --only database:rules --project dontroc-55570

if [ $? -eq 0 ]; then
    echo "✅ Règles Firebase déployées avec succès!"
else
    echo "❌ Erreur lors du déploiement des règles."
    echo "   Essayez de déployer manuellement via Firebase Console."
fi


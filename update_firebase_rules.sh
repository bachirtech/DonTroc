#!/bin/bash

# Script pour mettre à jour les règles Firebase
echo "🔧 Mise à jour des règles Firebase pour DonTroc..."

# Vérifier si Firebase CLI est installé
if ! command -v firebase &> /dev/null; then
    echo "❌ Firebase CLI n'est pas installé."
    echo "Installez-le avec: npm install -g firebase-tools"
    exit 1
fi

# Vérifier si nous sommes connectés à Firebase
if ! firebase projects:list &> /dev/null; then
    echo "❌ Vous n'êtes pas connecté à Firebase."
    echo "Connectez-vous avec: firebase login"
    exit 1
fi

# Déployer les nouvelles règles
echo "📤 Déploiement des nouvelles règles de sécurité..."
firebase deploy --only database:rules --project dontroc-55570

if [ $? -eq 0 ]; then
    echo "✅ Règles Firebase mises à jour avec succès!"
    echo ""
    echo "🔐 Nouvelles fonctionnalités ajoutées:"
    echo "  • Validation correcte des profils utilisateur"
    echo "  • Support pour UserStats (gamification)"
    echo "  • Règles améliorées pour Favorites, Notifications et Ratings"
    echo "  • Sécurité renforcée pour toutes les collections"
    echo ""
    echo "🚀 Vous pouvez maintenant tester votre application!"
else
    echo "❌ Erreur lors du déploiement des règles Firebase"
    exit 1
fi

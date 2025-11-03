#!/bin/bash

# Script de diagnostic et correction Firebase pour DonTroc
# Ce script aide à résoudre automatiquement les problèmes de connexion Firebase

echo "🔥 DIAGNOSTIC FIREBASE DONTROC 🔥"
echo "=================================="

# Vérification de Firebase CLI
if ! command -v firebase &> /dev/null; then
    echo "❌ Firebase CLI n'est pas installé."
    echo "📦 Installation via npm..."
    npm install -g firebase-tools
fi

echo "✅ Firebase CLI détecté"

# Connexion à Firebase
echo "🔑 Vérification de l'authentification Firebase..."
firebase login --no-localhost

# Initialisation du projet (si nécessaire)
if [ ! -f "firebase.json" ]; then
    echo "📝 Initialisation du projet Firebase..."
    firebase init database --project dontroc-13246
fi

# Sauvegarde des règles actuelles
echo "💾 Sauvegarde des règles actuelles..."
firebase database:get / --project dontroc-13246 > backup_rules_$(date +%Y%m%d_%H%M%S).json

# Déploiement des règles temporaires permissives
echo "🚀 Déploiement des règles temporaires (plus permissives)..."
firebase deploy --only database --project dontroc-13246 --config firebase_rules_temp.json

echo "✅ Règles temporaires déployées!"
echo ""
echo "🎯 PROCHAINES ÉTAPES:"
echo "1. Testez votre application maintenant"
echo "2. Les annonces devraient se charger correctement"
echo "3. Une fois confirmé, vous pourrez restaurer les règles sécurisées"
echo ""
echo "⚠️  IMPORTANT: Ces règles sont temporaires et moins sécurisées"
echo "   Restaurez les règles sécurisées dès que possible avec:"
echo "   firebase deploy --only database --project dontroc-13246"
echo ""

# Test de connectivité
echo "🧪 Test de connectivité Firebase..."
curl -s "https://dontroc-13246-default-rtdb.europe-west1.firebasedatabase.app/.json" | head -c 100

echo ""
echo "🎉 Script terminé! Testez votre application maintenant."

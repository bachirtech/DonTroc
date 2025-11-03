#!/bin/bash

echo "🔍 DIAGNOSTIC FIREBASE APPROFONDI"
echo "================================="

# Test de connectivité internet
echo "📡 Test de connectivité internet..."
if ping -c 1 google.com &> /dev/null; then
    echo "✅ Connexion internet OK"
else
    echo "❌ Pas de connexion internet"
    exit 1
fi

# Test des URLs Firebase possibles
echo ""
echo "🔥 Test des URLs Firebase pour le projet dontroc-13246..."

urls=(
    "https://dontroc-13246-default-rtdb.europe-west1.firebasedatabase.app"
    "https://dontroc-13246-default-rtdb.firebaseio.com"
    "https://dontroc-13246.firebaseio.com"
    "https://dontroc-13246-default-rtdb.us-central1.firebasedatabase.app"
)

working_url=""

for url in "${urls[@]}"; do
    echo "🧪 Test: $url"
    
    # Test HTTP direct
    response=$(curl -s -o /dev/null -w "%{http_code}" "$url/.json" --max-time 10)
    
    if [ "$response" = "200" ] || [ "$response" = "401" ]; then
        echo "✅ Réponse: $response (URL accessible)"
        working_url="$url"
        break
    else
        echo "❌ Réponse: $response"
    fi
done

if [ -z "$working_url" ]; then
    echo ""
    echo "❌ PROBLÈME IDENTIFIÉ: Aucune URL Firebase accessible"
    echo ""
    echo "🔧 SOLUTIONS POSSIBLES:"
    echo "1. Votre projet Firebase n'a pas de Realtime Database"
    echo "2. La région de votre database n'est pas europe-west1"
    echo "3. Le nom du projet est incorrect"
    echo ""
    echo "📋 ÉTAPES À SUIVRE:"
    echo "1. Allez sur https://console.firebase.google.com"
    echo "2. Sélectionnez votre projet 'dontroc-13246'"
    echo "3. Dans le menu à gauche, cliquez sur 'Realtime Database'"
    echo "4. Si vous voyez 'Créer une base de données', cliquez dessus"
    echo "5. Choisissez la région 'europe-west1'"
    echo "6. Démarrez en mode test (règles permissives)"
    echo ""
    echo "🎯 Si la database existe déjà:"
    echo "1. Vérifiez l'URL dans les paramètres du projet"
    echo "2. Notez la région exacte"
    echo "3. Mise à jour du code avec la bonne URL"
else
    echo ""
    echo "✅ URL fonctionnelle trouvée: $working_url"
    echo ""
    echo "🔧 Mise à jour automatique du FirebaseService..."
    
    # Créer un fichier de configuration avec la bonne URL
    cat > firebase_config.txt << EOF
URL_FIREBASE_WORKING=$working_url
TIMESTAMP=$(date)
EOF
    
    echo "✅ Configuration sauvegardée dans firebase_config.txt"
fi

echo ""
echo "🔍 Informations supplémentaires..."

# Vérification des dépendances Firebase dans le projet
if [ -f "DonTroc/DonTroc.csproj" ]; then
    echo "📦 Vérification des packages Firebase..."
    grep -i firebase DonTroc/DonTroc.csproj || echo "⚠️  Aucun package Firebase trouvé dans .csproj"
fi

# Vérification de la configuration
if [ -f "DonTroc/Services/FirebaseService.cs" ]; then
    echo "📝 URL actuelle dans FirebaseService.cs:"
    grep -n "firebasedatabase.app" DonTroc/Services/FirebaseService.cs | head -1
fi

echo ""
echo "🎯 PROCHAINES ÉTAPES RECOMMANDÉES:"
if [ -z "$working_url" ]; then
    echo "1. Créer/vérifier la Realtime Database dans Firebase Console"
    echo "2. Noter l'URL exacte de votre database"
    echo "3. Mettre à jour le code avec la bonne URL"
else
    echo "1. Mettre à jour FirebaseService.cs avec l'URL: $working_url"
    echo "2. Déployer les règles corrigées"
    echo "3. Tester l'application"
fi

echo ""
echo "📞 Besoin d'aide? Vérifiez:"
echo "- Console Firebase: https://console.firebase.google.com"
echo "- Documentation: https://firebase.google.com/docs/database"

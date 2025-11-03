#!/bin/bash

# Script de génération du keystore pour DonTroc
# Ce script crée une clé de signature pour votre application Android

echo "🔐 Génération du keystore pour DonTroc..."

# Configuration
KEYSTORE_PATH="./keystore/dontroc-release.keystore"
KEY_ALIAS="dontroc"
VALIDITY_DAYS=10000  # ~27 ans

# Vérifier si le keystore existe déjà
if [ -f "$KEYSTORE_PATH" ]; then
    echo "⚠️  Le keystore existe déjà à $KEYSTORE_PATH"
    read -p "Voulez-vous le remplacer ? (y/N): " confirm
    if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
        echo "❌ Opération annulée"
        exit 1
    fi
    rm -f "$KEYSTORE_PATH"
fi

# Informations pour le certificat
echo ""
echo "📝 Veuillez entrer les informations pour votre certificat :"
echo "   (Appuyez sur Entrée pour utiliser les valeurs par défaut)"
echo ""

read -p "Nom complet [Bassirou Balde]: " CN
CN=${CN:-"Bassirou Balde"}

read -p "Unité organisationnelle [BachirDev]: " OU
OU=${OU:-"BachirDev"}

read -p "Organisation [BachirDev]: " O
O=${O:-"BachirDev"}

read -p "Ville [Agadir]: " L
L=${L:-"Maroc"}

read -p "État/Province [Agadir]: " ST
ST=${ST:-"agadir"}

read -p "Code pays (2 lettres) [MA]: " C
C=${C:-"MA"}

# Générer un mot de passe fort par défaut
DEFAULT_PASSWORD="DonTroc2024!$(date +%m%d)"
read -s -p "Mot de passe du keystore [$DEFAULT_PASSWORD]: " STORE_PASSWORD
echo ""
STORE_PASSWORD=${STORE_PASSWORD:-$DEFAULT_PASSWORD}

read -s -p "Mot de passe de la clé (même que le keystore recommandé) [$STORE_PASSWORD]: " KEY_PASSWORD
echo ""
KEY_PASSWORD=${KEY_PASSWORD:-$STORE_PASSWORD}

# Construire le DN (Distinguished Name)
DNAME="CN=$CN, OU=$OU, O=$O, L=$L, ST=$ST, C=$C"

echo ""
echo "🔨 Génération du keystore..."

# Générer le keystore
keytool -genkeypair \
    -alias "$KEY_ALIAS" \
    -keyalg RSA \
    -keysize 2048 \
    -validity $VALIDITY_DAYS \
    -keystore "$KEYSTORE_PATH" \
    -storepass "$STORE_PASSWORD" \
    -keypass "$KEY_PASSWORD" \
    -dname "$DNAME"

if [ $? -eq 0 ]; then
    echo "✅ Keystore généré avec succès !"
    echo ""
    echo "📄 Informations du keystore :"
    echo "   Fichier: $KEYSTORE_PATH"
    echo "   Alias: $KEY_ALIAS"
    echo "   Validité: $VALIDITY_DAYS jours (~27 ans)"
    echo ""
    
    # Créer le fichier de configuration
    cat > ./keystore/signing.properties << EOF
# Configuration de signature pour DonTroc
# ⚠️  GARDEZ CE FICHIER SÉCURISÉ ET NE LE COMMITEZ PAS !

AndroidSigningKeyStore=$KEYSTORE_PATH
AndroidSigningKeyAlias=$KEY_ALIAS
AndroidSigningStorePassword=$STORE_PASSWORD
AndroidSigningKeyPassword=$KEY_PASSWORD
EOF
    
    echo "📝 Fichier de configuration créé : ./keystore/signing.properties"
    echo ""
    echo "🛡️  IMPORTANT - Sécurité :"
    echo "   1. Sauvegardez le keystore ET les mots de passe en lieu sûr"
    echo "   2. N'ajoutez JAMAIS signing.properties à Git"
    echo "   3. Le keystore doit être identique pour toutes les versions"
    echo ""
    echo "🚀 Pour compiler en mode Release :"
    echo "   dotnet build -c Release -f net8.0-android"
    echo "   ou utilisez Visual Studio/Rider avec la configuration Release"
    
    # Ajouter au .gitignore si il existe
    if [ -f ".gitignore" ]; then
        echo "" >> .gitignore
        echo "# Keystore et configuration de signature" >> .gitignore
        echo "keystore/" >> .gitignore
        echo "*.keystore" >> .gitignore
        echo "signing.properties" >> .gitignore
        echo "✅ Ajouté au .gitignore"
    fi
    
else
    echo "❌ Erreur lors de la génération du keystore"
    exit 1
fi

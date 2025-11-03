# 🗺️ Configuration de Google Maps API - Guide Complet

## 🔑 Étape 1 : Obtenir votre clé API Google Maps

### 1.1 Créer un projet Google Cloud
1. Allez sur [Google Cloud Console](https://console.cloud.google.com/)
2. Connectez-vous avec votre compte Google
3. Cliquez sur "Nouveau projet" ou sélectionnez un projet existant
4. Donnez un nom à votre projet (ex: "DonTroc-Maps")

### 1.2 Activer l'API Google Maps
1. Dans la console Google Cloud, allez dans "APIs et services" > "Bibliothèque"
2. Recherchez "Maps SDK for Android"
3. Cliquez dessus et cliquez sur "ACTIVER"
4. Recherchez également "Geocoding API" et activez-la (pour la recherche d'adresses)

### 1.3 Créer une clé API
1. Allez dans "APIs et services" > "Identifiants"
2. Cliquez sur "CRÉER DES IDENTIFIANTS" > "Clé API"
3. Votre clé API sera générée (format : AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXX)
4. **IMPORTANT** : Copiez cette clé immédiatement !

### 1.4 Restreindre la clé API (Sécurité)
1. Cliquez sur votre clé API nouvellement créée
2. Dans "Restrictions relatives aux applications" :
   - Sélectionnez "Applications Android"
   - Ajoutez le nom de package : `com.bachirdev.dontroc`
   - Ajoutez l'empreinte SHA-1 de votre certificat de signature
3. Dans "Restrictions relatives aux API" :
   - Sélectionnez "Limiter la clé"
   - Choisissez "Maps SDK for Android" et "Geocoding API"
4. Cliquez sur "ENREGISTRER"

## 🔧 Étape 2 : Configuration dans DonTroc

### Option A : Configuration Simple (Recommandée pour le développement)

1. **Modifiez directement l'AndroidManifest.xml** :
   Remplacez `YOUR_GOOGLE_MAPS_API_KEY_HERE` par votre vraie clé dans :
   `/DonTroc/Platforms/Android/AndroidManifest.xml`

```xml
<meta-data android:name="com.google.android.geo.API_KEY" android:value="AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXX" />
```

### Option B : Configuration Sécurisée (Production)

1. **Créez le fichier de configuration** :
```bash
# Depuis le terminal, dans le dossier du projet
cd /Users/aa1/RiderProjects/DonTroc
mkdir -p keystore
echo "GOOGLE_MAPS_API_KEY=VOTRE_VRAIE_CLE_ICI" > keystore/maps.properties
```

2. **Protégez le fichier** :
```bash
# Ajoutez au .gitignore pour ne pas commit la clé
echo "keystore/maps.properties" >> .gitignore
```

## 🧪 Étape 3 : Test de la configuration

### 3.1 Vérifier la compilation
```bash
cd /Users/aa1/RiderProjects/DonTroc/DonTroc
dotnet build -f net8.0-android -c Debug
```

### 3.2 Test sur l'appareil
1. Connectez votre appareil Android ou démarrez un émulateur
2. Déployez l'application
3. Vérifiez que la carte s'affiche correctement
4. Testez la géolocalisation et la recherche d'adresses

## 🚨 Dépannage

### Erreur : "Google Maps API key not found"
- Vérifiez que la clé est correctement copiée (sans espaces)
- Assurez-vous qu'elle est entre guillemets dans le XML

### Erreur : "This API project is not authorized"
- Vérifiez que l'API Maps SDK for Android est activée
- Vérifiez que votre nom de package correspond : `com.bachirdev.dontroc`

### Carte grise ou erreurs de chargement
- Vérifiez les restrictions de votre clé API
- Assurez-vous que votre empreinte SHA-1 est correcte
- Vérifiez que la facturation est activée sur votre projet Google Cloud

## 💰 Tarification

Google Maps offre :
- **200$ de crédit gratuit par mois**
- Environ **28 000 chargements de cartes gratuits par mois**
- Largement suffisant pour le développement et les tests

## 🔐 Sécurité

**IMPORTANT** : 
- Ne commitez JAMAIS votre clé API dans Git
- Utilisez des restrictions d'API appropriées
- Surveillez l'utilisation dans Google Cloud Console
- Pour la production, utilisez le système de variables d'environnement

## 📱 Empreinte SHA-1 pour Android

Pour obtenir votre empreinte SHA-1 :

```bash
# Pour le debug (développement)
keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android -keypass android

# Pour la release (si vous avez un keystore de production)
keytool -list -v -keystore /Users/aa1/RiderProjects/DonTroc/keystore/dontroc-release.keystore -alias dontroc
```

Copiez la ligne qui commence par "SHA1:" et ajoutez-la aux restrictions de votre clé API.

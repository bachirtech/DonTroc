# Guide de Configuration Google Sign-In pour DonTroc

## Étape 1 : Configuration dans la Console Firebase

### 1.1 Activer Google Sign-In
1. Allez sur [Firebase Console](https://console.firebase.google.com)
2. Sélectionnez votre projet **dontroc-55570**
3. Dans le menu de gauche, cliquez sur **Authentication**
4. Cliquez sur l'onglet **Sign-in method**
5. Cliquez sur **Google** dans la liste des fournisseurs
6. Activez-le en basculant le switch sur **Enabled**
7. Configurez l'email du projet (votre email)
8. **IMPORTANT** : Notez le **Web client ID** qui sera généré

### 1.2 Récupérer le Web Client ID
Après avoir activé Google Sign-In :
1. Le **Web client ID** ressemble à : `XXXX.apps.googleusercontent.com`
2. Copiez cette valeur

## Étape 2 : Mettre à jour le code

### 2.1 Mettre à jour GoogleAuthService.cs
Dans le fichier `DonTroc/Platforms/Android/GoogleAuthService.cs`, remplacez :
```csharp
private const string WebClientId = "12542152309-VOTRE_WEB_CLIENT_ID.apps.googleusercontent.com";
```
Par votre vrai Web Client ID récupéré à l'étape 1.2.

## Étape 3 : Configurer SHA-1 pour Android

### 3.1 Générer l'empreinte SHA-1
Exécutez cette commande dans le terminal :
```bash
keytool -list -v -keystore keystore/dontroc-release.keystore -alias dontroc
```
Ou pour le debug :
```bash
keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android
```

### 3.2 Ajouter SHA-1 à Firebase
1. Dans Firebase Console, allez dans **Project Settings** (icône engrenage)
2. Descendez jusqu'à **Your apps** > **Android app**
3. Cliquez sur **Add fingerprint**
4. Collez l'empreinte SHA-1 (et SHA-256 si disponible)
5. Cliquez sur **Save**

### 3.3 Télécharger le nouveau google-services.json
1. Après avoir ajouté les empreintes, cliquez sur **Download google-services.json**
2. Remplacez le fichier existant dans `DonTroc/Platforms/Android/google-services.json`

## Étape 4 : Vérification

Le fichier `google-services.json` devrait maintenant contenir une section `oauth_client` comme ceci :
```json
"oauth_client": [
  {
    "client_id": "XXXX.apps.googleusercontent.com",
    "client_type": 3
  },
  {
    "client_id": "XXXX.apps.googleusercontent.com",
    "client_type": 1,
    "android_info": {
      "package_name": "com.bachirdev.dontroc",
      "certificate_hash": "VOTRE_SHA1"
    }
  }
]
```

## Dépannage

### Erreur "DEVELOPER_ERROR"
- Vérifiez que le SHA-1 est correctement configuré dans Firebase
- Assurez-vous que le Web Client ID est correct dans le code

### Erreur "Sign in failed"
- Vérifiez que Google Sign-In est activé dans Firebase Authentication
- Vérifiez que le package name correspond (`com.bachirdev.dontroc`)

### L'écran de connexion Google ne s'affiche pas
- Vérifiez les permissions dans AndroidManifest.xml
- Vérifiez que le package `Xamarin.GooglePlayServices.Auth` est bien installé

## Permissions Android (déjà configurées)

Le fichier `AndroidManifest.xml` devrait contenir :
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

Ces permissions sont généralement déjà présentes pour Firebase.


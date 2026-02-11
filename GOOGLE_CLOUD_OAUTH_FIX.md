# 🔧 Résolution de l'erreur ApiException: 10 (DEVELOPER_ERROR)

## Situation actuelle
- ✅ SHA-1 correcte : `39:C4:B4:B8:DA:6D:0D:DC:FF:7A:1C:4F:47:CC:B8:6C:14:39:E6:24`
- ✅ google-services.json contient les oauth_client
- ✅ Google Sign-In activé dans Firebase Authentication
- ❌ Erreur ApiException: 10 persiste

## Cause probable

L'erreur `ApiException: 10` avec une configuration correcte indique généralement un problème avec **Google Cloud Console** (pas Firebase).

## 🔧 Solution : Configurer l'écran de consentement OAuth

### Étape 1 : Ouvrir Google Cloud Console

1. Allez sur : https://console.cloud.google.com/apis/credentials/consent?project=dontroc-55570
2. Connectez-vous avec le même compte que Firebase

### Étape 2 : Configurer l'écran de consentement

Si l'écran de consentement n'est pas configuré :

1. Sélectionnez **External** (pour les utilisateurs externes)
2. Cliquez **Créer**
3. Remplissez les champs obligatoires :
   - **Nom de l'application** : DonTroc
   - **Email d'assistance utilisateur** : votre email
   - **Adresses e-mail du développeur** : votre email
4. Cliquez **Enregistrer et continuer**
5. Dans **Scopes**, ajoutez :
   - `email`
   - `profile`
   - `openid`
6. Cliquez **Enregistrer et continuer**
7. Dans **Test users**, ajoutez votre email Google pour les tests
8. Cliquez **Enregistrer et continuer**

### Étape 3 : Vérifier les identifiants OAuth

1. Allez sur : https://console.cloud.google.com/apis/credentials?project=dontroc-55570
2. Vérifiez que vous avez ces clients OAuth 2.0 :
   - **Web client** : `12542152309-asqvk30n6eukuio6tbg8nm93vq4h2lv6.apps.googleusercontent.com`
   - **Android client (debug)** : `12542152309-pesha17s0htg50ub38496keevpsinci0.apps.googleusercontent.com`
   - **Android client (release)** : `12542152309-g9kn444qi5ofcn8270bt479hg3se9kl7.apps.googleusercontent.com`

### Étape 4 : Vérifier le client Android debug

1. Cliquez sur le client Android pour debug (`...pesha17s0htg50ub38496keevpsinci0...`)
2. Vérifiez que :
   - **Package name** : `com.bachirdev.dontroc`
   - **SHA-1 certificate fingerprint** : `39:C4:B4:B8:DA:6D:0D:DC:FF:7A:1C:4F:47:CC:B8:6C:14:39:E6:24`

### Étape 5 : Publier l'application en mode test (si nécessaire)

1. Retournez sur l'écran de consentement OAuth
2. Si l'application est en mode **Testing**, vérifiez que votre compte Google est dans la liste des **Test users**
3. Ou publiez l'application (cliquez **Publier l'application**)

## 🔄 Après ces modifications

1. Attendez quelques minutes (les changements peuvent prendre du temps à se propager)
2. Nettoyez et recompilez l'application :
   ```bash
   cd /Users/aa1/RiderProjects/DonTroc
   rm -rf DonTroc/bin DonTroc/obj
   dotnet build DonTroc/DonTroc.csproj -c Debug -f net9.0-android
   ```
3. Réinstallez l'APK sur votre appareil
4. Réessayez la connexion Google

## ⚠️ Points importants

- Le projet Google Cloud doit être le **même** que celui lié à Firebase (dontroc-55570)
- L'écran de consentement OAuth **DOIT** être configuré
- Si en mode "Testing", votre email **DOIT** être dans la liste des test users


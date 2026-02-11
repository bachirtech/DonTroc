# 🔍 DIAGNOSTIC GOOGLE SIGN-IN - ERREUR CODE 10

## État actuel vérifié ✅

| Élément | Valeur | Status |
|---------|--------|--------|
| SHA-1 Debug | `39:C4:B4:B8:DA:6D:0D:DC:FF:7A:1C:4F:47:CC:B8:6C:14:39:E6:24` | ✅ Correspond |
| Package Name | `com.bachirdev.dontroc` | ✅ Correct |
| Web Client ID | `12542152309-asqvk30n6eukuio6tbg8nm93vq4h2lv6.apps.googleusercontent.com` | ✅ Configuré |
| google-services.json | Présent avec bon SHA-1 | ✅ OK |
| CrossFirebaseSettings | Mis à jour avec googleRequestIdToken | ✅ OK |

---

## ⚠️ Actions à vérifier dans Google Cloud Console

### 1. Vérifier que le Web Client est bien créé

1. Allez sur https://console.cloud.google.com/apis/credentials
2. Sélectionnez le projet **dontroc-55570**
3. Cherchez un client OAuth de type **"Web application"** avec l'ID :
   ```
   12542152309-asqvk30n6eukuio6tbg8nm93vq4h2lv6.apps.googleusercontent.com
   ```
4. **S'il n'existe pas**, créez-le :
   - Cliquez **+ CREATE CREDENTIALS** > **OAuth client ID**
   - Type : **Web application**
   - Nom : `DonTroc Web Client`
   - Laissez les "Authorized origins" et "Authorized redirect URIs" vides
   - Cliquez **CREATE**

### 2. Vérifier le client Android

1. Dans la même page Credentials, cherchez un client OAuth de type **"Android"**
2. Vérifiez qu'il a ces valeurs EXACTES :
   - Package name : `com.bachirdev.dontroc`
   - SHA-1 : `39:C4:B4:B8:DA:6D:0D:DC:FF:7A:1C:4F:47:CC:B8:6C:14:39:E6:24`

### 3. Vérifier l'écran de consentement OAuth

1. Allez sur https://console.cloud.google.com/apis/credentials/consent
2. Vérifiez le **Publishing status** :
   - ✅ **"In production"** = Tout le monde peut se connecter
   - ⚠️ **"Testing"** = Seuls les utilisateurs de test peuvent se connecter

3. Si vous êtes en **"Testing"** :
   - Descendez jusqu'à **"Test users"**
   - Cliquez **+ ADD USERS**
   - Ajoutez votre adresse Gmail (celle que vous utilisez pour tester)
   - Sauvegardez

### 4. Vérifier Firebase Authentication

1. Allez sur https://console.firebase.google.com/project/dontroc-55570/authentication/providers
2. Vérifiez que **Google** est **activé** (bouton bleu)
3. Cliquez sur **Google** pour voir les détails
4. Vérifiez que le **Web client ID** affiché correspond à :
   ```
   12542152309-asqvk30n6eukuio6tbg8nm93vq4h2lv6.apps.googleusercontent.com
   ```

---

## 🔄 Après les modifications

Après avoir fait les vérifications ci-dessus :

1. **Téléchargez un nouveau google-services.json** depuis Firebase Console
2. Remplacez le fichier existant dans : `/DonTroc/Platforms/Android/google-services.json`
3. **Nettoyez et rebuiltez** le projet :
   ```bash
   cd /Users/aa1/RiderProjects/DonTroc
   dotnet clean
   dotnet build -c Debug
   ```
4. Réinstallez l'application sur votre appareil

---

## 🚨 Cause la plus fréquente

**L'erreur code 10 en mode debug avec le bon SHA-1 est presque toujours causée par :**

1. Le compte Gmail de test n'est PAS dans la liste des "Test users" dans Google Cloud Console
2. OU le projet OAuth n'est pas en mode "Production"

**Solution rapide :**
- Ajoutez votre Gmail dans les Test users ET passez en Production

---

## 📝 Informations pour le support

Si le problème persiste, voici les informations à fournir :

- Project ID Firebase : `dontroc-55570`
- Package Android : `com.bachirdev.dontroc`
- Web Client ID : `12542152309-asqvk30n6eukuio6tbg8nm93vq4h2lv6.apps.googleusercontent.com`
- SHA-1 Debug : `39:C4:B4:B8:DA:6D:0D:DC:FF:7A:1C:4F:47:CC:B8:6C:14:39:E6:24`


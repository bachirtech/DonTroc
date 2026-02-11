# 🔧 Résolution du problème Google Sign-In

## ✅ Configuration vérifiée

Le fichier `google-services.json` est maintenant **correctement configuré** avec les `oauth_client` :

- ✅ OAuth client pour Release (SHA-1: `b89efc6b712ad47dd02831bf8286be45564772a9`)
- ✅ OAuth client pour Debug (SHA-1: `39c4b4b8da6d0ddcff7a1c4f47ccb86c1439e624`)
- ✅ Web Client ID: `12542152309-asqvk30n6eukuio6tbg8nm93vq4h2lv6.apps.googleusercontent.com`

## 🔍 Débogage

### Voir les logs en temps réel

Connectez votre appareil et exécutez :
```bash
adb logcat | grep -E "(GoogleAuthService|AuthService|MainActivity)"
```

### Messages à surveiller

| Message | Signification |
|---------|---------------|
| `🔵 [GoogleAuthService] Début SignInAsync` | Le processus démarre correctement |
| `🔵 [GoogleAuthService] Lancement de l'intent Google Sign-In...` | L'intent Google est lancé |
| `🔵 [MainActivity] Google Sign-In result reçu` | Le résultat est reçu |
| `✅ [GoogleAuthService] Token obtenu` | Token Google récupéré avec succès |
| `❌ [GoogleAuthService] IdToken est NULL ou VIDE!` | **Problème de configuration** |

## 🚨 Erreurs courantes et solutions

### Erreur: "IdToken est NULL ou VIDE"

**Cause:** Le Web Client ID ne correspond pas ou Google Sign-In n'est pas activé dans Firebase.

**Solution:**
1. Allez sur [Firebase Console > Authentication > Sign-in method](https://console.firebase.google.com/project/dontroc-55570/authentication/providers)
2. Vérifiez que **Google** est **activé**
3. Vérifiez que le Web Client ID affiché correspond à `12542152309-asqvk30n6eukuio6tbg8nm93vq4h2lv6.apps.googleusercontent.com`

### Erreur: "task.IsSuccessful = false"

**Cause:** Problème avec les empreintes SHA-1 ou configuration Firebase.

**Solution:**
1. Vérifiez les empreintes SHA-1 dans Firebase Console
2. Re-téléchargez `google-services.json`
3. Nettoyez et recompilez le projet

### Erreur: "GoogleAuthService est NULL"

**Cause:** Le service n'est pas correctement injecté.

**Solution:**
1. Vérifiez que `MauiProgram.cs` contient bien l'enregistrement du service
2. Nettoyez le projet (`dotnet clean`)
3. Recompilez

## 📝 Vérifications Firebase Console

1. **Authentication > Sign-in method > Google**
   - [ ] Google est activé (switch ON)
   - [ ] Email de support configuré

2. **Project Settings > Your apps > Android**
   - [ ] Package name: `com.bachirdev.dontroc`
   - [ ] SHA-1 Debug ajouté: `39:C4:B4:B8:DA:6D:0D:DC:FF:7A:1C:4F:47:CC:B8:6C:14:39:E6:24`
   - [ ] SHA-1 Release ajouté: `B8:9E:FC:6B:71:2A:D4:7D:D0:28:31:BF:82:86:BE:45:56:47:72:A9`

## 🔨 Commandes utiles

```bash
# Compiler le projet
dotnet build DonTroc/DonTroc.csproj -c Debug -f net9.0-android

# Nettoyer et recompiler
dotnet clean && dotnet build DonTroc/DonTroc.csproj -c Debug -f net9.0-android

# Voir les logs Android
adb logcat | grep -E "(GoogleAuthService|AuthService|MainActivity)"

# Vérifier les empreintes SHA-1
keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android | grep SHA1
```


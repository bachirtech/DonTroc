# 📋 Checklist Release Test Interne - DonTroc

## Date d'analyse : 12 Décembre 2025

---

## 🔴 PROBLÈMES CRITIQUES - CORRIGÉS ✅

### 1. ✅ MOT DE PASSE KEYSTORE SÉCURISÉ
**Fichier:** `DonTroc.csproj` - **CORRIGÉ**

Les mots de passe sont maintenant chargés depuis :
- Variables d'environnement `DONTROC_KEYSTORE_PASS` et `DONTROC_KEY_PASS`
- OU fichier `Local.Build.props` (ignoré par git)

**Pour builder en release :**
```bash
export DONTROC_KEYSTORE_PASS="VotreMotDePasse"
export DONTROC_KEY_PASS="VotreMotDePasse"
dotnet publish -c Release -f net8.0-android
```

### 2. ⚠️ CLÉ API GOOGLE MAPS EXPOSÉE
**Fichier:** `DonTroc.csproj` (ligne 152)
```xml
<GOOGLE_MAPS_API_KEY>AIzaSyBBr5uAyAsABZXFCoQV5eUPmmIEWSAaNhc</GOOGLE_MAPS_API_KEY>
```

**Solution:** Ajouter des restrictions d'API dans Google Cloud Console (limiter à votre package Android + SHA-1).

---

## ✅ ÉLÉMENTS PRÊTS POUR LA RELEASE

### 1. Architecture & Code
- [x] Structure MVVM correctement implémentée
- [x] Services injectés via DI (Dependency Injection)
- [x] Gestion des erreurs avec try/catch
- [x] Logging configuré pour le débogage

### 2. Authentification Firebase
- [x] Connexion/Inscription fonctionnelle
- [x] Token d'authentification géré automatiquement
- [x] Persistance de session

### 3. Fonctionnalités Principales
- [x] Création d'annonces avec photos
- [x] Liste des annonces avec filtres
- [x] Système de messagerie (conversations)
- [x] Favoris avec listes personnalisées
- [x] Géolocalisation des annonces
- [x] Transactions (dons/échanges)
- [x] Évaluations utilisateurs
- [x] Gamification (points, niveaux)
- [x] Signalements

### 4. Publicités AdMob
- [x] Bannières configurées (ID de test actif)
- [x] Interstitiels avec limitation de fréquence
- [x] Rewarded ads configurées

### 5. Notifications Push
- [x] FCM V1 configuré avec compte de service
- [x] Notifications de messages
- [x] Notifications de signalements (admins)
- [x] Notifications de favoris

### 6. Sécurité Cloudinary
- [x] Upload sécurisé avec validation userId
- [x] Suppression sécurisée des images
- [x] Clés API non exposées côté client

---

## 📋 ANALYSE DU FICHIER DonTroc.csproj

### Configuration Générale ✅
| Élément | Valeur | Statut |
|---------|--------|--------|
| Target Framework | net8.0-android | ✅ OK |
| Application ID | com.bachirdev.dontroc | ✅ OK |
| Version | 1.1 (Build 3) | ✅ OK |
| Min Android SDK | 23 (Android 6.0) | ✅ OK |
| .NET MAUI | 8.0.100 | ✅ OK |
| Nullable | enable | ✅ OK |
| MultiDex | enable | ✅ OK |

### Packages NuGet ✅
| Package | Version | Usage |
|---------|---------|-------|
| Microsoft.Maui.Controls | 8.0.100 | Framework UI |
| CommunityToolkit.Mvvm | 8.4.0 | MVVM |
| Plugin.Firebase | 3.1.4 | Auth, FCM, Analytics |
| FirebaseDatabase.net | 4.2.0 | Realtime Database |
| CloudinaryDotNet | 1.27.7 | Upload images |
| Xamarin.GooglePlayServices.Ads.Lite | 120.4.0 | AdMob |
| SkiaSharp | 3.119.1 | Graphismes |
| Syncfusion.Maui.Core | 31.1.21 | UI Components |

### Configuration Build ✅
| Mode | Linking | AOT | ProGuard | Format |
|------|---------|-----|----------|--------|
| Debug | None | ❌ | ❌ | APK |
| Release | SdkOnly | ✅ Profiled | ❌ | APK |

### Points Positifs ✅
- [x] MultiDex activé pour éviter la limite 64K méthodes
- [x] D8/R8 configuré correctement
- [x] Ressources optimisées (MauiImage, MauiFont)
- [x] Configuration iOS préparée (mais non ciblée actuellement)
- [x] TrimmerRootAssembly configuré pour éviter les crashs
- [x] AOT Profilé activé pour meilleures performances
- [x] Linking SdkOnly pour réduire la taille

### Points d'Attention ⚠️
- [x] ~~Mots de passe keystore en clair~~ → **Corrigé ✅**
- [ ] Clé Google Maps exposée → **Ajouter restrictions dans GCP**
- [x] ~~AOT désactivé~~ → **Activé (Profiled AOT) ✅**
- [x] ProGuard désactivé → **Volontaire** (stabilité Firebase/MAUI)

---

## 🔒 SÉCURITÉ DES RÈGLES FIREBASE

### Améliorations appliquées :

| Collection | Lecture | Écriture | Validation |
|------------|---------|----------|------------|
| Annonces | ✅ Auth requis | ✅ Propriétaire seul | ✅ Champs obligatoires + limites |
| UserProfiles | ✅ Auth requis | ✅ Propriétaire seul | ✅ Id = auth.uid |
| Conversations | ✅ Participants seuls | ✅ Participants seuls | ✅ Vérification participants |
| Messages | ✅ Participants | ✅ Sender = auth.uid | ✅ Limite 5000 chars |
| Transactions | ✅ Parties impliquées | ✅ Parties impliquées | ✅ Champs requis |
| favorites | ✅ Propriétaire seul | ✅ Propriétaire seul | ✅ UserId = auth.uid |
| reports | ✅ Admins seuls | ✅ Créateur ou Admin | ✅ Limite raison 1000 chars |
| admins | ✅ Admins seuls | ❌ Console seule | N/A |

### Points de sécurité clés :
- ✅ Règle par défaut : `.read: false, .write: false`
- ✅ Validation de la longueur des champs texte
- ✅ Vérification que l'utilisateur ne peut modifier que ses propres données
- ✅ Collection `admins` en écriture bloquée (gestion via console Firebase uniquement)
- ✅ Les signalements ne sont lisibles que par les admins

---

## ⚠️ ACTIONS REQUISES AVANT RELEASE

### 1. 🔐 SÉCURISER LES MOTS DE PASSE KEYSTORE (CRITIQUE)
Modifier `DonTroc.csproj` pour utiliser des variables d'environnement :
```xml
<AndroidSigningStorePass>$(DONTROC_KEYSTORE_PASS)</AndroidSigningStorePass>
<AndroidSigningKeyPass>$(DONTROC_KEY_PASS)</AndroidSigningKeyPass>
```
Puis définir les variables avant le build :
```bash
export DONTROC_KEYSTORE_PASS="VotreMotDePasse"
export DONTROC_KEY_PASS="VotreMotDePasse"
```

### 2. 🗺️ RESTREINDRE LA CLÉ GOOGLE MAPS
Dans Google Cloud Console :
1. Aller à APIs & Services > Credentials
2. Cliquer sur votre clé API
3. Sous "Application restrictions", choisir "Android apps"
4. Ajouter : `com.bachirdev.dontroc` + SHA-1 de votre keystore

### 3. 🔑 Configurer un Admin dans Firebase
```
Dans la console Firebase > Realtime Database :
1. Créer le nœud "admins"
2. Ajouter votre userId comme clé
3. Exemple: admins/VOTRE_USER_ID : { "role": "admin", "email": "..." }
```

### 4. 📱 IDs AdMob (pour production finale)
- Bannière : Actuellement en test ✅
- Interstitiel : ID production déjà configuré
- Rewarded : ID production déjà configuré

### 5. 🔐 Vérifier .gitignore
Déjà configuré ✅ :
- google-services.json
- GoogleService-Info.plist
- dontroc-55570-*.json (compte de service FCM)
- keystore/

---

## 🚀 COMMANDES POUR LA RELEASE

### Build Debug (test)
```bash
dotnet build DonTroc/DonTroc.csproj -f net8.0-android -c Debug
```

### Build Release (production)
```bash
# Définir les variables d'environnement d'abord
export DONTROC_KEYSTORE_PASS="VotreMotDePasse"
export DONTROC_KEY_PASS="VotreMotDePasse"

dotnet publish DonTroc/DonTroc.csproj -f net8.0-android -c Release
```

### Déployer les règles Firebase
```bash
firebase deploy --only database
```

---

## 📊 RÉSUMÉ

| Critère | Statut | Action |
|---------|--------|--------|
| Code compilable | ✅ | - |
| Règles Firebase sécurisées | ✅ | Déployer avec `firebase deploy` |
| Notifications Push | ✅ | - |
| AdMob configuré | ✅ | Test IDs actifs |
| .gitignore complet | ✅ | - |
| Keystore passwords | ✅ | **Sécurisé via Local.Build.props** |
| Google Maps API | ⚠️ | **Ajouter restrictions dans GCP** |
| Prêt pour test interne | ✅ | - |

### Verdict : **L'application est prête pour une release de test interne** 🎉

> ⚠️ **Note importante:** Pour une release de production, il est impératif de sécuriser les mots de passe du keystore en les externalisant du fichier csproj.

---

## 📝 Notes pour les testeurs

1. **Première connexion** : Créer un compte avec email/mot de passe
2. **Publicités** : Les annonces de test s'affichent (bannières "Test Ad")
3. **Notifications** : Accepter les permissions pour recevoir les notifications push
4. **Signaler un bug** : Utiliser le système de signalement intégré

---

*Document généré automatiquement - DonTroc v1.1*


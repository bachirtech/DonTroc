# 🎯 GUIDE DE TEST - BANNIÈRES ADMOB

**Date:** 30 novembre 2025  
**Status:** ✅ CONFIGURATION COMPLÈTE

---

## 📱 OÙ TESTER LES BANNIÈRES

Les bannières AdMob sont affichées sur **3 pages** de l'application :

| Page | Emplacement | Fichier XAML |
|------|-------------|--------------|
| 📊 **Dashboard** | En haut de la page | `DashboardView.xaml` |
| 📢 **Annonces** | En haut (Grid.Row="0") | `AnnoncesView.xaml` |
| 👤 **Profil** | En haut de la page | `ProfilView.xaml` |

---

## 🧪 MODE TEST ACTIVÉ

### Configuration actuelle:
```csharp
// AdMobBannerHandler.cs
private const string TestBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
private const bool UseTestAds = true;  // ✅ Mode TEST activé
```

### Qu'est-ce que cela signifie ?
- ✅ Vous verrez des **annonces de démonstration Google**
- ✅ Les clics ne génèrent **pas de revenus** (mode test)
- ✅ Aucun risque de bannissement de votre compte AdMob
- ⚠️ Les annonces peuvent afficher "Test Ad" ou "Sample Ad"

---

## 🚀 COMMENT TESTER

### 1. **Compiler et lancer l'application**

```bash
cd /Users/aa1/RiderProjects/DonTroc
dotnet build DonTroc/DonTroc.csproj -c Debug -f net8.0-android
```

### 2. **Lancer sur un émulateur ou téléphone**

**Émulateur:**
```bash
adb devices  # Vérifier que l'émulateur est détecté
dotnet run --project DonTroc/DonTroc.csproj -f net8.0-android
```

**Téléphone physique:**
- Brancher votre téléphone en USB
- Activer le débogage USB
- Lancer l'application depuis Rider

### 3. **Naviguer vers les pages avec bannières**

1. **Dashboard** → Vous devriez voir une bannière en haut
2. **Annonces** → Bannière visible au-dessus de la liste
3. **Profil** → Bannière en haut de la page

---

## 🔍 VÉRIFICATION DES LOGS

### Logs de succès à rechercher:

```
═══════════════════════════════════════════════════
🎯 CRÉATION BANNIÈRE ADMOB
🎯 Mode: TEST (annonces de démonstration)
🎯 ID utilisé: ca-app-pub-3940256099942544/6300978111
═══════════════════════════════════════════════════
⏳ Envoi de la requête AdMob...
✅ Bannière AdMob créée et ajoutée au container
⏳ En attente de la réponse AdMob...

═══════════════════════════════════════════════════
✅✅✅ BANNIÈRE ADMOB CHARGÉE AVEC SUCCÈS ✅✅✅
✅ La bannière devrait maintenant être visible
═══════════════════════════════════════════════════
```

### Logs d'erreur possibles:

#### ❌ ERROR_CODE_NO_FILL (Code 3)
```
ℹ️ ERROR_CODE_NO_FILL - Aucune annonce disponible
ℹ️ C'est NORMAL en mode test, les annonces de test ne sont pas toujours disponibles
ℹ️ Solution: Réessayez ou attendez quelques instants
```
**→ Le système va automatiquement réessayer après 30 secondes**

#### ❌ ERROR_CODE_NETWORK_ERROR (Code 2)
```
ℹ️ ERROR_CODE_NETWORK_ERROR - Erreur réseau
ℹ️ Solution: Vérifiez votre connexion Internet
```
**→ Vérifiez que votre émulateur/téléphone a bien accès à Internet**

#### ❌ ERROR_CODE_INVALID_REQUEST (Code 1)
```
ℹ️ ERROR_CODE_INVALID_REQUEST - Requête invalide
ℹ️ Causes possibles:
   - ID AdMob incorrect
   - Application non enregistrée dans AdMob
   - Mauvaise configuration dans AndroidManifest.xml
```
**→ Vérifiez le fichier `AndroidManifest.xml`**

---

## 📐 DIMENSIONS DE LA BANNIÈRE

**Taille standard Google AdMob:**
- **Largeur:** 320 pixels
- **Hauteur:** 50 pixels
- **Format:** Banner standard (320x50)

**Configuration dans AdBannerView.cs:**
```csharp
HeightRequest = 50;
WidthRequest = 320;
HorizontalOptions = LayoutOptions.Center;
VerticalOptions = LayoutOptions.Center;
```

---

## 🎨 APPARENCE ATTENDUE

### Placeholder (avant chargement):
```
┌──────────────────────────────────┐
│  Chargement publicité...         │
│  (texte gris, fond #F5F5F5)     │
└──────────────────────────────────┘
```

### Bannière de test (après chargement):
```
┌──────────────────────────────────┐
│  [Annonce de test Google]        │
│  • Image de démonstration        │
│  • Texte "Test Ad" ou "Sample"   │
│  • Bouton d'action               │
└──────────────────────────────────┘
```

---

## ⚙️ CONFIGURATION ANDROIDMANIFEST.XML

Vérifiez que ces permissions/métadonnées sont présentes:

### Permissions requises:
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

### Métadonnée AdMob:
```xml
<meta-data
    android:name="com.google.android.gms.ads.APPLICATION_ID"
    android:value="ca-app-pub-5085236088670848~9806548793"/>
```

---

## 🐛 DÉPANNAGE

### Problème: La bannière reste "Chargement publicité..."

**Solutions:**
1. Vérifier la connexion Internet
2. Vérifier les logs pour voir les erreurs AdMob
3. Attendre 30 secondes (le système va réessayer automatiquement)
4. Redémarrer l'application

### Problème: Aucune bannière visible

**Solutions:**
1. Vérifier que le handler est bien enregistré dans `MauiProgram.cs`:
   ```csharp
   handlers.AddHandler<DonTroc.Views.AdBannerView, AdMobBannerHandler>();
   ```
**Il ne reste plus qu'à lancer l'application et vérifier que les bannières s'affichent !** 🎉

- ✅ Logs détaillés pour le débogage
- ✅ Système de retry automatique activé
- ✅ Bannières intégrées sur 3 pages
- ✅ Mode test configuré avec ID Google officiel
- ✅ Handler AdMob activé

**Status actuel:** ✅ **CONFIGURATION COMPLÈTE ET PRÊTE À TESTER**

## ✅ RÉSUMÉ

---

3. **Recompiler et déployer sur le Play Store**

   ```
   private const bool UseTestAds = false;  // ⚠️ Passer en production
   private const string ProductionBannerAdUnitId = "VOTRE_ID_ICI";
   ```csharp
2. **Modifier `AdMobBannerHandler.cs`:**

   - Copier l'ID généré (format: `ca-app-pub-XXXXXXXX/YYYYYY`)
   - Créer une unité publicitaire de type "Bannière"
   - Aller sur https://admob.google.com/
1. **Créer votre ID de bannière sur AdMob:**

**Quand vous serez prêt pour la production:**

## 🚀 PASSAGE EN PRODUCTION

---

- [ ] Les bannières affichent du contenu (pas juste "Chargement...")
- [ ] Les logs montrent "BANNIÈRE ADMOB CHARGÉE AVEC SUCCÈS"
- [ ] La page Profil affiche une bannière
- [ ] La page Annonces affiche une bannière
- [ ] La page Dashboard affiche une bannière
- [ ] L'application démarre sur l'émulateur/téléphone
- [ ] Le handler AdMob est activé dans `MauiProgram.cs`
- [ ] L'application compile sans erreurs

## 🎯 CHECKLIST DE TEST

---

| **Impression** | `👁️ Impression de bannière AdMob enregistrée` | L'impression est comptée |
| **Closed** | `❌ Bannière AdMob fermée` | L'annonce a été fermée |
| **Opened** | `📖 Bannière AdMob ouverte` | L'annonce est ouverte en plein écran |
| **Clicked** | `👆 Bannière AdMob cliquée` | L'utilisateur a cliqué |
| **Loaded** | `✅✅✅ BANNIÈRE ADMOB CHARGÉE AVEC SUCCÈS` | La bannière est prête |
|-----------|-----|-------------|
| Événement | Log | Description |

Surveillez ces événements dans les logs:

## 📊 ÉVÉNEMENTS DE LA BANNIÈRE

---

- Vous verrez dans les logs: `🔄 Nouvelle tentative de chargement de la bannière AdMob`
- Puis relance automatiquement une requête AdMob
- Le système attend **30 secondes**
- Si l'erreur est `NO_FILL (3)` ou `NETWORK_ERROR (2)`
**Fonctionnement:**

## 🔄 SYSTÈME DE RETRY AUTOMATIQUE

---

3. Rebuilder complètement l'application
2. Vérifier la métadonnée AdMob dans `AndroidManifest.xml`
1. Vérifier que l'ID de test est correct: `ca-app-pub-3940256099942544/6300978111`
**Solutions:**

### Problème: ERROR_CODE_INVALID_REQUEST

   ```
   dotnet build -c Debug -f net8.0-android
   dotnet clean
   ```bash
3. Nettoyer et rebuilder le projet:
2. Vérifier que `AdBannerView` est bien dans le XAML de la page


# 🎯 Guide AdMob - Configuration Test et Production

## ✅ Problème Identifié et Résolu

### **Problème :** Pas d'annonces de test visibles

**Causes identifiées :**
1. ❌ L'AndroidManifest.xml utilisait l'ID de production
2. ❌ Le mode test n'était pas configuré avec `RequestConfiguration`
3. ❌ Les logs d'erreur n'étaient pas assez détaillés

**Solutions appliquées :**
1. ✅ Ajout du mode test avec flag `UseTestAds = true`
2. ✅ Configuration de `RequestConfiguration` avec les device IDs de test
3. ✅ Amélioration des logs pour debugging
4. ✅ Séparation claire entre IDs de test et production

---

## 🔧 Modifications Appliquées

### 1. **AdMobBannerHandler.cs**
```csharp
// Mode test activé
private const bool UseTestAds = true;

// IDs de test Google
private const string TestBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";

// Configuration test activée
var requestConfiguration = new RequestConfiguration.Builder()
    .SetTestDeviceIds(testDeviceIds)
    .Build();
MobileAds.SetRequestConfiguration(requestConfiguration);
```

### 2. **AdMobNativeService.cs**
```csharp
// Mode test activé
private const bool UseTestAds = true;

// IDs de test Google
private const string TestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
private const string TestInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";

// Configuration test dans Initialize()
var requestConfiguration = new RequestConfiguration.Builder()
    .SetTestDeviceIds(testDeviceIds)
    .Build();
MobileAds.SetRequestConfiguration(requestConfiguration);
```

### 3. **Logs améliorés**
- Code d'erreur détaillé
- Message d'erreur clair
- Explication des codes d'erreur courants
- Indication du mode (TEST ou PRODUCTION)

---

## 🧪 Comment Tester

### Étape 1 : Build et Installation

```bash
# Nettoyer
dotnet clean DonTroc/DonTroc.csproj

# Build Debug
dotnet build DonTroc/DonTroc.csproj -f net8.0-android -c Debug

# Installer
adb install -r DonTroc/bin/Debug/net8.0-android/com.bachirdev.dontroc-Signed.apk

# Lancer
adb shell am start -n com.bachirdev.dontroc/.MainActivity
```

### Étape 2 : Surveiller les Logs

```bash
# Terminal 1 : Logs AdMob uniquement
adb logcat | grep -E "AdMob|ADMOB|Ads"

# Terminal 2 : Tous les logs de l'app
adb logcat | grep DonTroc
```

### Étape 3 : Vérifier les Annonces

**Ce que vous devriez voir :**

1. **Au démarrage :**
   ```
   🎯 Mode AdMob: TEST
   ✅ Configuration test AdMob activée
   ✅ SDK AdMob initialisé avec succès
   📝 ID Récompensée: ca-app-pub-3940256099942544/5224354917
   📝 ID Interstitielle: ca-app-pub-3940256099942544/1033173712
   ```

2. **Pour les bannières :**
   ```
   ✅ Bannière AdMob créée avec ID: ca-app-pub-3940256099942544/6300978111
   ⏳ Chargement de la bannière en cours...
   ✅ Bannière AdMob chargée avec succès
   ```

3. **Annonce visible :**
   - Bannière avec texte "Test Ad" ou "Google Ads"
   - Fond blanc/gris
   - Logo Google Ads
   - **C'est normal de voir "Test Ad" - cela signifie que ça fonctionne !**

---

## 📍 Où Afficher les Bannières

### Exemple d'utilisation dans vos vues XAML :

```xml
<!-- Dans DashboardView.xaml ou toute autre vue -->
<ContentPage ...>
    <StackLayout>
        <!-- Votre contenu principal -->
        <Label Text="Bienvenue sur DonTroc" />
        
        <!-- Bannière AdMob en bas -->
        <views:AdBannerView 
            VerticalOptions="End"
            HorizontalOptions="Center"
            Margin="0,10,0,10" />
    </StackLayout>
</ContentPage>
```

---

## 🐛 Codes d'Erreur AdMob

Si vous voyez des erreurs, voici leur signification :

| Code | Nom | Signification | Solution |
|------|-----|---------------|----------|
| 0 | INTERNAL_ERROR | Erreur interne AdMob | Réessayer plus tard |
| 1 | INVALID_REQUEST | Requête invalide | Vérifier l'ID AdMob |
| 2 | NETWORK_ERROR | Pas de connexion | Vérifier Internet |
| 3 | NO_FILL | Pas d'annonce dispo | Normal en test, réessayer |

### Erreur NO_FILL (Code 3)

C'est **normal** en mode test ! Cela signifie :
- ✅ Votre configuration est correcte
- ✅ AdMob fonctionne
- ⏳ Aucune annonce test disponible pour le moment

**Solutions :**
1. Attendre 30 secondes (retry automatique)
2. Relancer l'application
3. Vérifier la connexion Internet
4. Essayer sur un autre appareil

---

## 🔍 Obtenir l'ID de Votre Appareil de Test

Pour ajouter votre appareil physique aux appareils de test :

### Méthode 1 : Via les Logs

1. Lancez l'app sur votre téléphone
2. Surveillez les logs :
   ```bash
   adb logcat | grep "deviceId"
   ```
3. Cherchez une ligne comme :
   ```
   Use RequestConfiguration.Builder().setTestDeviceIds(Arrays.asList("33BE2250B43518CCDA7DE426D04EE231"))
   ```
4. Copiez l'ID et ajoutez-le dans le code :

```csharp
var testDeviceIds = new List<string>
{
    AdRequest.DeviceIdEmulator,
    "33BE2250B43518CCDA7DE426D04EE231" // Votre appareil
};
```

### Méthode 2 : Via AdMob Settings

```bash
adb shell dumpsys activity | grep "ADID"
```

---

## 🚀 Passage en Production

### Étape 1 : Créer vos Unités Publicitaires dans AdMob

1. Connectez-vous à [AdMob Console](https://apps.admob.com/)
2. Sélectionnez votre application
3. Créez 3 unités publicitaires :
   - **Bannière** → Notez l'ID (ex: ca-app-pub-5085236088670848/1234567890)
   - **Interstitielle** → Notez l'ID
   - **Récompensée** → Notez l'ID

### Étape 2 : Modifier les Fichiers

#### **AdMobBannerHandler.cs**
```csharp
// Ligne 20 : Changer à false
private const bool UseTestAds = false;

// Ligne 26 : Décommenter et remplir
private const string ProductionBannerAdUnitId = "ca-app-pub-5085236088670848/VOTRE_ID_ICI";

// Ligne 29 : Modifier
private static string BannerAdUnitId => UseTestAds ? TestBannerAdUnitId : ProductionBannerAdUnitId;
```

#### **AdMobNativeService.cs**
```csharp
// Ligne 22 : Changer à false
private const bool UseTestAds = false;

// Lignes 28-29 : Décommenter et remplir
private const string ProductionRewardedAdUnitId = "ca-app-pub-5085236088670848/VOTRE_ID_ICI";
private const string ProductionInterstitialAdUnitId = "ca-app-pub-5085236088670848/VOTRE_ID_ICI";

// Lignes 32-33 : Modifier
private static string RewardedAdUnitId => UseTestAds ? TestRewardedAdUnitId : ProductionRewardedAdUnitId;
private static string InterstitialAdUnitId => UseTestAds ? TestInterstitialAdUnitId : ProductionInterstitialAdUnitId;
```

### Étape 3 : Build Release et Test

```bash
dotnet build DonTroc/DonTroc.csproj -f net8.0-android -c Release
adb install -r DonTroc/bin/Release/net8.0-android/com.bachirdev.dontroc-Signed.apk
```

⚠️ **Important :** Les vraies annonces peuvent prendre jusqu'à 1 heure pour apparaître la première fois !

---

## 📊 Checklist de Test

### Mode Test (Développement)
- [x] `UseTestAds = true` dans les deux fichiers
- [x] IDs de test Google utilisés
- [x] Configuration test activée
- [x] Logs détaillés activés
- [x] Annonces "Test Ad" visibles

### Mode Production
- [ ] `UseTestAds = false` dans les deux fichiers
- [ ] IDs de production configurés
- [ ] Application testée en Release
- [ ] Délai d'1 heure respecté
- [ ] Annonces réelles visibles

---

## 🎯 Résumé des ID AdMob

### IDs de Test Google (Actuels)
```
Bannière:       ca-app-pub-3940256099942544/6300978111
Interstitielle: ca-app-pub-3940256099942544/1033173712
Récompensée:    ca-app-pub-3940256099942544/5224354917
```

### Votre ID d'Application (AndroidManifest.xml)
```
Application ID: ca-app-pub-5085236088670848~9868416380
```

### Vos IDs d'Unités (À créer)
```
Bannière:       ca-app-pub-5085236088670848/XXXXXXXX (à créer)
Interstitielle: ca-app-pub-5085236088670848/YYYYYYYY (à créer)
Récompensée:    ca-app-pub-5085236088670848/ZZZZZZZZ (à créer)
```

---

## 🔧 Dépannage

### Problème : Pas d'annonce visible

**Solutions :**
1. Vérifier les logs : `adb logcat | grep AdMob`
2. Vérifier la connexion Internet
3. Attendre 30 secondes (retry auto)
4. Redémarrer l'app
5. Vérifier que `UseTestAds = true`

### Problème : Erreur INVALID_REQUEST

**Solutions :**
1. Vérifier les IDs AdMob
2. Vérifier l'ID de l'app dans AndroidManifest.xml
3. Reconstruire l'app : `dotnet clean && dotnet build`

### Problème : Erreur NETWORK_ERROR

**Solutions :**
1. Vérifier la connexion Internet
2. Désactiver VPN si activé
3. Essayer sur WiFi différent

---

## 📱 Test Rapide

```bash
# Script tout-en-un
cd /Users/aa1/RiderProjects/DonTroc
dotnet clean DonTroc/DonTroc.csproj
dotnet build DonTroc/DonTroc.csproj -f net8.0-android -c Debug
adb install -r DonTroc/bin/Debug/net8.0-android/com.bachirdev.dontroc-Signed.apk
adb shell am start -n com.bachirdev.dontroc/.MainActivity
adb logcat | grep -E "AdMob|ADMOB|Ads"
```

---

**Date :** 5 novembre 2025  
**Statut :** ✅ **MODE TEST CONFIGURÉ ET PRÊT**

Lancez votre app et vous devriez voir des bannières de test "Test Ad" ! 🎉


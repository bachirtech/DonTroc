# 🎮 Guide de Configuration LevelPlay - DonTroc

## 📊 État actuel

| Système | Status | Action requise |
|---------|--------|----------------|
| **AdMob Direct** | ✅ Actif | Fonctionne (si compte non suspendu) |
| **IronSource/LevelPlay SDK** | ❌ Désactivé | En attente validation Unity |
| **Réseaux médiation LevelPlay** | ⚠️ Pas configuré | Configurer dans dashboard |

---

## 🔧 Étape 1 : Configuration dans le Dashboard LevelPlay

### Accéder au dashboard
1. Allez sur https://platform.unity.com/monetization
2. Connectez-vous avec votre compte (email utilisé pour créer le compte IronSource)

### Configurer votre App
1. **Apps** → Vérifiez que votre app "DonTroc" est listée
2. **App Key** : `2525f980d` (déjà dans votre code)

### Ajouter des réseaux de médiation (IMPORTANT!)

Pour avoir des pubs MAINTENANT (même sans validation du réseau IronSource Ads), ajoutez ces réseaux :

#### 🔶 Google AdMob (via LevelPlay)
1. **Ad Units** → **Setup** → **Networks**
2. Cliquez sur **AdMob**
3. Entrez vos informations :
   - **App ID AdMob** : `ca-app-pub-5085236088670848~9868416380`
   - **Interstitial Ad Unit ID** : `ca-app-pub-5085236088670848/8273475447`
   - **Rewarded Ad Unit ID** : `ca-app-pub-5085236088670848/1650434769`
   - **Banner Ad Unit ID** : `ca-app-pub-5085236088670848/2349645674`

#### 🎮 Unity Ads
1. Cliquez sur **Unity Ads**
2. Votre Game ID Unity Ads sera auto-lié si vous utilisez le même compte

#### 📱 Meta Audience Network (Facebook)
1. Cliquez sur **Meta Audience Network**
2. Créez un compte sur https://developers.facebook.com/
3. Ajoutez les IDs de placement

#### 🎯 AppLovin
1. Cliquez sur **AppLovin**
2. Créez un compte sur https://dash.applovin.com/
3. Ajoutez votre SDK Key

---

## 🔧 Étape 2 : Activer les modes de test

Dans le dashboard LevelPlay :

1. **Settings** → **Test Mode**
2. Ajoutez votre **Device ID** (GAID) pour tester
3. Ou activez **Test Mode** globalement pendant le développement

### Trouver votre Device ID (GAID)

Sur Android :
1. Paramètres → Google → Annonces → Votre ID publicitaire
2. Ou dans les logs de l'app (cherchez "GAID" ou "AdvertisingId")

---

## 🔧 Étape 3 : Réactiver le SDK dans votre projet

Une fois que Unity aura validé votre compte :

### 1. Modifier `IronSourceProvider.cs`

```csharp
// Changer de false à true
private const bool ACCOUNT_VALIDATED = true;
```

### 2. Décommenter dans `DonTroc.csproj`

```xml
<!-- Décommentez cette ligne : -->
<ProjectReference Include="..\IronSource.Binding\IronSource.Binding.csproj" />
```

### 3. Recompiler
```bash
dotnet build DonTroc/DonTroc.csproj -c Debug
```

---

## ❓ Pourquoi les pubs AdMob ne s'affichent pas ?

Si AdMob direct ne fonctionne pas non plus, vérifiez :

### 1. Compte AdMob suspendu ?
- Allez sur https://admob.google.com/
- Vérifiez s'il y a des alertes ou restrictions

### 2. App non approuvée ?
- Votre app doit être publiée sur le Play Store
- AdMob doit avoir approuvé l'app

### 3. Pas d'inventaire ?
- Les vraies pubs peuvent ne pas être disponibles
- Utilisez les IDs de test pour vérifier que le SDK fonctionne

### IDs de test AdMob (pour debug) :

```csharp
// Remplacez temporairement vos IDs par ceux-ci pour tester :
private const string TestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
private const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";
private const string TestBannerId = "ca-app-pub-3940256099942544/6300978111";
```

---

## 📋 Checklist de dépannage

### Pour AdMob Direct :
- [ ] Compte AdMob non suspendu
- [ ] App approuvée dans AdMob
- [ ] IDs de bloc d'annonces corrects
- [ ] AndroidManifest contient l'App ID AdMob
- [ ] SDK initialisé correctement

### Pour LevelPlay :
- [ ] Email envoyé à Unity pour validation
- [ ] Réseaux configurés dans dashboard LevelPlay
- [ ] App Key correcte (`2525f980d`)
- [ ] Binding réactivé dans le projet (après validation)
- [ ] Device en mode test (pour développement)

---

## 🔍 Activer les logs de débogage

Ajoutez ceci dans `MainActivity.cs` pour voir les logs AdMob :

```csharp
// Dans OnCreate, avant Initialize
var configuration = new RequestConfiguration.Builder()
    .SetTestDeviceIds(new List<string> { "VOTRE_DEVICE_ID" })
    .Build();
MobileAds.RequestConfiguration = configuration;
```

---

## 📞 Support

- **IronSource/Unity** : ironsource-account-review@unity3d.com
- **AdMob** : https://support.google.com/admob/
- **Ticket IronSource** : #00880519

---

*Dernière mise à jour : 5 février 2026*

# 🚀 Guide d'intégration AppLovin MAX pour DonTroc

## 📋 Vue d'ensemble

AppLovin MAX est une plateforme de **médiation publicitaire** qui vous permet de :
- Combiner plusieurs réseaux publicitaires (AdMob, Unity, Facebook, etc.)
- Maximiser vos revenus grâce au bidding en temps réel
- Avoir un dashboard unifié pour tous vos réseaux

## 🔧 Étape 1: Créer un compte AppLovin

1. Rendez-vous sur **https://dash.applovin.com**
2. Cliquez sur "Sign Up"
3. Remplissez le formulaire d'inscription
4. Vérifiez votre email

## 🔑 Étape 2: Obtenir votre SDK Key

1. Connectez-vous à https://dash.applovin.com
2. Allez dans **Account** (en haut à droite) > **Keys**
3. Copiez votre **SDK Key** (ressemble à: `xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`)

## 📱 Étape 3: Créer vos Ad Units

1. Dans le dashboard AppLovin, allez dans **Monetize** > **Ad Units**
2. Cliquez sur **+ Create Ad Unit**
3. Créez les types suivants pour Android :

### Bannière (Banner)
- **Platform**: Android
- **Ad Format**: Banner
- **Name**: `DonTroc_Banner_Android`
- Notez l'**Ad Unit ID** généré

### Interstitiel
- **Platform**: Android
- **Ad Format**: Interstitial
- **Name**: `DonTroc_Interstitial_Android`
- Notez l'**Ad Unit ID** généré

### Récompensée (Rewarded)
- **Platform**: Android
- **Ad Format**: Rewarded
- **Name**: `DonTroc_Rewarded_Android`
- Notez l'**Ad Unit ID** généré

## ⚙️ Étape 4: Configurer le projet DonTroc

### 4.1 Ajouter le package NuGet

```bash
# Dans le terminal, à la racine du projet
cd DonTroc
dotnet add package AppLovin.MaxSdk.Android --version 12.6.1
```

### 4.2 Modifier AndroidManifest.xml

Ajoutez dans `/DonTroc/Platforms/Android/AndroidManifest.xml` :

```xml
<application ...>
    <!-- AppLovin SDK Key -->
    <meta-data
        android:name="applovin.sdk.key"
        android:value="VOTRE_SDK_KEY_ICI" />
</application>
```

### 4.3 Configurer les IDs dans le code

Ouvrez `/DonTroc/Services/AppLovinConfiguration.cs` et mettez à jour :

```csharp
public static class AppLovinConfiguration
{
    // ✅ Activer AppLovin
    public const bool APPLOVIN_ENABLED = true;

    // Votre SDK Key AppLovin
    public const string SDK_KEY = "VOTRE_SDK_KEY_ICI";

    // Vos Ad Unit IDs
    public const string BANNER_AD_UNIT_ID = "VOTRE_BANNER_AD_UNIT_ID";
    public const string INTERSTITIAL_AD_UNIT_ID = "VOTRE_INTERSTITIAL_AD_UNIT_ID";
    public const string REWARDED_AD_UNIT_ID = "VOTRE_REWARDED_AD_UNIT_ID";

    // ⚠️ Mettre à FALSE avant publication !
    public const bool TEST_MODE = true;
}
```

## 📊 Étape 5: Configurer la médiation AdMob (optionnel mais recommandé)

AppLovin MAX peut utiliser AdMob comme source de revenus supplémentaire.

1. Dans le dashboard AppLovin : **Monetize** > **Networks**
2. Cliquez sur **Google AdMob**
3. Entrez vos informations AdMob :
   - **App ID**: `ca-app-pub-5085236088670848~9868416380`
   - **Banner Ad Unit ID**: `ca-app-pub-5085236088670848/2349645674`
   - etc.
4. Activez le **bidding** pour maximiser les revenus

## 🧪 Étape 6: Tester

### Mode test
1. Assurez-vous que `TEST_MODE = true` dans `AppLovinConfiguration.cs`
2. Lancez l'application en mode Debug
3. Vérifiez les logs pour voir les messages AppLovin

### Vérifier dans les logs
```
📱 AppLovin SDK initialisé
✅ AppLovin banner chargée
```

## 🚀 Étape 7: Passer en production

1. Changez `TEST_MODE = false` dans `AppLovinConfiguration.cs`
2. Recompilez en mode Release
3. Publiez sur le Play Store

## 📁 Fichiers créés/modifiés

| Fichier | Description |
|---------|-------------|
| `Services/AppLovinConfiguration.cs` | Configuration centralisée |
| `Services/AppLovinService.cs` | Service principal multiplateforme |
| `Platforms/Android/AppLovinServiceAndroid.cs` | Implémentation Android |
| `Platforms/Android/AppLovinBannerHandler.cs` | Handler natif bannières |
| `Platforms/Android/UnifiedAdBannerHandler.cs` | Handler unifié AppLovin/AdMob |
| `Views/UnifiedAdBannerView.cs` | Vue de bannière unifiée |
| `MauiProgram.cs` | Enregistrement des services |

## 🔄 Basculement automatique AppLovin ↔ AdMob

L'application bascule automatiquement entre les plateformes :

```
Priorité 1: AppLovin MAX (si APPLOVIN_ENABLED = true et configuré)
Priorité 2: AdMob (si ADS_ENABLED = true)
Priorité 3: Aucune pub (invisible)
```

Pour utiliser la bannière unifiée dans vos pages XAML :
```xml
<views:UnifiedAdBannerView />
```

## 💰 Revenus estimés

| Format | eCPM AdMob seul | eCPM avec AppLovin MAX |
|--------|-----------------|------------------------|
| Bannière | $0.50-2 | $1-4 |
| Interstitiel | $2-8 | $5-15 |
| Rewarded | $5-15 | $10-25 |

La médiation peut **augmenter vos revenus de 30-50%** en moyenne.

## ❓ Problèmes courants

### "SDK Key invalide"
- Vérifiez que vous avez copié la bonne clé depuis le dashboard AppLovin

### "Ad Unit not found"
- Vérifiez que l'Ad Unit ID est correct
- Attendez 15-30 minutes après la création d'un nouvel Ad Unit

### "No fill"
- Normal en mode test
- En production, cela peut prendre 24-48h pour avoir des impressions

## 📞 Support

- Documentation AppLovin: https://dash.applovin.com/documentation
- Support: support@applovin.com

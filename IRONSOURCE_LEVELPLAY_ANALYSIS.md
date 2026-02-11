# 🎮 Analyse d'intégration IronSource/LevelPlay pour DonTroc

## 📋 Résumé Exécutif

**Date d'analyse :** 4 février 2026

### État actuel
| Critère | Statut |
|---------|--------|
| Binding NuGet officiel | ❌ **Non disponible** |
| SDK Android natif | ✅ Disponible (AAR) |
| SDK iOS natif | ✅ Disponible (Framework) |
| Intégration MAUI native | ⚠️ Binding manuel requis |

---

## 🔍 Qu'est-ce qu'IronSource/LevelPlay ?

**IronSource** (maintenant **Unity LevelPlay**) est une plateforme de médiation publicitaire qui :

1. **Agrège plusieurs réseaux publicitaires** :
   - AdMob
   - Unity Ads
   - Meta Audience Network (Facebook)
   - AppLovin
   - Vungle
   - Pangle (TikTok)
   - Et bien d'autres...

2. **Optimise automatiquement les revenus** via l'enchères en temps réel (bidding)

3. **Fournit une seule intégration SDK** au lieu de plusieurs

### Avantages pour DonTroc

| Avantage | Description |
|----------|-------------|
| 🎯 Diversification | Ne dépend plus d'un seul réseau (ex: AdMob suspendu) |
| 💰 Revenus optimisés | Compétition entre réseaux = meilleurs eCPMs |
| 🛡️ Résilience | Si un réseau est bloqué, les autres prennent le relais |
| 📊 Dashboard unifié | Une seule interface pour tout gérer |

---

## 🚫 Problème : Pas de binding .NET MAUI officiel

### Recherche effectuée

```
✗ Xamarin.Android.IronSource → N'existe pas sur NuGet public
✗ LevelPlay.MAUI → N'existe pas
✗ IronSource.SDK → N'existe pas
```

### Raisons
- IronSource/Unity fournit uniquement des SDKs pour :
  - Unity Engine (C# spécifique Unity)
  - Android natif (Java/Kotlin - AAR)
  - iOS natif (Swift/Objective-C - Framework)
  - React Native (wrapper)
  - Flutter (plugin)

---

## 💡 Solutions possibles

### Solution 1 : Créer un Android Binding Library ⭐⭐⭐

**Difficulté :** Élevée  
**Temps estimé :** 3-5 jours  
**Maintenance :** Mise à jour à chaque nouvelle version SDK

#### Étapes

1. **Télécharger les AARs depuis Maven Central** :
   ```
   https://mvnrepository.com/artifact/com.ironsource.sdk/mediationsdk
   ```

2. **Créer un projet de binding Android** :
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net9.0-android</TargetFramework>
       <SupportedOSPlatformVersion>23</SupportedOSPlatformVersion>
     </PropertyGroup>
     
     <ItemGroup>
       <AndroidLibrary Include="Jars/ironsource-sdk.aar" />
       <TransformFile Include="Transforms/Metadata.xml" />
       <TransformFile Include="Transforms/EnumFields.xml" />
       <TransformFile Include="Transforms/EnumMethods.xml" />
     </ItemGroup>
   </Project>
   ```

3. **Résoudre les conflits de types Java → C#**

4. **Créer les wrappers MAUI**

---

### Solution 2 : Utiliser des bindings communautaires ⭐⭐

**Difficulté :** Moyenne  
**Risque :** Dépendance à un mainteneur tiers

Rechercher sur GitHub :
```
https://github.com/nicwise/IronSourceBinding
https://github.com/nicwise/IronSourceAdsBinding
```

⚠️ Ces bindings sont souvent obsolètes ou incomplets.

---

### Solution 3 : AppLovin MAX (Alternative recommandée) ⭐⭐⭐⭐⭐

**AppLovin MAX** est une alternative de médiation avec :
- ✅ **Binding Xamarin disponible** (plus maintenu mais fonctionnel)
- ✅ Fonctionnalités similaires à LevelPlay
- ✅ Inclut AdMob, Unity Ads, Meta, etc.

```xml
<!-- Rechercher sur NuGet -->
<PackageReference Include="Xamarin.AppLovin.MAX" Version="x.x.x" />
```

---

### Solution 4 : Intégration directe multi-réseaux ⭐⭐⭐

Intégrer manuellement plusieurs réseaux :

```xml
<!-- Votre configuration actuelle -->
<PackageReference Include="Xamarin.GooglePlayServices.Ads.Lite" Version="124.0.0.4" />

<!-- Meta Audience Network (Facebook Ads) -->
<PackageReference Include="Xamarin.Facebook.AudienceNetwork.Android" Version="6.x.x" />

<!-- Unity Ads (binding manuel requis) -->
<!-- <PackageReference Include="..." /> -->
```

---

## 🔧 Implémentation recommandée : Abstraction multi-provider

### Architecture proposée

```
┌────────────────────────────────────────────────┐
│           IAdsService (Interface)              │
│  - ShowBanner()                                │
│  - ShowInterstitial()                          │
│  - ShowRewarded()                              │
└────────────────────┬───────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────────┐
│  AdMobProvider  │    │  BackupAdsProvider  │
│   (Principal)   │    │   (Fallback)        │
└─────────────────┘    └─────────────────────┘
```

### Interface unifiée

```csharp
// /DonTroc/Services/IAdsService.cs
namespace DonTroc.Services
{
    public interface IAdsService
    {
        void Initialize();
        
        // Bannières
        Task<bool> ShowBannerAsync(string placement);
        void HideBanner();
        
        // Interstitiels
        bool IsInterstitialReady { get; }
        Task ShowInterstitialAsync();
        void PreloadInterstitial();
        
        // Récompensés
        bool IsRewardedReady { get; }
        Task<bool> ShowRewardedAsync();
        void PreloadRewarded();
        
        // Événements
        event EventHandler<AdEventArgs> OnAdLoaded;
        event EventHandler<AdEventArgs> OnAdFailed;
        event EventHandler<RewardEventArgs> OnRewardEarned;
    }
}
```

### Implémentation avec fallback

```csharp
// /DonTroc/Services/MediatedAdsService.cs
namespace DonTroc.Services
{
    public class MediatedAdsService : IAdsService
    {
        private readonly List<IAdsProvider> _providers;
        
        public MediatedAdsService()
        {
            _providers = new List<IAdsProvider>
            {
                new AdMobProvider(),      // Principal
                // new MetaAdsProvider(), // Backup 1
                // new UnityAdsProvider() // Backup 2
            };
        }
        
        public async Task ShowInterstitialAsync()
        {
            foreach (var provider in _providers)
            {
                if (provider.IsInterstitialReady)
                {
                    await provider.ShowInterstitialAsync();
                    return;
                }
            }
            
            Debug.WriteLine("⚠️ Aucune publicité disponible");
        }
    }
}
```

---

## 📦 Guide d'intégration IronSource (si vous décidez de créer le binding)

### Étape 1 : Créer le projet de binding

```bash
cd /Users/aa1/RiderProjects/DonTroc
mkdir -p IronSource.Binding/Jars
mkdir -p IronSource.Binding/Transforms
```

### Étape 2 : Télécharger le SDK

```bash
# Télécharger depuis Maven Central
# Version actuelle : 8.x.x
curl -o IronSource.Binding/Jars/mediationsdk.aar \
  "https://mvnrepository.com/artifact/com.ironsource.sdk/mediationsdk/8.0.0/aar"
```

### Étape 3 : Créer le fichier .csproj

```xml
<!-- IronSource.Binding/IronSource.Binding.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-android</TargetFramework>
    <SupportedOSPlatformVersion>23</SupportedOSPlatformVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <AndroidLibrary Include="Jars\mediationsdk.aar" />
    <TransformFile Include="Transforms\Metadata.xml" />
  </ItemGroup>
  
  <!-- Dépendances AndroidX requises -->
  <ItemGroup>
    <PackageReference Include="Xamarin.AndroidX.AppCompat" Version="1.7.0.4" />
    <PackageReference Include="Xamarin.AndroidX.RecyclerView" Version="1.4.0.1" />
    <PackageReference Include="Xamarin.Google.Android.Material" Version="1.13.0.1" />
  </ItemGroup>
</Project>
```

### Étape 4 : Créer Metadata.xml

```xml
<!-- IronSource.Binding/Transforms/Metadata.xml -->
<metadata>
  <!-- Renommer les packages pour C# -->
  <attr path="/api/package[@name='com.ironsource.sdk']" 
        name="managedName">IronSource.SDK</attr>
  
  <!-- Exclure les classes problématiques si nécessaire -->
  <!--<remove-node path="/api/package[@name='...']/class[@name='...']"/>-->
</metadata>
```

### Étape 5 : Créer le service wrapper

```csharp
// DonTroc/Services/IronSourceService.cs
#if ANDROID
using IronSource.SDK; // Namespace du binding

namespace DonTroc.Services
{
    public class IronSourceService : IAdsProvider
    {
        private const string AppKey = "VOTRE_APP_KEY_IRONSOURCE";
        
        public void Initialize()
        {
            var activity = Platform.CurrentActivity;
            if (activity == null) return;
            
            // Initialisation IronSource
            IronSource.Init(activity, AppKey, 
                IronSource.AD_UNIT.INTERSTITIAL,
                IronSource.AD_UNIT.REWARDED_VIDEO,
                IronSource.AD_UNIT.BANNER);
        }
        
        public bool IsInterstitialReady => IronSource.IsInterstitialReady();
        
        public async Task ShowInterstitialAsync()
        {
            if (IsInterstitialReady)
            {
                IronSource.ShowInterstitial();
            }
        }
        
        // ... autres méthodes
    }
}
#endif
```

---

## ⚠️ Difficultés attendues avec le binding

| Problème | Impact | Solution |
|----------|--------|----------|
| Conflits AndroidX | 🔴 Élevé | Aligner les versions avec votre projet |
| Obfuscation ProGuard | 🟡 Moyen | Ajouter des règles keep |
| Signatures Java génériques | 🟡 Moyen | Metadata.xml |
| Callbacks asynchrones | 🟡 Moyen | Wrappers C# |
| Mises à jour SDK | 🔴 Élevé | Maintenance régulière |

---

## 📊 Tableau comparatif des options

| Option | Difficulté | Temps | Maintenance | Fiabilité |
|--------|------------|-------|-------------|-----------|
| AdMob seul (actuel) | ✅ Facile | 0 | ✅ Basse | ⚠️ 1 réseau |
| Binding IronSource | ❌ Difficile | 3-5j | 🔴 Haute | ✅ Multi-réseau |
| AppLovin MAX | 🟡 Moyenne | 1j | 🟡 Moyenne | ✅ Multi-réseau |
| Multi-SDK manuel | 🟡 Moyenne | 2-3j | 🟡 Moyenne | ✅ Contrôle total |

---

## 🎯 Recommandation finale

### Pour DonTroc, je recommande :

1. **Court terme (maintenant)** :
   - ✅ Continuer avec AdMob (votre intégration actuelle)
   - ✅ Attendre la réactivation de votre compte (si suspendu)

2. **Moyen terme (1-2 mois)** :
   - 🔄 Ajouter Meta Audience Network comme backup
   - Binding disponible : `Xamarin.Facebook.AudienceNetwork.Android`

3. **Long terme** :
   - 📦 Créer un binding IronSource/LevelPlay si les revenus justifient l'effort
   - Ou utiliser AppLovin MAX si un binding communautaire est disponible

---

## 📁 Fichiers à créer pour l'intégration future

```
DonTroc/
├── IronSource.Binding/          # (à créer si besoin)
│   ├── IronSource.Binding.csproj
│   ├── Jars/
│   │   └── mediationsdk.aar
│   └── Transforms/
│       └── Metadata.xml
├── DonTroc/
│   └── Services/
│       ├── IAdsService.cs       # Interface unifiée (à créer)
│       ├── MediatedAdsService.cs # Service de médiation (à créer)
│       └── Providers/
│           ├── AdMobProvider.cs
│           ├── MetaAdsProvider.cs
│           └── IronSourceProvider.cs
```

---

## 🔗 Ressources utiles

- **IronSource SDK** : https://developers.is.com/ironsource-mobile/android/
- **Unity LevelPlay** : https://unity.com/products/mediation
- **Maven Central** : https://mvnrepository.com/artifact/com.ironsource.sdk
- **Binding Xamarin Guide** : https://learn.microsoft.com/xamarin/android/platform/binding-java-library/

---

## ✅ Conclusion

L'intégration d'IronSource/LevelPlay dans DonTroc est **techniquement possible** mais nécessite un **effort significatif** de création de binding Android. 

**L'option la plus pragmatique** pour votre situation est de :
1. Garder AdMob comme réseau principal
2. Ajouter un réseau backup (Meta Ads ou AppLovin) avec des bindings existants
3. Implémenter une abstraction permettant le fallback automatique

Cela offre la résilience multi-réseau sans la complexité d'un binding IronSource complet.

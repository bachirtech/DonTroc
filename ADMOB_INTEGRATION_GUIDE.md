# 📱 Guide d'Intégration AdMob Native pour DonTroc

## ✅ Ce qui a été fait

### 1. **Packages NuGet installés**
- ✅ `Xamarin.GooglePlayServices.Ads` (v23.5.0)
- ✅ `Xamarin.Google.Android.Play.Integrity` (v1.4.0)

### 2. **Fichiers créés/modifiés**

#### **Fichiers Android natifs**
- ✅ `AdMobBannerHandler.cs` - Handler personnalisé pour les bannières AdMob natives
- ✅ `AdMobNativeService.cs` - Service natif pour les pubs récompensées et interstitielles
- ✅ `MainActivity.cs` - Initialisation d'AdMob au démarrage

#### **Configuration**
- ✅ `AndroidManifest.xml` - Contient déjà l'ID de l'application AdMob
- ✅ `MauiProgram.cs` - Handler AdMob déjà enregistré
- ✅ `DonTroc.csproj` - Packages AdMob ajoutés

### 3. **Architecture de l'intégration**

```
┌─────────────────────────────────────────┐
│          Vue MAUI (XAML/C#)             │
│  <views:AdBannerView HeightRequest="50"/>│
└───────────────┬─────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────┐
│      AdBannerView.cs (Contrôle)         │
│  - Placeholder cross-platform           │
└───────────────┬─────────────────────────┘
                │
                ▼ (sur Android)
┌─────────────────────────────────────────┐
│   AdMobBannerHandler.cs (Handler)       │
│  - Remplace par AdView natif Android    │
│  - Charge les bannières AdMob réelles   │
└─────────────────────────────────────────┘
```

## 🔧 Configuration des IDs publicitaires

### IDs Production (déjà configurés)
```csharp
// Dans AdMobService.cs et AdMobNativeService.cs
Application ID: ca-app-pub-5085236088670848~9868416380
Banner ID:      ca-app-pub-5085236088670848/2349645674
Rewarded ID:    ca-app-pub-5085236088670848/1650434769
Interstitial ID: ca-app-pub-5085236088670848/8273475447
```

### ⚠️ Pour les tests en développement
Si vous voulez tester sans risquer votre compte AdMob, utilisez les IDs de test :

```csharp
// IDs de test AdMob (à utiliser pendant le développement)
const string TestBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
const string TestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
const string TestInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
```

## 📝 Comment utiliser dans vos vues

### 1. **Bannière publicitaire dans XAML**

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:views="clr-namespace:DonTroc.Views"
             x:Class="DonTroc.Views.MaPage">
    
    <VerticalStackLayout>
        <!-- Votre contenu ici -->
        
        <!-- Bannière AdMob en bas de page -->
        <views:AdBannerView 
            HeightRequest="50" 
            WidthRequest="320"
            HorizontalOptions="Center"
            VerticalOptions="End"
            Margin="0,10,0,10" />
    </VerticalStackLayout>
</ContentPage>
```

### 2. **Publicité récompensée (dans un ViewModel)**

```csharp
// Injecter le service AdMob
private readonly AdMobService _adMobService;

public MonViewModel(AdMobService adMobService)
{
    _adMobService = adMobService;
}

// Afficher une pub récompensée
private async Task ShowRewardedAdAsync()
{
    if (_adMobService.IsRewardedAdReady())
    {
        bool watchedFully = await _adMobService.ShowRewardedAdAsync();
        
        if (watchedFully)
        {
            // L'utilisateur a regardé la pub complètement
            // Donner la récompense ici
            await Shell.Current.DisplayAlert("🎉", "Vous avez gagné 10 pièces !", "Super");
        }
        else
        {
            await Shell.Current.DisplayAlert("Info", "Publicité non regardée complètement", "OK");
        }
    }
    else
    {
        await Shell.Current.DisplayAlert("Info", "Publicité en chargement, réessayez dans quelques secondes", "OK");
    }
}
```

### 3. **Publicité interstitielle (plein écran)**

```csharp
// Afficher une pub interstitielle entre deux pages
private async Task NavigateWithInterstitialAsync()
{
    // Vérifier si une pub est prête
    if (_adMobService.IsInterstitialAdReady())
    {
        await _adMobService.ShowInterstitialAdAsync();
        await Task.Delay(500); // Attendre la fin de l'animation
    }
    
    // Naviguer vers la page suivante
    await Shell.Current.GoToAsync("//NextPage");
}
```

## 🧪 Comment tester sur votre téléphone Android

### Étape 1: Compiler l'application
```bash
cd /Users/aa1/RiderProjects/DonTroc
dotnet build DonTroc/DonTroc.csproj -f net8.0-android -c Debug
```

### Étape 2: Déployer sur votre appareil
1. Activer le **Mode développeur** sur votre téléphone Android
2. Activer le **Débogage USB**
3. Connecter le téléphone via USB
4. Lancer depuis Rider ou Visual Studio

### Étape 3: Vérifier dans les logs
Ouvrez **Android Logcat** et recherchez :
```
✅ SDK AdMob initialisé
✅ AdMob initialisé dans MainActivity
✅ Bannière AdMob native créée et chargée
✅ Pub récompensée chargée
✅ Pub interstitielle chargée
```

### Étape 4: Observer les bannières
- Les bannières devraient apparaître automatiquement dans vos vues
- Les vraies pubs AdMob peuvent prendre 5-10 secondes à charger
- Si vous voyez "Chargement publicité...", c'est que le handler n'est pas encore activé

## 🐛 Debugging

### Les bannières ne s'affichent pas ?

**1. Vérifiez que le SDK est initialisé**
```
Logcat > Filter: "AdMob"
```

**2. Vérifiez les erreurs de chargement**
```
❌ Erreur chargement bannière: [MESSAGE] (Code: [CODE])
```

**Codes d'erreur courants :**
- `Code 0` : Erreur interne (redémarrez l'app)
- `Code 1` : Invalid request (vérifiez l'ID de l'unité publicitaire)
- `Code 2` : Network error (vérifiez votre connexion Internet)
- `Code 3` : No fill (aucune pub disponible - normal en test)

**3. Vérifiez que google-services.json est présent**
```bash
ls -la DonTroc/Platforms/Android/google-services.json
```

**4. Vérifiez les permissions Internet**
Dans `AndroidManifest.xml` :
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

### Les pubs récompensées ne se chargent pas ?

**Solution 1: Attendre 30-60 secondes**
Les pubs peuvent prendre du temps à se précharger.

**Solution 2: Forcer le rechargement**
```csharp
// Dans AdMobNativeService.cs, réduire le délai de réessai
Task.Delay(10000).ContinueWith(_ => LoadRewardedAd()); // 10 secondes au lieu de 30
```

**Solution 3: Utiliser les IDs de test**
Pendant le développement, les IDs de production peuvent avoir des limites de requêtes.

## 📊 Monitoring des performances

### Logs à surveiller

**✅ Succès**
```
✅ SDK AdMob initialisé
✅ Bannière AdMob chargée avec succès
✅ Pub récompensée chargée
🎉 Récompense: 10 coins
```

**⚠️ Avertissements**
```
⚠️ Pub récompensée pas prête
⚠️ Pub interstitielle pas prête
```

**❌ Erreurs**
```
❌ Erreur initialisation AdMob: [MESSAGE]
❌ Erreur chargement bannière: [MESSAGE]
❌ Erreur pub récompensée: [MESSAGE]
```

## 🚀 Optimisations pour la production

### 1. Préchargement intelligent
Les pubs sont automatiquement préchargées au démarrage et rechargées après chaque affichage.

### 2. Gestion de la mémoire
Les bannières sont automatiquement détruites quand la vue est déchargée :
```csharp
protected override void DisconnectHandler(AdView platformView)
{
    platformView?.Destroy();
    base.DisconnectHandler(platformView);
}
```

### 3. Gestion des erreurs réseau
Si une pub échoue, elle sera rechargée automatiquement après 30 secondes.

### 4. Limitation de fréquence
Pour éviter de spammer l'utilisateur avec des pubs :
```csharp
private DateTime _lastAdShown = DateTime.MinValue;

private bool CanShowAd()
{
    return (DateTime.Now - _lastAdShown).TotalMinutes >= 3; // 3 minutes minimum entre 2 pubs
}
```

## 📱 Exemple complet d'intégration

### Vue avec bannière
```xml
<!-- HomePage.xaml -->
<ContentPage xmlns:views="clr-namespace:DonTroc.Views">
    <Grid RowDefinitions="*, Auto">
        <!-- Contenu principal -->
        <ScrollView Grid.Row="0">
            <!-- Votre contenu ici -->
        </ScrollView>
        
        <!-- Bannière en bas -->
        <views:AdBannerView Grid.Row="1" />
    </Grid>
</ContentPage>
```

### ViewModel avec pub récompensée
```csharp
public class PremiumFeaturesViewModel : BaseViewModel
{
    private readonly AdMobService _adMobService;
    
    public ICommand WatchAdForCoinsCommand { get; }
    
    public PremiumFeaturesViewModel(AdMobService adMobService)
    {
        _adMobService = adMobService;
        WatchAdForCoinsCommand = new Command(async () => await WatchAdForCoinsAsync());
    }
    
    private async Task WatchAdForCoinsAsync()
    {
        if (!_adMobService.IsRewardedAdReady())
        {
            await Shell.Current.DisplayAlert("⏳", "Publicité en chargement...", "OK");
            return;
        }
        
        bool success = await _adMobService.ShowRewardedAdAsync();
        
        if (success)
        {
            // Récompenser l'utilisateur
            await GamificationService.AddCoins(10);
            await Shell.Current.DisplayAlert("🎉", "Vous avez gagné 10 pièces !", "Super");
        }
    }
}
```

## 🎯 Checklist avant la publication

- [ ] Vérifier que les IDs de production sont utilisés (pas les IDs de test)
- [ ] Tester sur plusieurs appareils Android
- [ ] Vérifier que les bannières s'affichent correctement
- [ ] Tester les pubs récompensées du début à la fin
- [ ] Tester les pubs interstitielles
- [ ] Vérifier que l'app ne crash pas si pas d'Internet
- [ ] Vérifier les logs pour détecter les erreurs
- [ ] Tester avec un compte Google différent (pas votre compte dev)
- [ ] Respecter les règles Google AdMob (pas de clics frauduleux)

## 📞 Support

### Logs utiles pour le debugging
```bash
# Voir tous les logs AdMob
adb logcat | grep -i "admob\|ads\|google"

# Voir uniquement les logs de votre app
adb logcat | grep "DonTroc"
```

### En cas de problème
1. Vérifier `google-services.json` est à jour
2. Vérifier que l'app ID dans `AndroidManifest.xml` correspond à votre compte AdMob
3. Nettoyer et rebuilder : `dotnet clean && dotnet build`
4. Désinstaller et réinstaller l'app sur le téléphone

---

**Date de création:** 14 octobre 2025  
**Version:** 1.0  
**Auteur:** Expert Mobile MAUI C#


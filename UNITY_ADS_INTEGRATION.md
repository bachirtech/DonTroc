# Guide d'intégration Unity Ads pour DonTroc

## État actuel
⚠️ **Unity Ads n'a pas de binding NuGet officiel pour Xamarin/.NET MAUI**

Le SDK Unity Ads est conçu pour :
- Unity Engine (jeux)
- Android natif (Java/Kotlin)  
- iOS natif (Swift/Objective-C)

## Solution : Binding Android manuel

### Étape 1 : Télécharger le SDK Unity Ads

1. Aller sur : https://dashboard.unity3d.com
2. Créer un compte Unity (gratuit)
3. Créer un nouveau projet
4. Aller dans **Monetization** → **Get Started**
5. Télécharger le SDK Android (AAR)

Ou télécharger directement depuis Maven :
```
https://repo1.maven.org/maven2/com/unity3d/ads/unity-ads/
```

### Étape 2 : Créer le projet de binding

Le projet `UnityAds.Binding` a été créé dans la solution.

### Étape 3 : Configuration dans Unity Dashboard

1. Créer votre Game ID : https://dashboard.unity3d.com
2. Créer les Placements :
   - `banner` - Bannière
   - `interstitial` - Interstitiel
   - `rewardedVideo` - Vidéo récompensée

3. Copier votre Game ID (ex: `5123456`)

### Étape 4 : Configurer le Game ID dans le code

Modifier `/DonTroc/Platforms/Android/UnityAdsService.cs` :
```csharp
private const string GameId = "VOTRE_GAME_ID_ICI";
```

## Alternative recommandée : IronSource/LevelPlay

IronSource est une plateforme de médiation qui :
- ✅ A un binding Xamarin officiel
- ✅ Inclut Unity Ads + AdMob + Facebook + autres
- ✅ Optimise automatiquement les revenus
- ✅ Un seul SDK à intégrer

Package : `Xamarin.Android.IronSource`

## En attendant AdMob

Votre compte AdMob sera réactivé dans **29 jours**. L'intégration AdMob existante fonctionnera automatiquement.

## Fichiers modifiés

- `/DonTroc/Platforms/Android/UnityAdsService.cs` - Service stub (en attente du binding)
- `/DonTroc/Services/IUnityAdsService.cs` - Interface du service
- `/DonTroc/MauiProgram.cs` - Injection de dépendances

## Prochaines étapes

1. [ ] Créer un compte Unity : https://dashboard.unity3d.com
2. [ ] Obtenir votre Game ID
3. [ ] Télécharger le SDK AAR
4. [ ] Créer le binding (ou utiliser IronSource)
5. [ ] Tester en mode test avant la production

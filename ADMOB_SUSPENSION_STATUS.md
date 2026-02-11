# 🚫 Suspension AdMob - Actions Effectuées

## Situation
**Date de suspension**: 21 janvier 2026  
**Durée**: 29 jours  
**Date estimée de réactivation**: ~19 février 2026  
**Motif**: Détection de trafic incorrect (auto-clics)

## Actions entreprises

### 1. Création d'un flag de désactivation centralisé
- **Fichier**: `DonTroc/Services/AdMobConfiguration.cs`
- **Constante**: `ADS_ENABLED = false`

Ce flag permet de désactiver toutes les publicités en un seul endroit.

### 2. Modifications apportées

| Fichier | Modification |
|---------|-------------|
| `AdMobConfiguration.cs` | **NOUVEAU** - Configuration centralisée avec flag `ADS_ENABLED` |
| `AdBannerView.cs` | Bannière invisible si `ADS_ENABLED = false` |
| `AdMobBannerHandler.cs` | Ne charge pas de pub si `ADS_ENABLED = false` |
| `AdMobService.cs` | Toutes les méthodes retournent `false` / ne chargent pas si désactivé |
| `MainActivity.cs` | `InitializeAdMob()` ignorée si désactivé |

### 3. Comportement actuel
- ✅ **Aucune publicité n'est chargée**
- ✅ **Aucune requête n'est envoyée à AdMob**
- ✅ **La bannière publicitaire est invisible (hauteur = 0)**
- ✅ **Le SDK AdMob n'est PAS initialisé**

## Comment réactiver les publicités

Après la fin de la période de suspension (~19 février 2026) et **confirmation de Google** que votre compte est réactivé:

1. Ouvrez `DonTroc/Services/AdMobConfiguration.cs`
2. Changez:
   ```csharp
   public const bool ADS_ENABLED = false;
   ```
   En:
   ```csharp
   public const bool ADS_ENABLED = true;
   ```
3. Recompilez et déployez l'application

## Recommandations pour éviter les futures suspensions

### ❌ NE PAS FAIRE
- **Ne jamais cliquer sur vos propres publicités**
- Ne pas utiliser d'émulateurs avec vos vrais IDs de production
- Ne pas demander à des amis/famille de cliquer sur les pubs

### ✅ À FAIRE
1. **Toujours utiliser des IDs de test pendant le développement**
   ```csharp
   // ID de test pour bannières
   private const string TestBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
   ```

2. **Configurer les appareils de test** dans `MainActivity.cs`:
   ```csharp
   var testDeviceIds = new List<string>
   {
       AdRequest.DeviceIdEmulator,
       "VOTRE_DEVICE_ID_ICI" // Obtenu depuis les logs AdMob
   };
   ```

3. **Utiliser la barre d'outils des éditeurs Google** pour tester les clics

4. **Surveiller le trafic** via la console AdMob

## Structure de contrôle AdMob

```
AdMobConfiguration.ADS_ENABLED = false
        │
        ├── MainActivity.InitializeAdMob() → IGNORÉ
        │
        ├── AdBannerView() → INVISIBLE (HeightRequest = 0)
        │
        ├── AdMobBannerHandler.ConnectHandler() → RETOUR IMMÉDIAT
        │
        └── AdMobService
                ├── IsRewardedAdReady() → false
                ├── IsInterstitialAdReady() → false
                └── LoadAds() → IGNORÉ
```

## Logs de debug
Quand les pubs sont désactivées, les logs afficheront:
```
⚠️ AdMob désactivé - Suspension en cours. X jours restants estimés.
🚫 Initialisation AdMob ignorée - compte suspendu
```

# 📱 Guide de Test - Bannière AdMob

## ✅ Corrections Appliquées

### 1. **Fond Rouge Retiré**
- La ligne `_adView.SetBackgroundColor(Android.Graphics.Color.Red);` a été supprimée
- La bannière affichera maintenant les vraies publicités AdMob

### 2. **Règles Firebase Déployées**
- Les index Firebase pour `UserId` ont été déployés avec succès
- Les erreurs de requêtes Firebase sont maintenant résolues

### 3. **Code Nettoyé**
- Retrait des `using` inutilisés
- Correction des warnings de code

---

## 🧪 Comment Tester la Bannière AdMob

### Étape 1 : Lancer l'Application en Mode Debug

```bash
cd /Users/aa1/RiderProjects/DonTroc
dotnet build DonTroc/DonTroc.csproj -c Debug -f net8.0-android
```

### Étape 2 : Vérifier les Logs

Ouvrez la console de débogage et recherchez ces messages :

#### ✅ **Messages de Succès**
```
🎯 CRÉATION BANNIÈRE ADMOB
🎯 Mode: TEST (annonces de démonstration)
🎯 ID utilisé: ca-app-pub-3940256099942544/6300978111
✅ Bannière AdMob créée et ajoutée au container
⏳ En attente de la réponse AdMob...
✅✅✅ BANNIÈRE ADMOB CHARGÉE AVEC SUCCÈS ✅✅✅
```

#### ⚠️ **Messages d'Erreur Possibles**

**Erreur Code 3 (NO_FILL)**
```
❌ Code erreur: 3
ℹ️ ERROR_CODE_NO_FILL - Aucune annonce disponible
ℹ️ C'est NORMAL en mode test
```
**Solution** : Réessayez après 30 secondes (le code réessaie automatiquement)

**Erreur Code 2 (NETWORK_ERROR)**
```
❌ Code erreur: 2
ℹ️ ERROR_CODE_NETWORK_ERROR - Erreur réseau
```
**Solution** : Vérifiez votre connexion Internet

**Erreur Code 1 (INVALID_REQUEST)**
```
❌ Code erreur: 1
ℹ️ ERROR_CODE_INVALID_REQUEST - Requête invalide
```
**Solution** : Vérifiez la configuration AdMob dans le `AndroidManifest.xml`

---

## 📍 Où la Bannière Apparaît

La bannière AdMob est affichée dans **3 vues** :

1. **AnnoncesView.xaml** (Page des annonces)
2. **DashboardView.xaml** (Page d'accueil)
3. **ProfilView.xaml** (Page de profil)

### Position de la Bannière
- **Hauteur** : 50-60 pixels
- **Largeur** : 320 pixels (format standard AdMob)
- **Position** : En haut de la page (`Grid.Row="0"`)
- **Alignement** : Centré horizontalement

---

## 🔍 Diagnostic Visuel

### Ce Que Vous Devriez Voir

#### En Mode Test (Actuel)
- Une **publicité de test Google** avec du contenu générique
- Format bannière standard (320x50)
- Fond blanc/gris avec du texte publicitaire

#### Si la Bannière Ne S'Affiche Pas

1. **Vérifiez les dimensions dans les logs**
```
📊 État de la bannière:
   • Visibility: Visible
   • Width: 900 pixels    <- Devrait être > 0
   • Height: 140 pixels   <- Devrait être > 0
   • Parent: ✅ Attaché
```

2. **Vérifiez que l'ID AdMob est correct**
```csharp
// Dans AdMobBannerHandler.cs, ligne ~34
private const string TestBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
```

3. **Vérifiez le AndroidManifest.xml**
```xml
<meta-data
    android:name="com.google.android.gms.ads.APPLICATION_ID"
    android:value="ca-app-pub-5085236088670848~XXXXXXXX"/>
```

---

## 🚀 Passer en Mode Production

Une fois les tests réussis, pour activer les vraies publicités :

### 1. Obtenir Votre ID de Bannière AdMob

1. Connectez-vous à [AdMob Console](https://apps.admob.com/)
2. Sélectionnez votre application **DonTroc**
3. Allez dans **Blocs d'annonces** > **Créer un bloc d'annonces**
4. Sélectionnez **Bannière**
5. Copiez l'ID (format : `ca-app-pub-XXXXXXXX/XXXXXXXXXX`)

### 2. Modifier AdMobBannerHandler.cs

```csharp
// Ligne ~36-38
private const string TestBannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
private const string ProductionBannerAdUnitId = "ca-app-pub-5085236088670848/VOTRE_ID_ICI";

// Ligne ~42 - Changer en false pour production
private const bool UseTestAds = false;

// Ligne ~45 - Modifier pour utiliser l'ID de production
private static string BannerAdUnitId => UseTestAds ? TestBannerAdUnitId : ProductionBannerAdUnitId;
```

### 3. Rebuild et Déployer
```bash
dotnet build DonTroc/DonTroc.csproj -c Release -f net8.0-android
```

---

## 🛠️ Dépannage

### La Bannière Est Invisible

**Cause 1** : Dimensions nulles
```
Width: 0 pixels
Height: 0 pixels
```
**Solution** : Vérifiez que le `AdBannerView` a bien `HeightRequest="60"` dans le XAML

**Cause 2** : Parent non attaché
```
Parent: ❌ Non attaché
```
**Solution** : Vérifiez que le `Grid.Row="0"` est correct dans le XAML

**Cause 3** : Erreur de chargement AdMob
```
❌❌❌ ÉCHEC CHARGEMENT BANNIÈRE ADMOB
```
**Solution** : Consultez le code d'erreur dans les logs

### La Bannière S'Affiche en Rouge

**Cause** : Ligne de debug non retirée
**Solution** : Vérifiez que cette ligne n'existe PAS dans `AdMobBannerHandler.cs` :
```csharp
_adView.SetBackgroundColor(Android.Graphics.Color.Red);  // ❌ NE DOIT PAS EXISTER
```

### La Bannière Affiche "Chargement publicité..."

**Cause** : Le handler natif Android ne s'est pas activé
**Solution** : Vérifiez que le handler est bien enregistré dans `MauiProgram.cs` :
```csharp
#if ANDROID
handlers.AddHandler<DonTroc.Views.AdBannerView, AdMobBannerHandler>();
#endif
```

---

## 📊 Métriques à Surveiller

Une fois en production, surveillez dans AdMob Console :

1. **Taux de remplissage** (Fill Rate) : Doit être > 80%
2. **Impressions** : Nombre de fois que la bannière a été affichée
3. **Clics** : Nombre de clics sur la bannière
4. **CTR** (Click-Through Rate) : Taux de clics (typiquement 0.5-2%)
5. **Revenus** : Gains générés par les publicités

---

## 📝 Résumé des Changements

| Fichier | Changement | Statut |
|---------|-----------|--------|
| `AdMobBannerHandler.cs` | Retrait fond rouge debug | ✅ Fait |
| `AdMobBannerHandler.cs` | Retrait fond bleu MAUI | ✅ Fait |
| `AdMobBannerHandler.cs` | Nettoyage using inutiles | ✅ Fait |
| `firebase_rules.json` | Index UserId ajoutés | ✅ Déployé |

---

## 🎯 Prochaines Étapes

1. ✅ **Tester en Debug** - Vérifier que la bannière s'affiche correctement
2. ⏳ **Valider les logs** - Confirmer les messages de succès
3. ⏳ **Tester sur appareil réel** - Déployer sur un téléphone Android
4. ⏳ **Configurer l'ID de production** - Une fois satisfait des tests
5. ⏳ **Déployer en Release** - Build de production signé

---

**Dernière mise à jour** : 1er décembre 2025  
**Mode actuel** : TEST (ID Google officiel)  
**Prêt pour production** : ⚠️ Nécessite configuration ID AdMob


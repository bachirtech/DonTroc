# 🔍 Diagnostic - Bannière AdMob Invisible

## 🎯 Problème Actuel

La bannière AdMob se charge avec **succès** dans les logs :
```
✅✅✅ BANNIÈRE ADMOB CHARGÉE AVEC SUCCÈS ✅✅✅
Width: 900 pixels, Height: 140 pixels
```

**MAIS** elle n'est **pas visible** à l'écran.

---

## 🟢 Test de Visibilité du Container

### Étape 1 : Vérifier le Container Vert

J'ai ajouté un **fond VERT** temporaire au container. Quand vous lancez l'app maintenant, vous devriez voir :

#### ✅ **SI VOUS VOYEZ UNE BANDE VERTE**
- Le container MAUI est visible ✅
- Le problème vient de l'AdView native qui est **cachée derrière** le container
- **Solution** : Passer à l'Étape 2

#### ❌ **SI VOUS NE VOYEZ RIEN (PAS DE VERT)**
- Le container MAUI est invisible ou a une taille de 0
- Le problème vient du **parent MAUI** (Grid de la page)
- **Solution** : Passer à l'Étape 3

---

## Étape 2 : Container Vert Visible → Problème AdView

### Diagnostic

Si vous voyez la bande verte, cela signifie que le container est visible mais l'AdView est cachée.

### Solution

Le problème vient probablement de la superposition des vues. L'AdView est ajoutée au container mais **derrière** le fond vert.

**Modifiez `AdMobBannerHandler.cs`** :

```csharp
// Ligne ~60 - RETIREZ cette ligne
container.SetBackgroundColor(Android.Graphics.Color.Green);

// ET ajoutez APRÈS la création de l'AdView (ligne ~120)
_adView.SetBackgroundColor(Android.Graphics.Color.Yellow);  // Fond jaune pour l'AdView
_adView.BringToFront();  // Mettre l'AdView au premier plan
```

Relancez l'app :
- **Bande JAUNE** = L'AdView est visible mais pas la pub (problème AdMob)
- **Publicité visible** = Tout fonctionne ! Retirez le fond jaune

---

## Étape 3 : Rien de Visible → Problème Layout MAUI

### Diagnostic

Si vous ne voyez **ni vert ni bannière**, le container MAUI n'est pas affiché.

### Causes Possibles

1. **Le Grid.Row n'a pas de hauteur définie**
2. **AdBannerView est écrasée par les autres éléments**
3. **Le parent Grid a un problème de layout**

### Solution A : Forcer la Hauteur du Grid.Row

**Ouvrez `AnnoncesView.xaml`** (ou DashboardView, ProfilView) :

```xml
<!-- AVANT -->
<Grid RowDefinitions="Auto, Auto, Auto, *">
    <views:AdBannerView Grid.Row="0" />
    
<!-- APRÈS -->
<Grid RowDefinitions="60, Auto, Auto, *">  <!-- Forcer 60 pixels -->
    <views:AdBannerView Grid.Row="0" HeightRequest="60" BackgroundColor="Red" />
```

Si vous voyez une **bande ROUGE**, le container MAUI fonctionne maintenant !

### Solution B : Retirer le Grid.Row et Mettre en StackLayout

```xml
<StackLayout Padding="10" Spacing="10">
    <views:AdBannerView HeightRequest="60" BackgroundColor="Red" />
    
    <Border Style="{StaticResource InputContainer}">
        <SearchBar ... />
    </Border>
    
    <!-- Reste du contenu -->
</StackLayout>
```

---

## 📊 Tableau de Diagnostic

| Ce Que Vous Voyez | Diagnostic | Solution |
|-------------------|------------|----------|
| **Bande VERTE** | Container visible, AdView cachée | Étape 2 - BringToFront() |
| **Bande JAUNE** | AdView visible, pub non chargée | Vérifier ID AdMob |
| **Rien du tout** | Container invisible | Étape 3 - Forcer hauteur Grid |
| **Bande ROUGE** | Container MAUI fonctionne | Enlever rouge, tester AdMob |
| **Publicité !** | **TOUT FONCTIONNE !** | Retirer tous les fonds de debug |

---

## 🚀 Actions Immédiates

### 1. Lancez l'Application

```bash
cd /Users/aa1/RiderProjects/DonTroc
dotnet build DonTroc/DonTroc.csproj -t:Run -c Debug -f net8.0-android
```

### 2. Naviguez vers la Page des Annonces

### 3. Observez le Haut de la Page

**Que voyez-vous ?**
- [ ] Une bande VERTE (60-140 pixels de haut)
- [ ] Une bande d'une autre couleur
- [ ] Une publicité Google
- [ ] Rien du tout

### 4. Rapportez-moi Ce Que Vous Voyez

Dites-moi **exactement** ce que vous voyez et je vous donnerai la solution précise.

---

## 🔧 Code Debug Actuel

### Dans `AdMobBannerHandler.cs` (ligne ~60)
```csharp
// DEBUG TEMPORAIRE: Fond vert pour voir si le container est visible
container.SetBackgroundColor(Android.Graphics.Color.Green);
```

### Dans `CreatePlatformView()` (ligne ~52-58)
```csharp
var layoutParams = new Android.Widget.FrameLayout.LayoutParams(
    Android.Widget.FrameLayout.LayoutParams.MatchParent,
    heightInPixels  // 60dp * density = ~168 pixels
);
container.LayoutParameters = layoutParams;
container.SetMinimumHeight(heightInPixels);
```

---

## 📝 Checklist Debug

- [x] Container a un fond vert temporaire
- [x] Container a des dimensions forcées (MATCH_PARENT x 168px)
- [x] AdView est créée et ajoutée au container
- [x] AdView se charge avec succès (logs ✅)
- [ ] **À VÉRIFIER** : Container est visible à l'écran
- [ ] **À VÉRIFIER** : AdView est au premier plan

---

## 🎯 Prochaine Étape

**Lancez l'app maintenant** et dites-moi :
1. Voyez-vous du **VERT** en haut de la page ?
2. Quelle est la **hauteur** approximative de ce que vous voyez ?
3. Y a-t-il **quelque chose** au-dessus du contenu normal ?

Avec ces informations, je pourrai vous donner la solution exacte ! 🚀

---

**Dernière mise à jour** : 1er décembre 2025  
**Mode Debug** : Container VERT actif  
**Objectif** : Identifier pourquoi la bannière n'est pas visible


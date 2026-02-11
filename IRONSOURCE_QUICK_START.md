# 🚀 Guide rapide d'installation IronSource/LevelPlay

## Étape 1 : Créer un compte Unity LevelPlay

1. Allez sur : **https://unity.com/products/mediation**
2. Cliquez sur "Get Started" 
3. Créez un compte ou connectez-vous
4. Créez une nouvelle application dans le dashboard

## Étape 2 : Obtenir votre App Key

1. Dans le dashboard Unity LevelPlay
2. Allez dans **Apps** → Votre application
3. Copiez l'**App Key** (format : `1a2b3c4d5`)

## Étape 3 : Télécharger le SDK IronSource

⚠️ **Important** : Le SDK IronSource n'est PAS disponible sur Maven Central public.

### Option A : Depuis le dashboard Unity (Recommandé)

1. Allez sur : **https://developers.is.com/ironsource-mobile/android/android-sdk/**
2. Connectez-vous avec votre compte Unity
3. Téléchargez le SDK Android (fichier `.aar`)
4. Copiez-le dans `IronSource.Binding/Jars/mediationsdk.aar`

### Option B : Via Android Studio / Gradle

1. Créez un projet Android Studio temporaire
2. Ajoutez dans `build.gradle` :
   ```gradle
   implementation 'com.ironsource.sdk:mediationsdk:8.+'
   ```
3. Synchronisez le projet
4. Le SDK sera dans `~/.gradle/caches/`
5. Cherchez `mediationsdk-X.X.X.aar`
6. Copiez dans `IronSource.Binding/Jars/mediationsdk.aar`

## Étape 4 : Compiler le binding

```bash
cd /Users/aa1/RiderProjects/DonTroc
dotnet build IronSource.Binding/IronSource.Binding.csproj
```

### En cas d'erreurs de compilation

Les erreurs sont normales au premier build. Pour les résoudre :

1. **Ouvrez** `IronSource.Binding/Transforms/Metadata.xml`
2. **Ajoutez des exclusions** pour les classes problématiques :
   ```xml
   <remove-node path="/api/package[@name='com.problematic.package']" />
   ```
3. **Relancez** le build

## Étape 5 : Configurer l'App Key

Modifiez `DonTroc/Services/Providers/IronSourceProvider.cs` :

```csharp
// Ligne 22 - Remplacez par votre App Key
private const string AppKey = "1a2b3c4d5";  // Votre vraie App Key
```

## Étape 6 : Activer le binding

Dans `DonTroc/Services/Providers/IronSourceProvider.cs` :

1. **Décommentez** le code dans la méthode `Initialize()` (lignes 68-85)
2. **Décommentez** les listeners à la fin du fichier (lignes 225-315)
3. **Ajoutez** le using en haut du fichier :
   ```csharp
   using IronSource.SDK;
   ```

## Étape 7 : Ajouter la référence au projet

Modifiez `DonTroc/DonTroc.csproj` pour ajouter :

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net9.0-android'">
  <ProjectReference Include="..\IronSource.Binding\IronSource.Binding.csproj" />
</ItemGroup>
```

## Étape 8 : Configurer AndroidManifest.xml

Ajoutez dans `DonTroc/Platforms/Android/AndroidManifest.xml` :

```xml
<!-- Permissions IronSource -->
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />

<!-- Activité IronSource (dans <application>) -->
<activity
    android:name="com.ironsource.sdk.controller.ControllerActivity"
    android:configChanges="orientation|screenSize"
    android:hardwareAccelerated="true" />
```

## Étape 9 : Enregistrer le provider

Modifiez `DonTroc/MauiProgram.cs` pour ajouter :

```csharp
// Dans ConfigureServices
builder.Services.AddSingleton<IAdsService>(sp =>
{
    var service = new MediatedAdsService();
    
#if ANDROID
    service.RegisterProvider(new AdMobProvider());
    service.RegisterProvider(new IronSourceProvider());
#endif
    
    return service;
});
```

## Étape 10 : Tester

```bash
dotnet build DonTroc/DonTroc.csproj -c Debug -f net9.0-android
```

---

## 📋 Checklist

- [ ] Compte Unity LevelPlay créé
- [ ] App Key obtenue
- [ ] SDK téléchargé (`IronSource.Binding/Jars/mediationsdk.aar`)
- [ ] Binding compilé sans erreurs
- [ ] App Key configurée dans `IronSourceProvider.cs`
- [ ] Code décommenté dans `IronSourceProvider.cs`
- [ ] Référence ajoutée dans `DonTroc.csproj`
- [ ] AndroidManifest.xml mis à jour
- [ ] Provider enregistré dans `MauiProgram.cs`
- [ ] Build réussi

---

## 🔧 Résolution des problèmes courants

### Erreur : "Could not find mediationsdk.aar"
→ Téléchargez le SDK avec `./IronSource.Binding/download_sdk.sh`

### Erreur : "Type not found: IronSource.SDK.IronSource"
→ Le binding n'est pas encore compilé ou a des erreurs

### Erreur : "Duplicate class"
→ Ajoutez des exclusions dans `Metadata.xml`

### L'app crash au démarrage
→ Vérifiez que l'App Key est correcte
→ Vérifiez les permissions dans AndroidManifest.xml

---

## 📚 Ressources

- **Documentation IronSource** : https://developers.is.com/ironsource-mobile/android/
- **Dashboard Unity LevelPlay** : https://unity.com/products/mediation
- **Binding Xamarin Guide** : https://learn.microsoft.com/xamarin/android/platform/binding-java-library/

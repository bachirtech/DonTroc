# IronSource.Binding - Projet de binding .NET MAUI

Ce projet permet d'utiliser le SDK IronSource/LevelPlay dans une application .NET MAUI.

## 📋 Prérequis

1. **SDK IronSource** : Doit être téléchargé manuellement (voir ci-dessous)
2. **.NET 9 SDK** : Installé et configuré
3. **Compte Unity LevelPlay** : Pour obtenir l'App Key

## 📥 Téléchargement du SDK

Le SDK IronSource n'est **pas disponible sur Maven Central public**. Vous devez le télécharger manuellement.

### Option 1 : Depuis le dashboard Unity LevelPlay (Recommandé)

1. Allez sur https://developers.is.com/ironsource-mobile/android/android-sdk/
2. Connectez-vous à votre compte Unity
3. Téléchargez le SDK Android (fichier `.aar`)
4. Copiez-le dans `Jars/mediationsdk.aar`

### Option 2 : Via Gradle (si vous avez Android Studio)

1. Créez un projet Android Studio
2. Ajoutez la dépendance dans `build.gradle` :
   ```gradle
   dependencies {
       implementation 'com.ironsource.sdk:mediationsdk:8.+'
   }
   ```
3. Synchronisez le projet
4. Le SDK sera téléchargé dans `~/.gradle/caches/`
5. Cherchez le fichier `mediationsdk-X.X.X.aar`
6. Copiez-le dans `Jars/mediationsdk.aar`

## 🔧 Compilation

```bash
dotnet build IronSource.Binding.csproj
```

### En cas d'erreurs

Les erreurs de binding sont normales. Pour les résoudre :

1. **Ouvrez** `Transforms/Metadata.xml`
2. **Ajoutez des exclusions** pour les classes problématiques :
   ```xml
   <remove-node path="/api/package[@name='com.problematic.package']" />
   ```
3. **Relancez** le build

## 📁 Structure du projet

```
IronSource.Binding/
├── IronSource.Binding.csproj  # Configuration du projet
├── Jars/
│   └── mediationsdk.aar       # SDK IronSource (à ajouter)
├── Transforms/
│   ├── Metadata.xml           # Transformations Java → C#
│   ├── EnumFields.xml         # Mappings d'énumérations
│   └── EnumMethods.xml        # Méthodes d'énumérations
├── Additions/
│   └── IronSourceExtensions.cs # Extensions C# personnalisées
└── proguard-ironsource.txt    # Configuration ProGuard
```

## 📚 Documentation

- [Documentation IronSource](https://developers.is.com/ironsource-mobile/android/)
- [Guide de binding Xamarin](https://learn.microsoft.com/xamarin/android/platform/binding-java-library/)
- [Guide d'intégration DonTroc](../IRONSOURCE_QUICK_START.md)

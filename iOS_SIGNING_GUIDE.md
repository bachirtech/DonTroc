# Configuration de signature iOS pour DonTroc
# Ce fichier contient les instructions pour configurer la signature iOS

## Prérequis iOS

Pour signer votre application iOS, vous aurez besoin de :

### 1. Compte développeur Apple
- Inscription au Apple Developer Program (99$/an)
- Accès à la Apple Developer Console

### 2. Certificats de développement
- Certificat de développement iOS (iPhone Developer)
- Certificat de distribution iOS (iPhone Distribution)

### 3. Identifiants d'application
- App ID configuré dans la Apple Developer Console
- Bundle ID : com.bachirdev.dontroc

### 4. Profils de provisioning
- Profil de développement pour les tests
- Profil de distribution pour l'App Store

## Configuration automatique

Pour une configuration automatique avec Visual Studio for Mac ou Xcode :

1. Connectez-vous avec votre Apple ID dans Xcode
2. Sélectionnez "Automatically manage signing"
3. Choisissez votre équipe de développement

## Configuration manuelle

Si vous préférez une configuration manuelle :

1. Téléchargez vos certificats et profils depuis la Developer Console
2. Installez-les dans le Keychain (double-clic sur les fichiers .p12 et .mobileprovision)
3. Configurez les propriétés dans DonTroc.csproj :

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND $(TargetFramework.Contains('-ios'))">
    <CodesignKey>iPhone Distribution: Votre Nom</CodesignKey>
    <CodesignProvision>Nom de votre profil de distribution</CodesignProvision>
    <CodesignEntitlements>Platforms/iOS/Entitlements.plist</CodesignEntitlements>
    <BuildIpa>true</BuildIpa>
</PropertyGroup>
```

## Scripts de build iOS

Pour compiler pour iOS en mode Release :

```bash
# Build pour iOS
dotnet build -c Release -f net8.0-ios

# Publier pour l'App Store
dotnet publish -c Release -f net8.0-ios -p:ArchiveOnBuild=true
```

## Notes importantes

- Les builds iOS nécessitent un Mac avec Xcode installé
- La signature iOS est plus complexe que Android
- Testez toujours sur des appareils physiques avant la distribution
- Les profils de développement sont limités à 100 appareils par an

## Débogage iOS sans signature

Pour le développement et les tests, la signature est désactivée par défaut.
Vous pouvez déployer sur le simulateur iOS sans certificats.

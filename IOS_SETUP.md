# Configuration iOS pour DonTroc

## Fichiers créés/modifiés

### ✅ Fichiers de configuration créés

1. **`Platforms/iOS/Info.plist`**
   - Permissions (Caméra, Photos, Localisation, Microphone)
   - Configuration AdMob (GADApplicationIdentifier)
   - SKAdNetwork pour le suivi publicitaire
   - Google Sign-In URL Schemes
   - App Transport Security

2. **`Platforms/iOS/Entitlements.plist`**
   - Push Notifications
   - Keychain Sharing
   - Application Groups

3. **`Platforms/iOS/Resources/PrivacyInfo.xcprivacy`** (mis à jour)
   - Déclaration des données collectées
   - APIs nécessitant une justification
   - Configuration App Tracking Transparency

4. **`Platforms/iOS/AdMobNativeService.cs`**
   - Implémentation native d'AdMob pour iOS

### ✅ Fichiers existants

- `Platforms/iOS/GoogleService-Info.plist` - ✅ Déjà configuré
- `Platforms/iOS/Linker.xml` - ✅ Déjà configuré
- `Platforms/iOS/AppDelegate.cs` - ✅ Mis à jour avec Firebase et AdMob

---

## 🔴 Actions requises avant publication

### 1. Compte Apple Developer (99$/an)
- Créer un compte sur [developer.apple.com](https://developer.apple.com)
- Enregistrer l'App ID: `com.bachirdev.dontroc`

### 2. Certificats et Profils de provisioning
```bash
# Dans Xcode → Preferences → Accounts → Gérer les certificats
# Créer:
# - Distribution Certificate (pour App Store)
# - Development Certificate (pour tests)
# - Provisioning Profile pour DonTroc
```

### 3. Configurer AdMob pour iOS
- Aller sur [admob.google.com](https://admob.google.com)
- Créer une nouvelle application iOS
- Obtenir les Unit IDs iOS pour:
  - Banner
  - Interstitial
  - Rewarded
- Mettre à jour `Platforms/iOS/AdMobNativeService.cs` avec les vrais IDs

### 4. Configurer les certificats de signature

Modifier `DonTroc.csproj` pour Release iOS:
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND $(TargetFramework.Contains('-ios'))">
    <CodesignKey>iPhone Distribution: Votre Nom (TEAM_ID)</CodesignKey>
    <CodesignProvision>DonTroc App Store</CodesignProvision>
    <BuildIpa>true</BuildIpa>
</PropertyGroup>
```

### 5. App Store Connect
- Créer l'application sur [App Store Connect](https://appstoreconnect.apple.com)
- Configurer les métadonnées (description, screenshots, etc.)
- Configurer les achats in-app si nécessaire

---

## 📱 Tester sur iOS

### Option 1: Simulateur iOS
```bash
cd /Users/aa1/RiderProjects/DonTroc
dotnet build -f net9.0-ios
# Ouvrir dans le simulateur iOS depuis Rider ou Visual Studio
```

### Option 2: Appareil physique (nécessite certificat)
```bash
dotnet build -f net9.0-ios -c Debug
# Déployer via Xcode ou directement depuis Rider
```

---

## 📋 Checklist avant soumission App Store

- [ ] Compte Apple Developer actif
- [ ] Certificats de distribution créés
- [ ] Profil de provisioning App Store créé
- [ ] AdMob IDs iOS configurés
- [ ] Screenshots iPhone (6.7", 6.5", 5.5")
- [ ] Screenshots iPad (12.9", 11")
- [ ] Icône d'app 1024x1024
- [ ] Description de l'app
- [ ] Politique de confidentialité URL
- [ ] Catégorie de l'app sélectionnée
- [ ] Classification d'âge remplie
- [ ] Test sur appareil réel effectué
- [ ] Build IPA généré et uploadé

---

## 🔧 Commandes utiles

```bash
# Build iOS Debug
dotnet build -f net9.0-ios -c Debug

# Build iOS Release (nécessite certificats)
dotnet build -f net9.0-ios -c Release

# Nettoyer le projet
dotnet clean

# Restaurer les packages
dotnet restore
```

---

## ⚠️ Notes importantes

1. **Sign in with Apple**: Si vous utilisez Google Sign-In, Apple exige aussi Sign in with Apple comme option.

2. **ATT (App Tracking Transparency)**: iOS 14.5+ requiert une demande explicite pour le tracking. C'est configuré dans Info.plist.

3. **Privacy Manifest**: Obligatoire depuis iOS 17. Déjà configuré dans PrivacyInfo.xcprivacy.

4. **Push Notifications**: Nécessite une configuration APNs dans Apple Developer et Firebase.


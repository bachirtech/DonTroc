# 🍎 Checklist soumission App Store — DonTroc

> Document généré pour la **Phase 4–8** du plan iOS.
> Bundle ID : `com.bachirdev.dontroc` · Team ID : `ZF7KX4SVSJ` · Version : voir `Info.plist` (`CFBundleShortVersionString` / `CFBundleVersion`)

---

## ✅ Pré-requis (à faire **une seule fois**)

### Apple Developer Portal — https://developer.apple.com/account/

- [x] **App ID** `com.bachirdev.dontroc` créé avec capabilities :
  - [x] Push Notifications
  - [ ] Sign In with Apple *(seulement si l'app expose Google/Facebook SSO — sinon skip)*
  - [ ] Associated Domains *(deep links — optionnel pour v1)*
- [x] **APNs Auth Key (.p8)** générée → fichier sauvegardé dans `~/Documents/AppleKeys/DonTroc/AuthKey_XXXXXXXXXX.p8`
  - Key ID : `__________` *(10 caractères, à reporter)*
  - Team ID : `ZF7KX4SVSJ`
- [x] **Membership active** : Apple Developer Program (bassirou balde)

### Xcode → Settings → Accounts

- [x] Compte Apple connecté (équipe `Bassirou Balde — ZF7KX4SVSJ`)
- [x] Cert **Apple Development** créé
- [x] Cert **Apple Distribution** créé
  - Vérification : `security find-identity -v -p codesigning` doit lister les **2** identités

### Firebase Console — https://console.firebase.google.com/

- [x] App iOS Firebase enregistrée avec Bundle ID `com.bachirdev.dontroc`
- [x] `GoogleService-Info.plist` téléchargé et placé dans `DonTroc/Platforms/iOS/`
- [ ] **APNs Auth Key (.p8) uploadée** dans Firebase → Project Settings → Cloud Messaging → Apple app configuration
  - Key ID : (même que celui d'Apple)
  - Team ID : `ZF7KX4SVSJ`

### App Store Connect — https://appstoreconnect.apple.com/

- [ ] Fiche app créée (Bundle ID `com.bachirdev.dontroc`, SKU `dontroc-ios-001`)
- [ ] **Privacy Policy URL** publiée en HTTPS (héberger `docs/privacy-policy.html`)
- [ ] **Métadonnées** remplies (sous-titre, mots-clés, description, captures iPhone 6.9")
- [ ] **App Privacy** questionnaire validé (Email, UserID, Localisation, Photos, IDFA)
- [ ] **Age Rating** rempli (probablement 4+)

---

## 🔧 Configuration projet — état actuel

| Fichier | Élément | Valeur | OK |
|---|---|---|---|
| `Info.plist` | `CFBundleIdentifier` | `com.bachirdev.dontroc` | ✅ |
| `Info.plist` | `MinimumOSVersion` | 15.0 | ✅ |
| `Info.plist` | `ITSAppUsesNonExemptEncryption` | `false` | ✅ |
| `Info.plist` | `NSPrivacyTracking` | `true` (cohérent ATT/AdMob) | ✅ |
| `Info.plist` | `NSPrivacyTrackingDomains` | googleads, googlesyndication, GA | ✅ |
| `Info.plist` | `SKAdNetworkItems` | ~85 IDs (liste Google AdMob 2024) | ✅ |
| `Info.plist` | `UIRequiredDeviceCapabilities` | absent (arm64 implicite) | ✅ |
| `Info.plist` | `NSPrivacyAccessedAPITypes` | 4 catégories déclarées | ✅ |
| `Info.plist` | Toutes les `*UsageDescription` | Camera, Photos, Location, Mic, Calendar, FaceID, ATT | ✅ |
| `Entitlements.plist` (Debug) | `aps-environment` | `development` | ✅ |
| `Entitlements.Release.plist` | `aps-environment` | `production` | ✅ |
| `Entitlements.Release.plist` | `background-modes` | `fetch`, `remote-notification` | ✅ |
| `Entitlements.Release.plist` | `default-data-protection` | `NSFileProtectionComplete` | ✅ |
| `DonTroc.csproj` | Bloc Debug device | `EnableCodeSigning=true`, `Apple Development`, `Automatic` | ✅ |
| `DonTroc.csproj` | Bloc Release | `Apple Distribution`, `Automatic`, `BuildIpa=true`, `ArchiveOnBuild=true`, `RuntimeIdentifier=ios-arm64` | ✅ |
| `Linker.xml` | Préservation Firebase / AdMob / Plugin.Firebase.Bundled | ✅ | ✅ |
| `GoogleService-Info.plist` | `BUNDLE_ID` | `com.bachirdev.dontroc` | ✅ |
| `AppDelegate.cs` | Init Firebase **avant** `base.FinishedLaunching` | ✅ | ✅ |
| `AppDelegate.cs` | APNs registration manuelle (proxy disabled) | ✅ | ✅ |
| `AppDelegate.cs` | UITabBar opaque (anti flash transitions) | ✅ | ✅ |
| `AppDelegate.cs` | `KeyboardAutoManagerScroll.Disconnect()` | ✅ (fix bande grise MAUI 9) | ✅ |
| `csproj` | TabBar icons `Resize="True" BaseSize="24,24"` | ✅ (fix bande grise) | ✅ |

---

## 🚀 Phase 5 — Premier déploiement iPhone physique

```bash
# 1. iPhone branché, déverrouillé, "Trust This Computer", Mode développeur ON
# 2. Lister les devices :
xcrun devicectl list devices

# 3. Build Debug device :
./scripts/ios_build_device.sh
# OU explicite :
dotnet build DonTroc/DonTroc.csproj -f net9.0-ios -c Debug \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:CodesignKey="Apple Development: bassirou balde (85FYUJRVMP)" \
  -p:CodesignProvision=Automatic

# 4. Installer sur device :
xcrun devicectl device install app --device <UDID> \
  DonTroc/bin/Debug/net9.0-ios/ios-arm64/DonTroc.app
```

**Au 1er lancement sur iPhone** : Réglages → Général → VPN & gestion d'appareils → Apple Development: bassirou balde → **Faire confiance**.

### Critères ✅
- [ ] App installée et lancée
- [ ] Login Google/Email fonctionne
- [ ] Firebase reçoit l'event d'auth (visible dans Firebase Console → Authentication)
- [ ] AdMob charge une bannière (pub test)
- [ ] Notification push test reçue (envoyer via Firebase Console → Cloud Messaging)

---

## 📦 Phase 6 — Build IPA Release pour App Store

### Cert Distribution disponible
```
Apple Distribution: bassirou balde (ZF7KX4SVSJ) — 4DB4618573F4BD340F3F75FD84FB096529BA3D9F
```

### Build
```bash
# Option A — script :
./scripts/ios_build_release_ipa.sh

# Option B — explicite :
dotnet publish DonTroc/DonTroc.csproj -f net9.0-ios -c Release \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:ArchiveOnBuild=true \
  -p:BuildIpa=true \
  -p:CodesignKey="Apple Distribution: bassirou balde (ZF7KX4SVSJ)" \
  -p:CodesignProvision=Automatic
```

### Localisation IPA
```
DonTroc/bin/Release/net9.0-ios/ios-arm64/publish/DonTroc.ipa
```

### Vérification signature
```bash
# Extraire et inspecter le .app dans l'IPA
mkdir -p /tmp/dontroc_ipa && cd /tmp/dontroc_ipa
unzip -q ~/RiderProjects/DonTroc/DonTroc/bin/Release/net9.0-ios/ios-arm64/publish/DonTroc.ipa
xcrun codesign -dv --verbose=4 Payload/DonTroc.app 2>&1 | grep -E "Authority|TeamIdentifier|Identifier"
# Doit afficher :
#   Authority=Apple Distribution: bassirou balde (ZF7KX4SVSJ)
#   TeamIdentifier=ZF7KX4SVSJ
#   Identifier=com.bachirdev.dontroc

# Vérifier les entitlements embarqués
xcrun codesign -d --entitlements - Payload/DonTroc.app 2>&1 | grep -A1 "aps-environment"
# Doit afficher : <string>production</string>
```

### Upload
```bash
# Option A : Transporter.app (Mac App Store) — glisser/déposer le .ipa
open -a Transporter

# Option B : altool (nécessite App Store Connect API Key préalable)
xcrun altool --upload-app -f DonTroc.ipa -t ios \
  --apiKey <KEY_ID> --apiIssuer <ISSUER_ID>
```

### Critères ✅
- [ ] IPA produit sans erreur de signing
- [ ] `codesign -dv` confirme `Apple Distribution` + Team `ZF7KX4SVSJ`
- [ ] Upload Transporter affiche **"Delivery successful"**
- [ ] Build apparaît dans App Store Connect → TestFlight → Builds (statut "Processing")

---

## 🧪 Phase 7 — TestFlight

- [ ] Build "Processed" (15–60 min après upload)
- [ ] **Test Information** rempli (What to Test, contact email, privacy URL)
- [ ] **Internal Testing** : groupe créé, testeurs ajoutés (max 100, immédiat)
- [ ] **External Testing** : groupe Beta soumis à Beta App Review (~24h, 1ère fois uniquement)
- [ ] Email TestFlight reçu et installation OK sur device de test

---

## 📤 Phase 8 — Soumission App Store finale

- [ ] Build TestFlight validé sélectionné dans **App Store → 1.0 Prepare for Submission**
- [ ] **Notes for the reviewer** : compte démo (login + mdp) + instructions test
- [ ] Cliquer **Add for Review** → **Submit to App Review**
- [ ] Attente Apple Review : 24h–72h en moyenne

### ⚠️ Pièges fréquents au review

| Erreur | Cause | Fix |
|---|---|---|
| Rejet **§4.8** | Google/Facebook SSO sans Sign In with Apple | Activer SIWA si SSO tiers présent |
| Rejet **App Privacy** | AdMob déclaré sans ATT prompt visible | Vérifier que `ATTrackingManager.requestTrackingAuthorization` est appelé au 1er lancement |
| **ITMS-90713** | `NS*UsageDescription` manquant pour une API utilisée | Vérifier toutes les permissions runtime |
| **ITMS-91053** | API privée sans déclaration `PrivacyInfo.xcprivacy` | Vérifier que le fichier est inclus (déjà ✅) |
| Crash launch sur reviewer | Firebase init après `base.FinishedLaunching` | OK ✅ — init avant dans AppDelegate |

---

## 📌 Identifiants à conserver précieusement

| Élément | Valeur |
|---|---|
| Bundle ID | `com.bachirdev.dontroc` |
| Team ID | `ZF7KX4SVSJ` |
| App Store SKU | `dontroc-ios-001` |
| Cert Development | `85FYUJRVMP` (suffixe affiché par Xcode) |
| Cert Distribution SHA1 | `4DB4618573F4BD340F3F75FD84FB096529BA3D9F` |
| AdMob App ID iOS | `ca-app-pub-5085236088670848~2688254033` |
| APNs Key ID | `__________` *(à compléter)* |
| App Store Connect API Key ID | `__________` *(à créer si upload via altool)* |


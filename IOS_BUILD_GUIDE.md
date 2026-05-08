# 🍎 Guide iOS — DonTroc

Configuration et procédures pour développer, tester et publier l'app iOS.

## ✅ État actuel détecté

| Élément | Valeur |
|---|---|
| **Bundle ID** | `com.bachirdev.dontroc` |
| **Team ID** | `ZF7KX4SVSJ` |
| **Apple ID** | `bassiroubalde91@yahoo.com` (User `85FYUJRVMP`) |
| **Certificat installé** | ✅ Apple Development (`6A4B561866C098A97726441AAF9713D2745C6D0C`) |
| **Provisioning Profiles** | ❌ Aucun installé |
| **Workloads .NET** | ✅ ios 26.4.9015 + maui-ios 9.0.120 |
| **Xcode** | ✅ 26.4.1 |

> ℹ️ Vous avez un certificat **Development** (test) mais pas encore **Distribution** (App Store).
> Vous pouvez tester sur simulateur (immédiat) et sur iPhone physique (après création d'un profile),
> mais pas encore publier sur l'App Store.

---

## 1️⃣ Tester sur simulateur (PRÊT — aucun setup requis)

```bash
./scripts/ios_run_simulator.sh                  # iPhone 17 par défaut
./scripts/ios_run_simulator.sh "iPhone 16 Pro"  # autre simulateur
```

Le script :
1. Compile en `Debug` pour `iossimulator-arm64`
2. Démarre le simulateur
3. Installe et lance l'app

**Depuis Rider** : sélectionner le framework `net9.0-ios`, choisir un simulateur dans la liste, puis bouton ▶️ Run.

> 💡 Si Rider ne propose pas de simulateurs : **File > Settings > Build, Execution, Deployment > iOS** → vérifier que Xcode 26.4 est sélectionné.

---

## 2️⃣ Tester sur iPhone physique

### Prérequis (one-time setup)

#### A. Récupérer l'UDID de votre iPhone

Brancher l'iPhone, puis :
```bash
xcrun devicectl list devices
# ou via Xcode > Window > Devices and Simulators
```

#### B. Enregistrer l'iPhone sur Apple Developer

1. Aller sur https://developer.apple.com/account/resources/devices/add
2. Nom : `iPhone DonTroc Dev`
3. Coller l'UDID

#### C. Créer un App ID

https://developer.apple.com/account/resources/identifiers/add/bundleId
- Description : `DonTroc`
- Bundle ID : **Explicit** → `com.bachirdev.dontroc`
- Capabilities à cocher (correspond à `Entitlements.plist`) :
  - ✅ Push Notifications
  - ✅ Sign in with Apple (recommandé car Google Sign-In utilisé)
  - ✅ Associated Domains (si deep links)

#### D. Créer un Provisioning Profile de Development

https://developer.apple.com/account/resources/profiles/add
- Type : **iOS App Development**
- App ID : `com.bachirdev.dontroc`
- Certificats : cocher `Apple Development: bassiroubalde91@yahoo.com`
- Devices : cocher votre iPhone
- Nom : `DonTroc Dev`
- → **Download** puis **double-cliquer** le `.mobileprovision` pour l'installer

Vérifier l'installation :
```bash
ls ~/Library/MobileDevice/Provisioning\ Profiles/
```

#### E. Build & déploiement

```bash
./scripts/ios_build_device.sh
# Puis :
xcrun devicectl list devices             # noter le UDID
xcrun devicectl device install app --device <UDID> \
    DonTroc/bin/Debug/net9.0-ios/ios-arm64/DonTroc.app
```

> 💡 Plus simple : depuis **Rider**, branchez l'iPhone, sélectionnez-le dans la barre d'outils
> et cliquez ▶️ Run.

---

## 3️⃣ Publier sur l'App Store

### Prérequis

#### A. Créer un certificat Apple Distribution

Dans **Xcode** : `Settings > Accounts > [votre compte] > Manage Certificates… > +` → **Apple Distribution**

Vérifier :
```bash
security find-identity -v -p codesigning | grep "Apple Distribution"
```

#### B. Créer un Provisioning Profile App Store

https://developer.apple.com/account/resources/profiles/add
- Type : **App Store Connect**
- App ID : `com.bachirdev.dontroc`
- Certificate : `Apple Distribution`
- Nom : `DonTroc App Store`
- → Télécharger et installer

#### C. Créer l'app sur App Store Connect

https://appstoreconnect.apple.com/apps → **+** → New App
- Bundle ID : `com.bachirdev.dontroc`
- SKU : `DONTROC001`
- Nom : DonTroc

#### D. Build IPA

```bash
./scripts/ios_build_release_ipa.sh
```

Le script :
1. Vérifie qu'un cert Distribution existe
2. Compile en `Release` avec trimming, LLVM, optimisations PNG
3. Signe avec le profile App Store
4. Génère un `.ipa` dans `DonTroc/bin/Release/net9.0-ios/`

#### E. Upload

```bash
# Option 1 : altool (CLI)
xcrun altool --upload-app -f DonTroc/bin/Release/net9.0-ios/DonTroc.ipa \
    -t ios -u bassiroubalde91@yahoo.com -p <APP_SPECIFIC_PASSWORD>

# Option 2 : Transporter.app (Mac App Store, GUI)
open -a Transporter
```

> 🔑 Créer un **App-Specific Password** : https://appleid.apple.com/account/manage > "App-Specific Passwords"

---

## 4️⃣ Configuration AdMob iOS

L'app utilise `Jc.GMA.iOS 12.7.1` côté natif. Avant publication :

1. Créer une app iOS sur https://admob.google.com
2. Obtenir l'**App ID iOS** (format `ca-app-pub-XXX~YYY`)
3. Mettre à jour `Platforms/iOS/Info.plist` (clé `GADApplicationIdentifier`)
4. Récupérer les **Unit IDs** Banner / Interstitial / Rewarded
5. Mettre à jour `Platforms/iOS/AdMobNativeService.cs`

---

## 🛠️ Dépannage

### Le simulateur ne se lance pas depuis Rider
```bash
# Forcer Rider à utiliser le bon Xcode :
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
xcrun simctl list devices available | head -5
```

### Erreur « No matching profiles found »
- Vérifier que le profile `.mobileprovision` est bien dans `~/Library/MobileDevice/Provisioning Profiles/`
- Vérifier que l'UDID de l'iPhone est inclus dans le profile (pour Development)
- Vérifier que le certificat utilisé pour signer est inclus dans le profile

### Erreur de signature
```bash
# Voir tous les certs valides :
security find-identity -v -p codesigning

# Voir quel profile est lié à quel App ID :
ls ~/Library/MobileDevice/Provisioning\ Profiles/ | while read p; do
  /usr/libexec/PlistBuddy -c "Print :Name" /dev/stdin <<< "$(security cms -D -i ~/Library/MobileDevice/Provisioning\ Profiles/$p 2>/dev/null)"
done
```

### Build OOM (Out of Memory)
Le csproj alloue déjà 8 GB Java heap. Si problème :
```bash
export DOTNET_gcServer=1
export DOTNET_GCHeapCount=4
```

---

## 📁 Scripts iOS

| Script | Usage |
|---|---|
| `scripts/ios_run_simulator.sh [SIM_NAME]` | Build + run sur simulateur (no signing) |
| `scripts/ios_build_device.sh` | Build signé pour iPhone physique (Development) |
| `scripts/ios_build_release_ipa.sh` | Build IPA pour App Store (Distribution) |

---

## 🔗 Liens utiles

- Apple Developer : https://developer.apple.com/account
- App Store Connect : https://appstoreconnect.apple.com
- Firebase Console (iOS) : https://console.firebase.google.com/project/dontroc-55570
- AdMob : https://admob.google.com
- Documentation .NET MAUI iOS publishing : https://learn.microsoft.com/dotnet/maui/ios/deployment/


# 📦 Dossier des APK de release

Ce dossier héberge les fichiers APK signés de DonTroc, accessibles publiquement
via GitHub Pages.

## ⬇️ Comment publier une nouvelle version

1. **Build** l'APK signé localement :
   ```bash
   cd DonTroc
   dotnet publish -f net9.0-android -c Release \
     /p:AndroidPackageFormat=apk \
     /p:AndroidKeyStore=true \
     /p:AndroidSigningKeyStore=../keystore/dontroc-release.keystore \
     /p:AndroidSigningStorePass=<pwd> \
     /p:AndroidSigningKeyAlias=<alias> \
     /p:AndroidSigningKeyPass=<pwd>
   ```

2. **Copier** l'APK généré ici :
   ```bash
   cp DonTroc/bin/Release/net9.0-android/publish/com.companyname.dontroc-Signed.apk \
      docs/downloads/DonTroc-latest.apk

   # Garder aussi une copie versionnée pour l'historique
   cp DonTroc/bin/Release/net9.0-android/publish/com.companyname.dontroc-Signed.apk \
      docs/downloads/DonTroc-v2.1.apk
   ```

3. **Mettre à jour** `app_config.json` (à la racine) :
   - Incrémenter `latest_version_code` et `latest_version_name`
   - Mettre à jour `release_notes`

4. **Pousser** la config Firebase :
   ```bash
   python release_app_config.py
   ```

5. **Commit & push** sur GitHub :
   ```bash
   git add docs/ app_config.json
   git commit -m "release: v2.1 - APK + page download"
   git push
   ```
   GitHub Pages met le contenu en ligne en ~1 minute.

6. **Notifier les utilisateurs** (push FCM) — voir `APP_UPDATE_SYSTEM.md`.

## 📂 Convention de nommage

- `DonTroc-latest.apk` → toujours la dernière version (référencé par `download.html`)
- `DonTroc-vX.Y.apk`   → archive versionnée (à conserver)

## 🔒 Sécurité

Ces APK sont **signés** avec `keystore/dontroc-release.keystore`. Ne JAMAIS publier
un APK non signé : Android refusera la mise à jour.


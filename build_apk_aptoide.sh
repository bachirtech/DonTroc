#!/bin/bash
# ============================================================
# Build APK signé pour Aptoide / Samsung / Huawei / sites APK
# ============================================================
# Aptoide n'accepte PAS les AAB, seulement les APK.
# Ce script force AndroidPackageFormat=apk pour ce build.
# ============================================================

set -e

cd "$(dirname "$0")/DonTroc"

# Charger les mots de passe du keystore
if [ -f "../keystore/signing.properties" ]; then
    export $(grep -v '^#' ../keystore/signing.properties | xargs)
    export DONTROC_KEYSTORE_PASS="${KeyStorePassword}"
    export DONTROC_KEY_PASS="${KeyPassword}"
fi

LOG="../build_apk_aptoide.log"
echo "🔨 Build APK (format APK pour stores alternatifs)…" | tee "$LOG"

dotnet publish \
    -f net9.0-android \
    -c Release \
    -p:AndroidPackageFormat=apk \
    -p:AndroidCreatePackagePerAbi=false \
    -p:RuntimeIdentifiers="android-arm64;android-arm" \
    2>&1 | tee -a "$LOG"

echo "" | tee -a "$LOG"
echo "✅ APK généré :" | tee -a "$LOG"
find bin/Release/net9.0-android -name "*-Signed.apk" -exec ls -lh {} \; | tee -a "$LOG"


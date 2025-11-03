# 🔐 Guide de Signature d'Application - DonTroc

Ce guide vous explique comment signer votre application DonTroc pour la distribution sur les stores.

## 📱 Signature Android (Google Play Store)

### Étape 1 : Générer votre keystore
```bash
./generate_keystore.sh
```

Ce script va :
- Créer un keystore sécurisé dans `./keystore/dontroc-release.keystore`
- Générer un fichier de configuration `./keystore/signing.properties`
- Mettre à jour automatiquement votre `.gitignore`

### Étape 2 : Build de production
```bash
./build_release.sh
```

Ce script automatise :
- Nettoyage des builds précédents
- Restauration des packages
- Compilation en mode Release
- Signature automatique
- Génération du fichier AAB pour le Play Store

### Étape 3 : Upload sur Google Play
1. Connectez-vous à la [Google Play Console](https://play.google.com/console)
2. Créez votre application
3. Uploadez le fichier `.aab` généré
4. Configurez les métadonnées de votre app

## 🍎 Signature iOS (App Store)

Pour iOS, consultez le fichier `iOS_SIGNING_GUIDE.md` pour les instructions détaillées.

## 🛡️ Sécurité IMPORTANTE

### ⚠️ À FAIRE :
- ✅ Sauvegardez votre keystore en lieu sûr
- ✅ Notez vos mots de passe dans un gestionnaire sécurisé
- ✅ Gardez une copie de sauvegarde hors ligne
- ✅ Utilisez le même keystore pour toutes les mises à jour

### ❌ À NE JAMAIS FAIRE :
- ❌ Committer le keystore ou signing.properties sur Git
- ❌ Partager vos mots de passe par email/chat
- ❌ Perdre votre keystore (impossible de publier des mises à jour)
- ❌ Changer de keystore après publication

## 🔧 Configuration du projet

Votre fichier `DonTroc.csproj` a été configuré avec :

### Android Release :
- Signature automatique activée
- Format AAB (Android App Bundle)
- Optimisations de production (R8, Proguard)
- Trimming activé pour réduire la taille

### iOS Release :
- Configuration pour certificats Apple
- Support des bibliothèques Swift
- Optimisations LLVM

## 🚀 Commandes rapides

```bash
# Générer le keystore (une seule fois)
./generate_keystore.sh

# Build complet signé pour Android
./build_release.sh

# Build manuel Android
dotnet publish -c Release -f net8.0-android -p:AndroidPackageFormat=aab

# Build iOS (sur Mac uniquement)
dotnet build -c Release -f net8.0-ios
```

## 📊 Structure des fichiers

```
DonTroc/
├── generate_keystore.sh      # Génération du keystore
├── build_release.sh          # Build automatique
├── setup_signing.sh          # Configuration env
├── iOS_SIGNING_GUIDE.md      # Guide iOS
├── keystore/                 # Clés de signature (sécurisé)
│   ├── dontroc-release.keystore
│   └── signing.properties
└── DonTroc/
    ├── DonTroc.csproj        # Configuration signature
    └── bin/Release/          # Builds signés
```

## 🆘 En cas de problème

### Erreur "keystore not found"
```bash
# Vérifiez que le keystore existe
ls -la ./keystore/

# Régénérez si nécessaire
./generate_keystore.sh
```

### Erreur de mot de passe
```bash
# Vérifiez le fichier de configuration
cat ./keystore/signing.properties

# Rechargez les variables
source ./keystore/signing.properties
```

### Build qui échoue
```bash
# Nettoyez complètement
dotnet clean -c Release
rm -rf ./DonTroc/bin ./DonTroc/obj

# Recommencez
./build_release.sh
```

## 📞 Support

Si vous rencontrez des problèmes :
1. Vérifiez que Java JDK est installé (`java -version`)
2. Vérifiez que .NET 8 est installé (`dotnet --version`)
3. Consultez les logs d'erreur détaillés
4. Assurez-vous que tous les packages NuGet sont restaurés

---

**🎉 Votre application DonTroc est maintenant prête pour la signature et la distribution !**

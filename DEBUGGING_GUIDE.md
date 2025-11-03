# 🎓 GUIDE DE DEBUGGING - Exceptions MAUI

## Exceptions Courantes et Solutions

### **[XA8000] - Ressource Android manquante**
```
Xamarin.Android.Common.targets(1489, 3): [XA8000] Ressource Android 
'@styleable/XXX' introuvable.
```

**Diagnostic:**
- Vérifier quelle ressource manque
- Chercher le fichier attrs XML correspondant
- Vérifier la déclaration dans le manifeste

**Solutions:**
```bash
# 1. Créer le fichier attrs manquant
cat > Platforms/Android/Resources/values/missing_attrs.xml << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <declare-styleable name="MissingView">
        <attr name="android:background" />
    </declare-styleable>
</resources>
EOF

# 2. Ou désactiver la vérification stricte en Release:
# Dans .csproj:
<AndroidStrictResourceErrors Condition="'$(Configuration)' == 'Release'">false</AndroidStrictResourceErrors>
```

---

### **[XABBA7023] - DLL localisée manquante**
```
System.IO.DirectoryNotFoundException: Could not find a part of the path 
'/Users/.../.nuget/packages/.../ar/shrunk/Microsoft.Maui.Controls.resources.dll'
```

**Diagnostic:**
- Erreur de trimming trop agressif
- DLLs satellites supprimées accidentellement
- Problème de cache NuGet

**Solutions:**
```bash
# 1. Nettoyer le cache NuGet
rm -rf ~/.nuget/packages/microsoft.maui.controls*
dotnet nuget locals all --clear

# 2. Dans Directory.Build.targets:
<ItemGroup Condition="'$(Configuration)' == 'Release'">
    <AndroidPackagesToSkipResourceProcessing Include="Microsoft.Maui.Controls.Core" />
</ItemGroup>

# 3. Ou désactiver le trimming:
<PublishTrimmed>false</PublishTrimmed>
```

---

### **[XA4314] - Mot de passe de signature vide**
```
Xamarin.Android.Common.targets(2447, 2): [XA4314] 
`$(AndroidSigningKeyPass)` est vide.
```

**Diagnostic:**
- Les propriétés de signature ne sont pas transmises
- Fallback de configuration manquant

**Solutions:**
```xml
<!-- Patter double-fallback dans .csproj: -->
<PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
    <!-- Fallback 1: Variables d'environnement -->
    <AndroidSigningKeyPassword Condition="'$(AndroidSigningKeyPassword)' == ''">$(KeyPass)</AndroidSigningKeyPassword>
    <!-- Fallback 2: Valeur par défaut -->
    <KeyPass Condition="'$(KeyPass)' == ''">MyPassword</KeyPass>
</PropertyGroup>
```

---

### **[XA8044] - MacCatalyst PublishTrimmed**
```
Xamarin.Shared.Sdk.targets(303, 3): MacCatalyst projects must build 
with PublishTrimmed=true. Current value: false.
```

**Diagnostic:**
- Configuration MacCatalyst incorrecte
- Mélange Debug/Release

**Solutions:**
```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net8.0-maccatalyst'">
    <!-- Différencier Debug/Release -->
    <PublishTrimmed Condition="'$(Configuration)' != 'Debug'">true</PublishTrimmed>
    <PublishTrimmed Condition="'$(Configuration)' == 'Debug'">false</PublishTrimmed>
    <MtouchLink>None</MtouchLink>
    <TrimMode Condition="'$(Configuration)' != 'Debug'">partial</TrimMode>
</PropertyGroup>
```

---

### **Cannot resolve symbol 'App', 'AndroidApp'**
```
DonTroc\Platforms\Android\AdMobNativeService.cs:9 
Cannot resolve symbol 'App'
```

**Diagnostic:**
- Référence à `Microsoft.Maui.Controls.Application.Current` qui peut être null
- `Platform.CurrentActivity` pas disponible au moment de l'appel

**Solutions:**
```csharp
// Pattern de récupération sûre du contexte:
private static Android.Content.Context? GetAndroidContext()
{
    try
    {
        // 1. Essayer l'activity courante
        var activity = Platform.CurrentActivity;
        if (activity != null)
            return activity;

        // 2. Essayer le contexte application global
        var context = Android.App.Application.Context;
        if (context != null)
            return context;

        // 3. Fallback MainPage
        return Microsoft.Maui.Controls.Application.Current
            ?.MainPage?.Handler?.MauiContext?.Context;
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Erreur contexte: {ex.Message}");
        return null;
    }
}
```

---

## 🔧 Procédure Générale de Debugging

### 1️⃣ Collecte des Logs
```bash
# Logs détaillés
dotnet build -verbosity:diagnostic > build.log 2>&1

# Filtre les erreurs
grep -i "error\|exception\|failed" build.log | head -20
```

### 2️⃣ Nettoyage Progressif
```bash
# Étape 1: Nettoyage léger
rm -rf bin obj

# Étape 2: Nettoyage intermédiaire
dotnet clean
dotnet restore

# Étape 3: Nettoyage complet (destructeur!)
rm -rf ~/.nuget/packages/microsoft.maui*
rm -rf ~/.nuget/packages/xamarin*
dotnet nuget locals all --clear
dotnet restore
```

### 3️⃣ Vérification des Dépendances
```bash
# Voir les versions installées
dotnet list package --include-transitive | grep -E "Maui|Xamarin|SkiaSharp"

# Vérifier les conflits
dotnet build /p:TreatWarningsAsErrors=true
```

### 4️⃣ Tests Granulaires
```bash
# Juste la restauration
dotnet restore DonTroc.csproj

# Juste la compilation
dotnet build -c Debug -f net8.0-android --no-restore

# Juste le packaging
dotnet publish -c Release -f net8.0-android --no-build
```

---

## 🎯 Checklist de Production

Avant de publier une version Release:

- [ ] Aucune erreur XA***
- [ ] Aucune erreur XABBA***
- [ ] Signature vérifiée (clé et mot de passe)
- [ ] APK signé présent: `bin/Release/net8.0-android/*.apk`
- [ ] Taille APK raisonnable (< 150 MB)
- [ ] Pas de ressources manquantes au runtime
- [ ] Pas d'avertissements critiques
- [ ] Version incrementée (ApplicationVersion, ApplicationDisplayVersion)
- [ ] Fichier `google-services.json` à jour
- [ ] Firebase Crashlytics build tools présent

---

## 🆘 Quand Rien Ne Marche

### Option nucléaire (reset complet)
```bash
# ⚠️ ATTENTION: Ceci supprime TOUS les artefacts de build

# 1. Backup les fichiers importants
cp DonTroc/Platforms/Android/google-services.json /tmp/

# 2. Nettoyage complet
git clean -fdx  # Supprime fichiers non-tracés
git reset --hard  # Reset les modifications

# 3. Restauration sélective
cp /tmp/google-services.json DonTroc/Platforms/Android/

# 4. Reconstruit from scratch
dotnet restore
dotnet build -c Debug
```

### Contacter le Support Microsoft/MAUI
Si l'erreur persiste après:
1. Nettoyage complet
2. Dernière version MAUI (.NET SDK)
3. Outils de build à jour

Fournir:
- Output complet du build avec `-verbosity:diagnostic`
- Version MAUI/dotnet: `dotnet --version`
- Fichier `.csproj` complet
- Fichier `Directory.Build.targets`
- Log complet de l'erreur

---

## 📊 Matrice de Diagnostic

| Erreur | Cause Probable | Solution Prioritaire |
|--------|---|---|
| XA8000 | Ressource manquante | Créer attrs XML |
| XABBA7023 | Trimming agressif | Désactiver PublishTrimmed |
| XA4314 | Signature vide | Ajouter fallback variables |
| XA8044 | Config MacCatalyst | Différencier Debug/Release |
| "Cannot resolve symbol" | Import missing | Ajouter using ou vérifier accessible |
| ANE0001 | Exception runtime | Chercher dans logcat |

---

**Version:** 1.0  
**Mis à jour:** 3 novembre 2025  
**Compatible:** .NET 8 + MAUI 8.0.100


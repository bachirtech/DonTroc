# Guide : Comment ajouter un Administrateur dans Firebase Realtime Database

## 📋 Vue d'ensemble

Ce guide vous montre **3 méthodes** pour ajouter un administrateur dans votre application DonTroc.

---

## Méthode 1 : Via la Console Firebase (RECOMMANDÉ) ✅

### Étape par étape :

1. **Ouvrez la Console Firebase**
   - Allez sur : https://console.firebase.google.com
   - Sélectionnez votre projet **DonTroc**

2. **Accédez à Realtime Database**
   - Dans le menu de gauche, cliquez sur **"Realtime Database"**
   - Cliquez sur l'onglet **"Données"**

3. **Trouvez l'ID utilisateur à promouvoir en admin**
   - Naviguez vers : `users/` 
   - Cherchez l'utilisateur que vous voulez promouvoir admin
   - **Copiez son ID** (c'est la clé sous `users/`)
   
   Exemple : Si vous voyez `users/abc123def456/`, l'ID est `abc123def456`

4. **Créez le nœud `admins` (si pas déjà créé)**
   - À la racine de votre base de données (au même niveau que `users`, `Annonces`, etc.)
   - Cliquez sur le **+** à côté du nom de votre base
   - Entrez comme **nom** : `admins`
   - Laissez la **valeur** vide pour l'instant
   - Cliquez sur **"Ajouter"**

5. **Ajoutez l'administrateur**
   - Cliquez sur le **+** à côté de `admins`
   - **Nom** : `[L'ID de l'utilisateur copié à l'étape 3]`
   - **Valeur** : Laissez vide
   - Cliquez sur **"Ajouter"**

6. **Ajoutez les propriétés de l'admin**
   - Cliquez sur le **+** à côté de l'ID que vous venez d'ajouter
   - Ajoutez ces propriétés une par une :

   | Nom | Type | Valeur |
   |-----|------|---------|
   | `Id` | string | [Même ID que ci-dessus] |
   | `Email` | string | email@exemple.com |
   | `Name` | string | Nom de l'admin |
   | `IsAdmin` | boolean | `true` |
   | `FcmToken` | string | (laissez vide, sera rempli auto) |

### Résultat final dans Firebase :

```
📁 Realtime Database
  ├── 📁 users
  ├── 📁 Annonces
  ├── 📁 admins
  │   └── 📁 abc123def456  ← ID de l'utilisateur
  │       ├── Id: "abc123def456"
  │       ├── Email: "admin@dontroc.com"
  │       ├── Name: "Admin Principal"
  │       ├── IsAdmin: true
  │       └── FcmToken: ""
  └── ...
```

---

## Méthode 2 : Importer un JSON directement 🚀

### Étape par étape :

1. **Créez un fichier JSON** avec cette structure :

```json
{
  "admins": {
    "VOTRE_USER_ID_ICI": {
      "Id": "VOTRE_USER_ID_ICI",
      "Email": "admin@dontroc.com",
      "Name": "Admin Principal",
      "IsAdmin": true,
      "FcmToken": "",
      "DisplayName": "Admin Principal",
      "ProfilePictureUrl": ""
    }
  }
}
```

2. **Dans la Console Firebase**
   - Allez dans **Realtime Database** > **Données**
   - Cliquez sur les **3 points verticaux** (⋮) en haut à droite
   - Sélectionnez **"Importer JSON"**
   - Choisissez votre fichier JSON
   - ⚠️ **ATTENTION** : Sélectionnez **"Fusionner"** (pas "Remplacer") pour ne pas perdre vos données existantes

---

## Méthode 3 : Programmation via l'application (Pour développeurs) 💻

### Option A : Créer une page d'administration

Je peux créer pour vous une page d'administration dans l'app où vous pouvez promouvoir des utilisateurs en admins.

### Option B : Code temporaire à exécuter une fois

Ajoutez cette méthode temporairement dans votre `FirebaseService.cs` :

```csharp
/// <summary>
/// Méthode temporaire pour promouvoir un utilisateur en administrateur
/// À SUPPRIMER après utilisation !
/// </summary>
public async Task PromoteUserToAdmin(string userId)
{
    try
    {
        // Récupérer le profil utilisateur existant
        var userProfile = await GetUserProfileAsync(userId);
        
        if (userProfile == null)
        {
            Debug.WriteLine($"Utilisateur {userId} non trouvé");
            return;
        }

        // Ajouter l'utilisateur dans le nœud admins
        await _firebaseClient
            .Child("admins")
            .Child(userId)
            .PutAsync(userProfile);

        Debug.WriteLine($"✅ Utilisateur {userProfile.Name} promu administrateur avec succès !");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"❌ Erreur lors de la promotion : {ex.Message}");
    }
}
```

**Puis appelez-la depuis votre code (par exemple dans un ViewModel) :**

```csharp
await _firebaseService.PromoteUserToAdmin("ID_DE_L_UTILISATEUR");
```

---

## 🔍 Comment trouver l'ID d'un utilisateur ?

### Dans Firebase Console :
1. Allez dans **Realtime Database** > **Données**
2. Naviguez vers `users/`
3. L'ID est la clé sous laquelle se trouvent les données de l'utilisateur

### Dans l'application (logs) :
Ajoutez ce code temporaire dans votre `LoginViewModel` après une connexion réussie :

```csharp
var userId = _authService.GetUserId();
Debug.WriteLine($"🔑 ID UTILISATEUR : {userId}");
```

Puis consultez les logs de votre application.

---

## ✅ Vérifier qu'un admin a été ajouté correctement

### Dans Firebase Console :
1. Allez dans **Realtime Database** > **Données**
2. Vérifiez que le nœud `admins/[USER_ID]` existe
3. Vérifiez que la propriété `IsAdmin` est à `true`

### Dans l'application (test) :
Ajoutez ce code temporaire pour tester :

```csharp
var admins = await _firebaseService.GetData<Dictionary<string, UserProfile>>("admins");
if (admins != null && admins.Count > 0)
{
    Debug.WriteLine($"✅ {admins.Count} administrateur(s) configuré(s) :");
    foreach (var admin in admins.Values)
    {
        Debug.WriteLine($"  - {admin.Name} ({admin.Email})");
    }
}
else
{
    Debug.WriteLine("❌ Aucun administrateur configuré");
}
```

---

## 🎯 Exemple complet : Ajouter VOTRE compte comme admin

**Scénario** : Vous êtes connecté à l'app et voulez vous promouvoir admin.

### Étapes rapides :

1. **Récupérez votre ID utilisateur**
   - Connectez-vous à l'app
   - Regardez dans Firebase Console > Realtime Database > users/
   - Trouvez votre compte (cherchez par email)
   - Copiez l'ID (la clé)

2. **Ajoutez-vous comme admin dans Firebase Console**
   - Créez `admins/[VOTRE_ID]/`
   - Ajoutez les propriétés comme expliqué dans la Méthode 1

3. **Redémarrez l'application**

4. **Testez** : Demandez à quelqu'un de signaler une annonce
   - Vous devriez recevoir une notification 🚨

---

## 🔒 Sécurité : Règles Firebase

Pour sécuriser le nœud `admins`, ajoutez ces règles dans Firebase :

1. Allez dans **Realtime Database** > **Règles**
2. Modifiez les règles pour protéger le nœud `admins` :

```json
{
  "rules": {
    "users": {
      ".read": "auth != null",
      ".write": "auth != null"
    },
    "admins": {
      ".read": "root.child('admins').child(auth.uid).exists()",
      ".write": "root.child('admins').child(auth.uid).exists()"
    },
    "reports": {
      ".read": "root.child('admins').child(auth.uid).exists()",
      ".write": "auth != null"
    }
  }
}
```

**Explication** :
- `admins` : Seuls les admins peuvent lire/écrire dans ce nœud
- `reports` : Seuls les admins peuvent lire les signalements, mais tout le monde peut en créer

---

## 📞 Aide rapide

### Problème : "Je ne vois pas le nœud admins"
**Solution** : Créez-le manuellement comme expliqué dans la Méthode 1, étape 4

### Problème : "Je ne reçois pas de notifications"
**Solution** : 
1. Vérifiez que vous êtes bien dans le nœud `admins`
2. Vérifiez que vous avez configuré la clé serveur FCM dans `PushNotificationService.cs`
3. Testez sur un appareil physique (pas émulateur)

### Problème : "Je ne connais pas mon ID utilisateur"
**Solution** : Consultez la section "Comment trouver l'ID d'un utilisateur ?" ci-dessus

---

## 🎉 Récapitulatif

**Pour ajouter rapidement un admin** :
1. ✅ Trouvez l'ID utilisateur dans Firebase Console (`users/`)
2. ✅ Créez `admins/[USER_ID]/` dans Firebase Console
3. ✅ Ajoutez les propriétés : Id, Email, Name, IsAdmin (true)
4. ✅ Configurez la clé FCM dans `PushNotificationService.cs`
5. ✅ Testez en signalant une annonce

**Besoin d'aide ?** Consultez `NOTIFICATIONS_ADMIN_GUIDE.md` pour plus de détails sur le système de notifications.


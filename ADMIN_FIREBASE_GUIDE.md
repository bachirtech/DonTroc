# 📋 Guide : Ajouter un Administrateur dans Firebase

## 🎯 Objectif
Ce guide vous explique comment ajouter un administrateur dans Firebase Realtime Database pour recevoir les notifications de signalements.

---

## 📝 Étapes pour ajouter un administrateur

### 1️⃣ Accéder à Firebase Console
1. Allez sur [Firebase Console](https://console.firebase.google.com)
2. Sélectionnez votre projet **DonTroc**
3. Dans le menu de gauche, cliquez sur **Realtime Database**

### 2️⃣ Créer la structure des administrateurs

Dans la base de données, vous devez créer une structure comme ceci :

```
dontroc/
  └── admins/
      └── {userId}/
          ├── email: "admin@dontroc.com"
          ├── name: "Admin Principal"
          ├── displayName: "Admin Principal"
          ├── role: "admin"
          ├── fcmToken: "f1234567890..." (optionnel pour les notifications push)
          └── createdAt: "2024-01-15T10:30:00Z"
```

### 3️⃣ Ajouter manuellement un admin

#### Option A : Via l'interface Firebase (Recommandé)

1. Dans Realtime Database, cliquez sur le **+** à côté de `dontroc`
2. Créez un nœud `admins` si il n'existe pas
3. Cliquez sur le **+** à côté de `admins`
4. Créez un nœud avec l'**UID de l'utilisateur** (vous le trouvez dans Authentication > Users)
   - Par exemple : `kR7mYPz8wXhGN4vZ2Qp1TyFc3Ld9`
5. Ajoutez les propriétés suivantes :

| Clé | Type | Valeur exemple |
|-----|------|----------------|
| `email` | string | `admin@dontroc.com` |
| `name` | string | `Admin Principal` |
| `displayName` | string | `Admin Principal` |
| `role` | string | `admin` |
| `fcmToken` | string | (laissez vide pour l'instant) |
| `createdAt` | string | `2025-01-15T10:30:00Z` |

#### Option B : Via l'application (si vous avez un écran d'admin)

Si vous créez un écran de gestion des admins dans l'application, vous pouvez utiliser le code suivant :

```csharp
public async Task AddAdmin(string userId, string email, string name)
{
    var admin = new UserProfile
    {
        UserId = userId,
        Email = email,
        Name = name,
        DisplayName = name,
        Role = "admin",
        CreatedAt = DateTime.UtcNow
    };
    
    await _firebaseService.SaveData($"admins/{userId}", admin);
}
```

### 4️⃣ Vérifier l'administrateur

1. Dans Realtime Database, vérifiez que votre administrateur apparaît sous `dontroc/admins/`
2. Notez l'**UID** de l'administrateur (c'est la clé du nœud)

---

## 🔔 Comment être notifié des signalements ?

### 📱 Notifications Locales (Actuellement actif)

**C'est déjà fonctionnel !** Lorsqu'un utilisateur signale une annonce :
- ✅ Une notification locale s'affiche sur l'appareil de l'admin (si l'app est ouverte)
- ✅ Le signalement est enregistré dans Firebase sous `reports/`

### 🌐 Notifications Push (À configurer)

Pour recevoir des notifications même quand l'application est fermée, vous devez :

#### 1. Obtenir la clé serveur FCM

1. Allez dans [Firebase Console](https://console.firebase.google.com)
2. Sélectionnez votre projet **DonTroc**
3. Cliquez sur l'**icône engrenage** ⚙️ > **Paramètres du projet**
4. Allez dans l'onglet **Cloud Messaging**
5. Cherchez la section **Cloud Messaging API (Legacy)**
6. Si vous ne voyez pas de **Server Key** :
   - Cliquez sur **Activer Cloud Messaging API (Legacy)**
7. Copiez la **Server Key** (elle commence par `AAAA...` et fait environ 150 caractères)

#### 2. Configurer la clé dans l'application

1. Ouvrez le fichier : `/DonTroc/Services/PushNotificationService.cs`
2. Remplacez la ligne 31 :
   ```csharp
   private const string FCM_SERVER_KEY = ""; // Laissez vide pour l'instant
   ```
   par :
   ```csharp
   private const string FCM_SERVER_KEY = "AAAA...votre_clé_complète_ici";
   ```

#### 3. Obtenir votre token FCM (pour recevoir les notifications)

Quand vous vous connectez à l'application en tant qu'admin :
- L'application génère automatiquement un **FCM Token**
- Ce token est enregistré dans Firebase sous `admins/{votre_uid}/fcmToken`
- Vous n'avez rien à faire manuellement

#### 4. Test des notifications push

Une fois configuré :
1. Connectez-vous à l'application avec un compte admin
2. Demandez à quelqu'un de signaler une annonce
3. Vous devriez recevoir une notification push même si l'app est fermée

---

## 🔍 Vérifier les signalements dans Firebase

### Via Firebase Console

1. Allez dans **Realtime Database**
2. Naviguez vers `dontroc/reports/`
3. Vous verrez tous les signalements :

```
dontroc/
  └── reports/
      └── {reportId}/
          ├── id: "abc123..."
          ├── annonceId: "xyz789..."
          ├── reporterId: "user123..."
          ├── reason: "Contenu inapproprié"
          ├── description: "L'annonce contient..."
          ├── timestamp: "2025-01-15T14:30:00Z"
          └── status: "Pending"
```

### Via l'application (Page de modération)

Si vous avez accès à la page de modération dans l'application :
1. Connectez-vous avec un compte admin
2. Allez dans la section **Modération**
3. Vous verrez la liste des signalements

---

## ⚙️ Configuration des règles Firebase (Sécurité)

Pour que seuls les admins puissent accéder aux signalements, ajoutez ces règles dans **Realtime Database > Règles** :

```json
{
  "rules": {
    "dontroc": {
      "admins": {
        ".read": "auth != null",
        ".write": false
      },
      "reports": {
        ".read": "root.child('dontroc').child('admins').child(auth.uid).exists()",
        ".write": "auth != null",
        "$reportId": {
          ".validate": "newData.hasChildren(['id', 'annonceId', 'reporterId', 'reason', 'timestamp', 'status'])"
        }
      }
    }
  }
}
```

**Explication** :
- `.read` sur `reports` : Seuls les utilisateurs présents dans `admins/` peuvent lire
- `.write` sur `reports` : Tout utilisateur authentifié peut créer un signalement
- `.write` sur `admins` : Personne ne peut modifier les admins (à faire manuellement)

---

## 📊 Résumé du flux de notification

```
1. Utilisateur signale une annonce
   ↓
2. Signalement enregistré dans Firebase (reports/)
   ↓
3. Le ReportService récupère la liste des admins (admins/)
   ↓
4. Pour chaque admin :
   - Si fcmToken existe → Notification Push FCM
   - Notification locale affichée
   ↓
5. Admin reçoit la notification et peut modérer
```

---

## 🚨 Dépannage

### Problème : Je ne reçois pas de notifications

**Vérifications** :
1. ✅ Votre compte est bien dans `dontroc/admins/` ?
2. ✅ La clé FCM Server Key est configurée dans `PushNotificationService.cs` ?
3. ✅ L'application a les permissions de notifications activées ?
4. ✅ Votre `fcmToken` est bien enregistré dans Firebase ?

### Problème : Erreur "StaticResource not found"

**Solution** : ✅ Déjà corrigé ! J'ai modifié le fichier `AnnoncesView.xaml` pour utiliser des couleurs directes au lieu de références StaticResource dans les DataTemplate.

### Problème : Erreur "Cannot resolve symbol 'DisplayName'"

**Solution** : ✅ Déjà corrigé ! Le code utilise maintenant `DisplayName ?? Name` pour gérer les deux cas.

---

## 📞 Support

Si vous avez des questions ou des problèmes :
1. Vérifiez les logs dans l'application
2. Consultez la console Firebase pour voir les données
3. Testez avec un autre utilisateur pour valider le flux complet

Bonne gestion ! 🎉


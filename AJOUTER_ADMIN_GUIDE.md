# Guide : Comment ajouter un administrateur dans Firebase Realtime Database

## Vue d'ensemble
Ce guide vous explique comment ajouter un utilisateur en tant qu'administrateur pour recevoir les notifications de signalement dans l'application DonTroc.

---

## 📋 Prérequis
- Accès à la console Firebase : https://console.firebase.google.com
- Un compte utilisateur existant dans votre application
- L'ID utilisateur (userId) de la personne à promouvoir admin

---

## 🔧 Étapes pour ajouter un administrateur

### 1. Accéder à Firebase Console
1. Ouvrez https://console.firebase.google.com
2. Sélectionnez votre projet **DonTroc**
3. Dans le menu de gauche, cliquez sur **Realtime Database**

### 2. Trouver l'ID utilisateur
Avant d'ajouter un admin, vous devez connaître son ID :

1. Dans Realtime Database, naviguez vers : `users/`
2. Vous verrez une liste d'IDs d'utilisateurs (ressemblent à : `abc123xyz...`)
3. Cliquez sur un ID pour voir les détails :
   - `Name` : Nom de l'utilisateur
   - `Email` : Email de l'utilisateur
4. Notez l'ID de l'utilisateur que vous voulez promouvoir admin

### 3. Ajouter l'utilisateur dans le nœud "admins"

**Option A : Via l'interface Firebase (Recommandé)**

1. Dans Realtime Database, cliquez sur le nœud racine (root)
2. Cliquez sur le bouton **+** pour ajouter un enfant
3. Entrez le nom : `admins` (si ce nœud n'existe pas déjà)
4. Cliquez sur le bouton **+** à côté de `admins`
5. Entrez l'ID utilisateur que vous avez noté
6. Ajoutez les propriétés suivantes :
   
   ```
   admins/
     └─ [USER_ID]/
         ├─ Id: "[USER_ID]"
         ├─ Name: "Nom de l'admin"
         ├─ Email: "email@example.com"
         ├─ FcmToken: ""  (vide pour l'instant, sera rempli automatiquement)
   ```

**Option B : En copiant depuis "users"**

1. Naviguez vers `users/[USER_ID]`
2. Cliquez sur l'icône **⋮** (trois points) à droite
3. Sélectionnez **Export JSON**
4. Copiez le JSON
5. Naviguez vers `admins/`
6. Cliquez sur le bouton **+**
7. Collez l'ID utilisateur comme clé
8. Cliquez sur **+** pour ajouter les propriétés
9. Importez les données en cliquant sur **⋮** > **Import JSON**

**Option C : Via l'API REST Firebase**

Vous pouvez aussi utiliser curl ou Postman :

```bash
curl -X PUT \
  'https://[VOTRE-DATABASE].firebaseio.com/admins/[USER_ID].json?auth=[VOTRE-AUTH-TOKEN]' \
  -H 'Content-Type: application/json' \
  -d '{
    "Id": "[USER_ID]",
    "Name": "Nom de l'Admin",
    "Email": "admin@example.com",
    "FcmToken": ""
  }'
```

### 4. Vérifier l'ajout

1. Rafraîchissez la vue dans Realtime Database
2. Vous devriez voir :
   ```
   dontroc-database/
     ├─ users/
     │   └─ [USER_ID]/...
     ├─ admins/
     │   └─ [USER_ID]/
     │       ├─ Id: "[USER_ID]"
     │       ├─ Name: "..."
     │       ├─ Email: "..."
     │       └─ FcmToken: "..."
     ├─ annonces/
     └─ reports/
   ```

---

## 📱 Comment ça fonctionne ?

### Quand un utilisateur signale une annonce :

1. **Un nouveau document est créé** dans `reports/[REPORT_ID]`
2. **Le service ReportService** récupère tous les admins depuis `admins/`
3. **Pour chaque admin** :
   - Si l'admin a un `FcmToken` (notification push token), une notification push est envoyée
   - Une notification locale est affichée si l'admin est connecté sur cet appareil
4. **L'admin reçoit** :
   - 🚨 Titre : "Nouveau signalement"
   - 📝 Message : "[Nom utilisateur] a signalé une annonce : [raison]"
   - 🔗 Un lien pour ouvrir la page de modération

### Structure d'un signalement :

```json
{
  "reports": {
    "report123": {
      "Id": "report123",
      "AnnonceId": "annonce456",
      "ReporterId": "user789",
      "Reason": "Contenu inapproprié",
      "Status": "Pending",
      "Timestamp": "2025-10-13T10:30:00Z",
      "Notes": ""
    }
  }
}
```

---

## 🔔 Configuration des notifications push

Pour que les admins reçoivent des notifications push, vous devez configurer FCM (Firebase Cloud Messaging) :

### 1. Obtenir la clé serveur FCM

1. Dans Firebase Console, allez dans **Paramètres du projet** (icône ⚙️)
2. Onglet **Cloud Messaging**
3. Cherchez la section **Cloud Messaging API (Legacy)**
4. Si désactivé, cliquez sur **Activer Cloud Messaging API (Legacy)**
5. Copiez la **Server Key** (commence par `AAAA...`)

### 2. Configurer la clé dans le code

Ouvrez `/DonTroc/Services/PushNotificationService.cs` :

```csharp
private const string FCM_SERVER_KEY = "AAAAxxxxxxx:APA91bH..."; // Collez votre clé ici
```

### 3. Test de notification

Une fois configuré, vous pouvez tester avec un signalement :
1. Créez un compte utilisateur normal
2. Signalez une annonce
3. L'admin devrait recevoir la notification

---

## 🛡️ Sécurité : Règles Firebase

Pour protéger le nœud `admins`, ajoutez ces règles dans **Realtime Database > Rules** :

```json
{
  "rules": {
    "admins": {
      ".read": "auth != null",
      ".write": false,
      "$userId": {
        ".read": "auth != null && auth.uid == $userId"
      }
    },
    "reports": {
      ".read": "root.child('admins').child(auth.uid).exists()",
      ".write": "auth != null",
      "$reportId": {
        ".read": "auth != null && (auth.uid == data.child('ReporterId').val() || root.child('admins').child(auth.uid).exists())"
      }
    }
  }
}
```

Cela garantit que :
- ✅ Seuls les admins peuvent lire la liste des signalements
- ✅ Tout utilisateur authentifié peut créer un signalement
- ✅ Le nœud `admins` est protégé en écriture

---

## 🔄 Retirer un administrateur

Pour retirer les privilèges admin :

1. Dans Realtime Database, naviguez vers `admins/[USER_ID]`
2. Cliquez sur **⋮** (trois points)
3. Sélectionnez **Delete**
4. Confirmez la suppression

L'utilisateur reste dans `users/` mais ne recevra plus les notifications de signalement.

---

## 📊 Voir les signalements (pour les admins)

Les admins peuvent accéder à la page de modération dans l'app :

1. **Via notification** : Cliquez sur la notification de signalement
2. **Via le menu** : Navigation > Page de modération
3. **Manuellement** : Dans Firebase, allez à `reports/` pour voir tous les signalements

---

## ❓ Dépannage

### Les notifications ne sont pas reçues ?

1. **Vérifiez la clé FCM** :
   - La clé serveur est-elle configurée dans `PushNotificationService.cs` ?
   - La clé est-elle valide (commence par `AAAA`) ?

2. **Vérifiez le token FCM de l'admin** :
   - Dans `admins/[USER_ID]/FcmToken`, y a-t-il une valeur ?
   - Le token est généré quand l'admin se connecte

3. **Consultez les logs** :
   - Dans l'application, regardez les logs de debug
   - Recherchez "Notification" ou "FCM"

### L'admin n'apparaît pas dans la liste ?

1. Vérifiez que l'ID utilisateur est correct
2. Vérifiez la structure des données dans `admins/`
3. Vérifiez les règles de sécurité Firebase

---

## 📞 Besoin d'aide ?

- Consultez la documentation Firebase : https://firebase.google.com/docs/cloud-messaging
- Vérifiez les logs de l'application pour des erreurs spécifiques
- Testez avec l'outil de test FCM dans la console Firebase

---

## Résumé rapide

```bash
# Structure Firebase requise
admins/
  └─ [USER_ID]/
      ├─ Id: "user123"
      ├─ Name: "Admin Name"
      ├─ Email: "admin@email.com"
      └─ FcmToken: "..."

# Flux de notification
Utilisateur signale → ReportService.CreateReport()
                   → NotifyAdministrators()
                   → Pour chaque admin dans admins/
                   → Envoi notification push + notification locale
                   → Admin reçoit notification 🚨
```

Voilà ! Vous savez maintenant comment gérer les administrateurs dans votre application DonTroc. 🎉


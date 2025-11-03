# Guide de Configuration des Notifications pour Administrateurs

## Vue d'ensemble

Ce système permet aux administrateurs d'être notifiés lorsqu'un utilisateur signale une annonce.

## 📱 Types de notifications implémentées

1. **Notifications locales** : Affichées sur l'appareil si l'admin utilise l'application
2. **Notifications push FCM** : Envoyées via Firebase Cloud Messaging aux administrateurs, même s'ils n'utilisent pas l'app

## 🔧 Configuration requise

### 1. Configurer la liste des administrateurs dans Firebase

Ajoutez vos administrateurs dans votre base de données Firebase Realtime Database :

```json
{
  "admins": {
    "admin_user_id_1": {
      "Id": "admin_user_id_1",
      "Email": "admin@dontroc.com",
      "Name": "Admin Principal",
      "FcmToken": "fcm_token_de_l_admin",
      "IsAdmin": true
    },
    "admin_user_id_2": {
      "Id": "admin_user_id_2",
      "Email": "moderateur@dontroc.com",
      "Name": "Modérateur",
      "FcmToken": "fcm_token_du_moderateur",
      "IsAdmin": true
    }
  }
}
```

### 2. Obtenir la clé serveur Firebase pour FCM

1. Allez dans la **Console Firebase** : https://console.firebase.google.com
2. Sélectionnez votre projet DonTroc
3. Allez dans **Paramètres du projet** (icône engrenage) > **Cloud Messaging**
4. Copiez votre **Clé serveur** (Server key)
5. Remplacez `YOUR_FIREBASE_SERVER_KEY` dans le fichier `PushNotificationService.cs` :

```csharp
private const string FCM_SERVER_KEY = "VOTRE_CLE_SERVEUR_ICI";
```

### 3. Configurer les permissions Android

Les permissions sont déjà configurées, mais assurez-vous que votre `AndroidManifest.xml` contient :

```xml
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
```

### 4. Enregistrer le token FCM des administrateurs

Quand un administrateur se connecte, son token FCM est automatiquement enregistré dans son profil utilisateur. Pour marquer un utilisateur comme administrateur :

**Option 1 : Manuellement dans Firebase Console**
- Ajoutez l'utilisateur dans le nœud `admins` avec son ID

**Option 2 : Via code (à implémenter)**
```csharp
await _firebaseService.SaveData($"admins/{userId}", userProfile);
```

## 🚀 Comment ça fonctionne

### Flux de notification lors d'un signalement :

1. **Utilisateur signale une annonce** → `AnnoncesViewModel.ReportAnnonce()`
2. **Création du signalement** → `ReportService.CreateReport()`
3. **Notification automatique** → `ReportService.NotifyAdministrators()`
   - Récupère la liste des admins depuis Firebase
   - Envoie une notification push FCM à chaque admin
   - Affiche une notification locale

### Format de la notification :

**Titre** : 🚨 Nouveau signalement

**Message** : [Nom de l'utilisateur] a signalé une annonce : [Raison du signalement]

**Données** : 
- `type`: "report"
- `reportId`: ID du signalement pour navigation

## 📊 Tableau de bord de modération

Les administrateurs peuvent consulter tous les signalements via `ModerationViewModel` :

- Liste de tous les signalements
- Statut : Pending, Reviewed, ActionTaken
- Possibilité d'ajouter des notes
- Mise à jour du statut

## 🔔 Tester les notifications

### Test en développement :

1. **Ajouter un utilisateur de test comme admin** dans Firebase
2. **Lancer l'application** sur un appareil physique (les notifications ne fonctionnent pas sur émulateur)
3. **Se connecter** avec un compte utilisateur normal
4. **Signaler une annonce**
5. **Vérifier** :
   - Les logs de debug dans la console
   - La notification sur l'appareil de l'admin

### Logs à surveiller :

```csharp
// Dans ReportService.cs
Debug.WriteLine($"Erreur lors de la notification des administrateurs: {ex.Message}");
Debug.WriteLine($"Notification push préparée pour le token: {fcmToken}");
```

## 🛠️ Améliorations futures recommandées

1. **Dashboard web pour les admins** : Interface web pour gérer les signalements
2. **Notifications par email** : Envoyer aussi un email aux admins
3. **Statistiques de modération** : Nombre de signalements par jour/semaine
4. **Filtres avancés** : Filtrer les signalements par type, statut, date
5. **Actions automatiques** : Bloquer automatiquement après X signalements
6. **Escalade** : Notifier un super-admin si un signalement n'est pas traité dans X heures

## 🐛 Dépannage

### Les notifications ne s'affichent pas :

1. **Vérifier les permissions** : L'app a-t-elle la permission d'afficher des notifications ?
2. **Vérifier le token FCM** : Le token est-il bien enregistré dans Firebase ?
3. **Vérifier la clé serveur** : La clé serveur FCM est-elle correcte ?
4. **Vérifier les logs** : Y a-t-il des erreurs dans les logs ?
5. **Tester sur appareil physique** : Les notifications ne fonctionnent pas sur émulateur

### Erreur "StaticResource not found for key ActionButton" :

Cette erreur n'est pas liée aux notifications. Elle indique que des ressources XAML sont manquantes dans vos vues. Vérifiez vos fichiers de ressources dans :
- `Resources/Styles/Colors.xaml`
- `Resources/Styles/Styles.xaml`

### Pour résoudre l'erreur XAML :

Ajoutez dans votre fichier de ressources (ex: `App.xaml` ou `Styles.xaml`) :

```xml
<Style x:Key="ActionButton" TargetType="Button">
    <Setter Property="BackgroundColor" Value="{StaticResource Primary}" />
    <Setter Property="TextColor" Value="White" />
    <Setter Property="CornerRadius" Value="8" />
    <Setter Property="Padding" Value="16,12" />
</Style>
```

## 📝 Notes importantes

- **Sécurité** : Ne commitez jamais votre clé serveur FCM dans Git. Utilisez des variables d'environnement ou un service de configuration sécurisé.
- **Rate limiting** : Firebase FCM a des limites de taux. Pour de nombreux admins, considérez un système de batch.
- **Coût** : Les notifications push FCM sont gratuites jusqu'à un certain volume.

## 📞 Support

Pour toute question sur la configuration, consultez :
- Documentation Firebase : https://firebase.google.com/docs/cloud-messaging
- Documentation .NET MAUI : https://learn.microsoft.com/dotnet/maui/


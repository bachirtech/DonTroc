# Guide de dépannage : Réinitialisation de mot de passe Firebase

## ✅ Vérifications à faire dans la Console Firebase

### 1. Activer l'authentification par Email/Password

1. Aller sur [Firebase Console](https://console.firebase.google.com/)
2. Sélectionner le projet **dontroc-55570**
3. Menu latéral : **Authentication** → **Sign-in method**
4. Vérifier que **Email/Password** est **activé** (Enabled)

### 2. Configurer les templates d'emails

1. Dans **Authentication** → **Templates**
2. Vérifier le template **Password reset** (Réinitialisation du mot de passe)
3. Cliquer sur l'icône crayon pour modifier si nécessaire
4. S'assurer que :
   - L'expéditeur (From) est configuré
   - Le sujet est défini
   - Le contenu HTML contient `%LINK%` pour le lien de réinitialisation

### 3. Vérifier les Authorized domains

1. Dans **Authentication** → **Settings** → **Authorized domains**
2. Ajouter votre domaine si nécessaire (pour les deep links)

### 4. Vérifier les quotas d'envoi d'emails

1. Dans **Authentication** → **Settings** → **Email template**
2. Firebase a une limite de **100 emails/jour** sur le plan gratuit (Spark)
3. Si vous dépassez cette limite, les emails ne seront pas envoyés

## 🔍 Comment tester

### Sur l'application

1. Lancer l'application en mode Debug
2. Sur l'écran de connexion, entrer une adresse email existante
3. Cliquer sur "Mot de passe oublié"
4. Observer les logs dans la console de debug (Android Logcat)

### Logs attendus (succès)

```
🔐 [AuthService] SendPasswordResetEmailAsync - Email: test@example.com
📧 [AuthService] Email normalisé: test@example.com
📤 [AuthService] Appel Firebase SendPasswordResetEmailAsync...
✅ [AuthService] Email de réinitialisation envoyé avec succès pour : test@example.com
✅ [LoginViewModel] Réinitialisation réussie pour : test@example.com
```

### Logs en cas d'erreur

```
❌ [AuthService] FirebaseException: USER_NOT_FOUND
```
→ L'email n'existe pas dans Firebase

```
❌ [AuthService] FirebaseException: TOO_MANY_REQUESTS
```
→ Trop de tentatives, attendez quelques minutes

```
❌ [AuthService] Exception générale: ...
```
→ Problème de réseau ou configuration Firebase

## 🛠️ Solutions aux problèmes courants

### "L'email n'est jamais reçu"

1. **Vérifier le dossier spam** de l'utilisateur
2. **Vérifier que l'email existe** dans Firebase Auth
3. **Vérifier la limite quotidienne** (100 emails/jour plan gratuit)
4. **Tester avec un autre email** pour isoler le problème

### "Erreur USER_NOT_FOUND"

- L'adresse email n'est pas enregistrée dans Firebase
- Vérifier l'orthographe de l'email
- L'utilisateur doit d'abord créer un compte

### "Erreur OPERATION_NOT_ALLOWED"

- L'authentification Email/Password n'est pas activée
- Aller dans Firebase Console → Authentication → Sign-in method
- Activer "Email/Password"

### "Erreur TOO_MANY_REQUESTS"

- Firebase limite les requêtes pour éviter les abus
- Attendre quelques minutes avant de réessayer
- En production, implémenter un rate limiting côté client

## 📱 Configuration vérifiée

- **Project ID**: dontroc-55570
- **Package Name**: com.bachirdev.dontroc
- **Firebase URL**: https://dontroc-55570-default-rtdb.europe-west1.firebasedatabase.app

## 🔗 Liens utiles

- [Firebase Auth Documentation](https://firebase.google.com/docs/auth/android/manage-users#send_a_password_reset_email)
- [Plugin.Firebase Documentation](https://github.com/AdrianDiaz2000/Plugin.Firebase)
- [Firebase Console](https://console.firebase.google.com/project/dontroc-55570/authentication)

# Guide de déploiement des règles Firebase

## Problème actuel
L'erreur "Permission denied" indique que les règles de sécurité Firebase Realtime Database ne permettent pas les opérations demandées.

## Solution rapide

### Option 1: Via Firebase Console (Recommandé)

1. Allez sur [Firebase Console](https://console.firebase.google.com/)
2. Sélectionnez votre projet **dontroc-55570**
3. Dans le menu de gauche, cliquez sur **Realtime Database**
4. Cliquez sur l'onglet **Rules** (Règles)
5. Copiez-collez le contenu du fichier `firebase_rules.json` de ce projet
6. Cliquez sur **Publish** (Publier)

### Option 2: Via Firebase CLI

```bash
# Installer Firebase CLI si pas déjà fait
npm install -g firebase-tools

# Se connecter
firebase login

# Déployer les règles
firebase deploy --only database --project dontroc-55570
```

## Règles actuelles nécessaires

Les règles dans `firebase_rules.json` doivent être déployées. Voici les sections importantes:

### Messages
```json
"Messages": {
  ".indexOn": ["SenderId", "Timestamp"],
  ".read": "auth != null",
  ".write": "auth != null",
  "$conversationId": {
    ".indexOn": ["Timestamp"],
    "$messageId": {
      ".validate": "newData.hasChildren(['SenderId', 'Timestamp']) && newData.child('SenderId').val() == auth.uid"
    }
  }
}
```

### Conversations
```json
"Conversations": {
  ".indexOn": ["BuyerId", "SellerId", "LastMessageTimestamp", "AnnonceId"],
  ".read": "auth != null",
  "$conversationId": {
    ".write": "auth != null && (!data.exists() || data.child('ParticipantIds').child(auth.uid).val() === true || data.child('BuyerId').val() == auth.uid || data.child('SellerId').val() == auth.uid)"
  }
}
```

## Vérification

Après avoir déployé les règles, testez à nouveau la fonctionnalité de chat dans l'application.

## Diagnostic supplémentaire

Si le problème persiste après le déploiement des règles, vérifiez:
1. Que l'utilisateur est bien authentifié (le token JWT est valide)
2. Que la structure des données envoyées correspond aux règles de validation
3. Les logs de la console Firebase pour plus de détails sur les erreurs

## Debug dans la console

Vous pouvez utiliser le simulateur de règles dans Firebase Console:
1. Allez dans Realtime Database > Rules
2. Cliquez sur "Simulateur de règles" en haut
3. Testez différentes opérations (read/write) sur différents chemins


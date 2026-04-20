# 🔄 Système de notification de mise à jour — DonTroc

Ce document explique comment déclencher une notification de mise à jour auprès des utilisateurs de DonTroc.

Le système repose sur **deux mécanismes complémentaires** :

| # | Mécanisme | Déclenche | Effet utilisateur |
|---|-----------|-----------|-------------------|
| 1 | Firebase Realtime DB `/app_config` | Popup **in-app** au démarrage | « Mise à jour disponible » (soft) ou « Mise à jour obligatoire » (force) |
| 2 | Cloud Function `sendToTopic` sur topic `all_users` | **Notification push** dans la barre système | Ramène l'utilisateur dans l'app même fermée |

> ✅ **100% conforme Google Play** : l'app ne télécharge jamais d'APK, elle redirige uniquement vers le Play Store (intent `market://details?id=...`).

---

## 🧩 Mécanisme 1 — Popup in-app via Realtime DB

### Nœud Firebase à créer/modifier

Dans la **console Firebase → Realtime Database**, créer le nœud `/app_config/android` :

```json
{
  "app_config": {
    "android": {
      "latest_version_code": 29,
      "latest_version_name": "1.2",
      "min_required_version_code": 0,
      "update_message": "Nouveautés : stories, trocs structurés, carte améliorée !",
      "release_notes": "- 🤝 Nouveau système de propositions de troc\n- 🗺️ Carte optimisée\n- 🐛 Corrections de bugs"
    },
    "ios": {
      "latest_version_code": 29,
      "latest_version_name": "1.2",
      "min_required_version_code": 0,
      "update_message": "",
      "release_notes": ""
    }
  }
}
```

### Champs

| Champ | Rôle |
|-------|------|
| `latest_version_code` | Dernier **build** disponible (comme `ApplicationVersion` dans `DonTroc.csproj`). **Si l'utilisateur a un build inférieur → popup soft** (« Plus tard » possible) |
| `latest_version_name` | Version affichée (ex. "1.2") |
| `min_required_version_code` | En dessous → **popup bloquante** (force update). Utile pour bugs critiques / breaking changes serveur. Mettre `0` pour désactiver. |
| `update_message` | Message principal dans la popup |
| `release_notes` | Notes de version affichées sous le message |

### Règle de sécurité (déjà déployée)

```
"app_config": {
  ".read": "auth != null",
  ".write": "role == admin"
}
```

Seuls les admins peuvent modifier, tous les utilisateurs connectés lisent.

### Comportement côté app

- Vérif au démarrage (3s après init) dans `App.xaml.cs` → `AppUpdateService.CheckForUpdateAsync()`
- **Soft update** : popup avec « Mettre à jour » / « Plus tard ». Rappel **max 1×/24h** et pas 2× pour la même version refusée.
- **Force update** : popup bloquante en boucle jusqu'à ce que l'utilisateur ouvre le Play Store.

---

## 📣 Mécanisme 2 — Push FCM via topic `all_users`

Tous les utilisateurs Android sont auto-abonnés au topic `all_users` au démarrage (dans `App.xaml.cs → InitializePushNotificationsAsync`).

### Envoyer une notification globale

Endpoint Cloud Function déjà déployé :

```
POST https://europe-west1-dontroc-55570.cloudfunctions.net/sendToTopic
Headers:
  Authorization: Bearer <FIREBASE_ID_TOKEN_ADMIN>
  Content-Type: application/json

Body:
{
  "topic": "all_users",
  "title": "✨ DonTroc v1.2 est dispo !",
  "body": "Nouveau système de troc structuré, stories, et plus. Mettez à jour maintenant !",
  "data": {
    "type": "app_update",
    "action": "open_store"
  }
}
```

### Depuis l'admin panel (futur)
Un bouton peut être ajouté dans `AdminDashboardPage` pour déclencher ce push en un clic.

### Depuis cURL (immédiat, manuel après release)

```bash
# 1. Récupérer un ID token admin (via Firebase Auth REST ou depuis l'app admin)
TOKEN="<id_token_admin>"

# 2. Envoyer le push
curl -X POST https://europe-west1-dontroc-55570.cloudfunctions.net/sendToTopic \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "topic": "all_users",
    "title": "✨ DonTroc v1.2 est disponible !",
    "body": "Nouveau système de troc, stories et plus. Ouvrez l\u0027app pour mettre à jour.",
    "data": { "type": "app_update" }
  }'
```

---

## 🔁 Workflow recommandé à chaque release

1. **Build** l'AAB signé avec nouvelle version (`ApplicationVersion` incrémentée dans `DonTroc.csproj`).
2. **Upload** sur le Google Play Console.
3. **Attendre la validation** Google (quelques heures à 1 jour).
4. ✅ Une fois en ligne :
   - Mettre à jour `/app_config/android/latest_version_code` et `release_notes` dans Firebase.
   - *(Optionnel)* Envoyer le push `sendToTopic` pour réveiller les utilisateurs inactifs.
5. Les utilisateurs voient la popup **au prochain lancement** + notification push (s'ils ont activé les notifs).

---

## ⚙️ Cas d'urgence : mise à jour obligatoire

Si une faille critique est découverte :

1. Mettre `min_required_version_code` = nouveau build number dans `/app_config/android`.
2. → Tous les anciens utilisateurs auront une popup bloquante au prochain lancement.
3. Envoyer le push via `sendToTopic` pour accélérer.

---

## 🧪 Test

1. Modifier localement `ApplicationVersion` à **27** dans `DonTroc.csproj` (actuellement 28)
2. Créer `/app_config/android/latest_version_code: 28` dans Firebase
3. Build + installer → la popup soft doit apparaître 3s après le démarrage
4. Avec `min_required_version_code: 28` → la popup force doit apparaître en boucle

---

## 📌 Conformité Google Play

| Règle | Statut |
|-------|--------|
| Ne pas installer d'APK hors Play Store | ✅ (on redirige vers `market://`) |
| Ne pas forcer MAJ sans raison légitime | ✅ (force update réservé aux bugs critiques) |
| Respecter le consentement notifications | ✅ (topic requiert l'autorisation FCM) |
| Fournir un bouton « Mettre à jour » | ✅ |

100% aligné avec les [Google Play Developer Policies](https://play.google.com/about/developer-content-policy/) et la [guidance officielle sur les mises à jour](https://developer.android.com/guide/playcore/in-app-updates).


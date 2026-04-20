# 📊 Analyse de Rétention Utilisateur — DonTroc

> Document d'analyse des améliorations à implémenter pour maximiser la rétention des utilisateurs.
> Dernière mise à jour : 15 avril 2026

---

## 🔍 État actuel de l'app

### Ce qui existe déjà
- **Gamification complète** : XP, niveaux, badges (25+), streaks quotidiens, défis journaliers
- **Roue de la fortune** : 1 tour/jour, chance de gagner XP ou boost credits
- **Quiz quotidien** : Questions thématiques avec streak, rappel configurable
- **Système de favoris** : Listes personnalisées, alertes sur annonces
- **Chat intégré** : Messagerie en temps réel entre utilisateurs
- **Système social** : Parrainage, amis, activité des amis, partage
- **Notifications de proximité** : Nouvelles annonces dans un rayon configurable
- **Tips/Onboarding** : Conseils contextuel à la première utilisation de chaque feature
- **AdMob** : Bannières, interstitiels, rewarded ads (pubs récompensées)
- **Premium** : Suppression de pubs, boost credits

---

## 📋 Plan d'action par priorité

### ✅ 1. Page de détail d'annonce — TERMINÉ
> **Impact : ★★★★★ | Effort : ★★★☆☆**

- [x] `AnnonceDetailView.xaml` + `AnnonceDetailView.xaml.cs` créés
- [x] `AnnonceDetailViewModel.cs` avec chargement complet, favoris, chat, partage
- [x] Navigation depuis Dashboard et Annonces
- [x] Correction bug "Ambiguous routes" (routes dupliquées dans AppShell)
- [x] Navigation sécurisée avec `SafeGoBackAsync()` au lieu de `Shell.GoToAsync("..")`
- [x] Annonces similaires chargées en-place (pas de stacking de routes)

---

### ✅ 2. Notifications intelligentes de ré-engagement — TERMINÉ
> **Impact : ★★★★★ | Effort : ★★★☆☆**

Service : `RetentionNotificationService.cs` (singleton, timer 2h dans `App.xaml.cs`)

- [x] **Streak en danger** : Notifie si streak ≥ 2j et pas de claim aujourd'hui (après 18h locale)
- [x] **Badge proche** : Nudge quand un badge est à ≥ 75% de progression
- [x] **Alertes d'annonces** : Match des alertes utilisateur contre les annonces < 3h
- [x] **Favoris statut** : Détecte changement de statut (réservé, échangé, archivé, re-disponible)
- [x] **Système de cooldown** : 20h minimum entre 2 notifications du même type (via Preferences)
- [x] **Permission Android POST_NOTIFICATIONS** : Implémentation réelle pour API 33+

**Fichiers modifiés :**
- `Services/RetentionNotificationService.cs` (nouveau)
- `Services/NotificationService.cs` (permission Android)
- `App.xaml.cs` (timer périodique 2h, OnSleep/OnResume)
- `MauiProgram.cs` (enregistrement singleton)

---

### ✅ 3. Pop-up récompense quotidienne à l'ouverture — TERMINÉ
> **Impact : ★★★★★ | Effort : ★★☆☆☆**

**Problème résolu :** La récompense quotidienne était cachée dans l'onglet Récompenses. L'utilisateur devait naviguer manuellement pour la réclamer → beaucoup oubliaient → le streak se cassait.

**Solution implémentée :**
- [x] Pop-up modal overlay sur le Dashboard à l'ouverture (si récompense disponible)
- [x] Affiche la série (streak), l'icône de la récompense et le bouton "Récupérer"
- [x] Animation d'apparition en séquence (scale + fade, icône bounce, confettis au claim)
- [x] Montre les 7 jours de la semaine avec progression visuelle (vert=réclamé, surbrillance=aujourd'hui, grisé=verrouillé)
- [x] Si déjà réclamé → pas de pop-up (vérification via `CanClaimDailyRewardAsync`)
- [x] Se déclenche dans `DashboardView.OnAppearing()` (une seule fois par session)
- [x] Boutons "Récupérer" et "Plus tard"
- [x] Support thème clair/sombre

**Fichiers créés/modifiés :**
- `Views/DailyRewardPopup.xaml` (nouveau — ContentView overlay)
- `Views/DailyRewardPopup.xaml.cs` (nouveau — logique claim + animations)
- `Views/DashboardView.xaml` (ajout de l'overlay)
- `Views/DashboardView.xaml.cs` (injection GamificationService + AuthService, déclenchement popup)

---

### ✅ 4. Feed Dashboard personnalisé — TERMINÉ
> **Impact : ★★★★☆ | Effort : ★★★☆☆**

**Problème résolu :** Le Dashboard affichait juste les 5 annonces les plus récentes, sans personnalisation. Aucune raison de revenir le consulter régulièrement.

**Solution implémentée :**
- [x] Section "⭐ Recommandé pour vous" basée sur :
  - Catégories des annonces favorites (scoring pondéré)
  - Types d'annonces (don/échange) préférés
  - Récence, boost, popularité (nombre de vues)
- [x] Section "📍 Près de chez vous" (annonces triées par distance, rayon 30km)
  - Utilise le cache de géolocalisation (pas de popup GPS à chaque refresh)
  - Affiche le nom de la ville/zone détectée
  - Distance formatée sur chaque carte
- [x] Section "📊 Activité récente" (messages non lus, badges débloqués, favoris)
- [x] Widget streak 🔥 en haut (jours de série, niveau, titre, XP)
- [x] Badge "NEW" rouge sur les annonces < 24h (DateToIsNewConverter)
- [x] Indicateur boost 🚀 sur les annonces boostées
- [x] Pull-to-refresh avec invalidation du cache
- [x] Chargement parallèle pour la performance (Task.WhenAll)
- [x] Cache 5 minutes sur les recommandations

**Fichiers créés/modifiés :**
- `Services/RecommendationService.cs` (nouveau — scoring, cache, nearby)
- `Converters/DashboardConverters.cs` (nouveau — DateToIsNewConverter)
- `ViewModels/DashboardViewModel.cs` (refonte complète — 8 services injectés)
- `Views/DashboardView.xaml` (refonte complète — 3 carrousels, streak, activité)
- `MauiProgram.cs` (enregistrement RecommendationService singleton)
- `App.xaml` (enregistrement DateToIsNewConverter)

---

### ✅ 5. Système d'alertes sur annonces (UI complète) — TERMINÉ
> **Impact : ★★★★☆ | Effort : ★★★☆☆**

**Problème résolu :** Le modèle `AnnonceAlert` existait, `FavoritesService` avait les méthodes CRUD, et `RetentionNotificationService` vérifiait déjà les matchs… mais la suppression et le toggle ne fonctionnaient pas, le formulaire de création était minimal, et les cartes manquaient d'informations.

**Solution implémentée :**
- [x] Formulaire de création d'alerte enrichi :
  - Nom de l'alerte
  - Mots-clés (séparés par virgule)
  - Sélecteur de type (Don / Troc / Les deux) avec Picker
  - Chips de catégories sélectionnables (Vêtements, Meubles, Livres, Électronique, Maison, Jardin, Outils, Loisirs, Autre)
  - Localisation optionnelle
  - ScrollView pour le formulaire sur petits écrans
- [x] Cartes d'alertes enrichies :
  - Icône dynamique 🔔/🔕 selon l'état actif/inactif
  - Mots-clés affichés en chips avec 🔍
  - Badges type et localisation
  - Nombre de matchs affiché
  - Bouton toggle ✅/⏸️ pour activer/désactiver
  - Bouton supprimer 🗑️
- [x] Correction de `DeleteAlertAsync` (appelait réellement le service au lieu d'afficher "en cours de développement")
- [x] Correction de `DeleteListAsync` (même problème)
- [x] `ToggleAlertCommand` pour activer/désactiver une alerte
- [x] `ToggleCategoryCommand` pour les chips de catégories
- [x] Réinitialisation du formulaire à l'ouverture

**Fichiers modifiés :**
- `ViewModels/FavoritesViewModel.cs` (refonte — toggle, categories, types, corrections CRUD)
- `Views/FavoritesView.xaml` (cartes enrichies, formulaire complet, chips, toggle)
- `Views/FavoritesView.xaml.cs` (event handlers toggle et chips)

**Services existants utilisés (non modifiés) :**
- `FavoritesService.CreateAlertAsync()`, `DeleteAlertAsync()`, `ToggleAlertAsync()`, `GetUserAlertsAsync()`
- `RetentionNotificationService.CheckAlertMatchesAsync()` (détection automatique des matchs)

---

### ✅ 6. Leaderboard / Classement — TERMINÉ + OPTIMISÉ
> **Impact : ★★★☆☆ | Effort : ★★★☆☆**

**Problème initial :** L'utilisateur gagnait des XP et montait de niveau, mais ne se comparait à personne. Pas de compétition sociale = moins de motivation.

**Problème de performance détecté :** Le chargement était très lent (3-5s) car :
- Sync XP bloquante AVANT l'affichage (3 appels réseau séquentiels)
- Double chargement (constructeur + OnAppearing)
- Téléchargement de TOUS les profils Firebase à chaque ouverture
- Pas d'indicateur de chargement visible

**Solution implémentée + optimisations :**
- [x] Page classement accessible depuis Récompenses
- [x] Top 50 utilisateurs par XP (global + amis)
- [x] Position de l'utilisateur courant mise en évidence
- [x] Filtres : global / amis uniquement
- [x] Classement anonymisé (initiales, pas de nom complet)
- [x] Podium top 3 avec médailles 🥇🥈🥉
- [x] **Sync XP en arrière-plan** (fire-and-forget, ne bloque plus l'affichage)
- [x] **Chargement parallélisé** (profils Firebase + gamification locale en même temps)
- [x] **Cache séparé global/amis** (5 minutes, évite les re-téléchargements)
- [x] **SemaphoreSlim** pour éviter les syncs multiples simultanées
- [x] **Suppression du double chargement** (constructeur ne charge plus)
- [x] **ActivityIndicator plein écran** avec texte "Chargement du classement..."
- [x] **RewardsViewModel parallélisé** (boutons, défis, badges chargés en Task.WhenAll)

**Fichiers créés/modifiés :**
- `Services/LeaderboardService.cs` (nouveau — cache, sync background, parallélisé)
- `ViewModels/LeaderboardViewModel.cs` (nouveau — OnAppearingAsync, pas de double load)
- `Views/LeaderboardView.xaml` (nouveau — podium, liste, loading indicator)
- `Views/LeaderboardView.xaml.cs` (nouveau)
- `ViewModels/RewardsViewModel.cs` (optimisé — chargement parallèle Task.WhenAll)
- `MauiProgram.cs` (enregistrement singleton + transient)
- `AppShell.xaml.cs` (route enregistrée)

---

### ✅ 7. Amélioration de l'onboarding (premier lancement) — TERMINÉ
> **Impact : ★★★☆☆ | Effort : ★★☆☆☆**

**Problème résolu :** Le TipsService montrait des conseils par page, mais il n'y avait pas de vrai parcours d'onboarding guidé pour les nouveaux utilisateurs. Risque de churn J0.

**Solution implémentée :**
- [x] Écran de bienvenue animé (CarouselView 4 slides) après la première connexion
  - Slide 1 : "Bienvenue sur DonTroc" + explication du concept 🤝
  - Slide 2 : "Donnez ou échangez" + comment créer une annonce 📦
  - Slide 3 : "Gagnez des récompenses" + gamification 🏆
  - Slide 4 : "Restez informé" + bouton permission notifications 🔔
  - IndicatorView (dots) + boutons Passer/Suivant/C'est parti
  - Thème DonTroc cohérent (gradient terracotta, vert sauge)
- [x] Checklist "Premiers pas" visible sur le Dashboard pendant 7 jours :
  - ☐ Compléter le profil (photo + nom) → 👤 + navigation EditProfileView
  - ☐ Publier une première annonce → 📝 + navigation CreationAnnonceView
  - ☐ Ajouter un favori → ❤️ + navigation AnnoncesView
  - ☐ Envoyer un premier message → 💬 + navigation ConversationsView
  - ☐ Réclamer la première récompense quotidienne → 🎁 + navigation RewardsPage
  - Bonus +25 XP à la completion de chaque étape
  - Auto-détection des étapes complétées (profil, annonces, favoris, conversations, daily reward)
  - Barre de progression visuelle + compteur X/5
  - Bouton fermer (✕) pour masquer définitivement la checklist
  - Tap sur une étape → navigation vers la page correspondante
- [x] État stocké dans Preferences (`onboarding_completed`, `onboarding_first_signin_date`, `onboarding_step_X`, `onboarding_checklist_dismissed`)
- [x] Intégration dans le flux de connexion (LoginViewModel → OnboardingView si premier login)
- [x] Intégration dans l'auto-login (App.xaml.cs → OnboardingView si non complété)

**Fichiers créés/modifiés :**
- `Services/OnboardingService.cs` (nouveau — état checklist, auto-check, XP bonus)
- `ViewModels/OnboardingViewModel.cs` (nouveau — carousel 4 slides, navigation)
- `Views/OnboardingView.xaml` (nouveau — CarouselView, IndicatorView, boutons)
- `Views/OnboardingView.xaml.cs` (nouveau — code-behind)
- `ViewModels/DashboardViewModel.cs` (checklist, DismissChecklist, auto-check)
- `Views/DashboardView.xaml` (widget checklist avec ProgressBar, étapes, ✅/icône)
- `ViewModels/LoginViewModel.cs` (redirection vers OnboardingView si premier login)
- `App.xaml.cs` (check onboarding dans auto-login)
- `MauiProgram.cs` (enregistrement OnboardingService, OnboardingViewModel, OnboardingView)

---

### ✅ 8. Rappels push intelligents (hors app) — TERMINÉ
> **Impact : ★★★★☆ | Effort : ★★★★☆**

**Problème résolu :** Le `RetentionNotificationService` ne fonctionnait que quand l'app était ouverte (timer dans `App.xaml.cs`). Si l'utilisateur ne revenait pas pendant des jours, aucune notification ne partait.

**Solution implémentée :**
- [x] 4 Cloud Functions Firebase schedulées (cron) en TypeScript :
  - **reminderJ1** (toutes les 6h) : « X nouvelles annonces près de chez vous ! » — match GeoHash préfixe 4
  - **reminderJ3** (toutes les 12h) : « Votre série est en danger ! » — streak à risque
  - **reminderJ7** (1x/jour à 10h) : « Vos annonces ont été vues X fois » — résumé hebdo
  - **reminderJ14** (1x/jour à 14h) : « Nouvelles annonces dans vos catégories favorites » — match favoris
- [x] Tracking `LastActiveAt` (epoch ms) dans Firebase à chaque OnStart/OnResume
- [x] Système anti-spam : cooldown par type (20h / 48h / 6j / 12j) via nœud `RemindersSent/{uid}`
- [x] Vérifications d'éligibilité : FCM token valide, pas suspendu, préférences opt-in
- [x] Préférences utilisateur opt-in/opt-out par type dans la page Modifier le profil :
  - 📍 Nouvelles annonces dans ma zone (J1)
  - 🔥 Streak en danger (J3)
  - 📊 Résumé hebdomadaire (J7)
  - ❤️ Annonces correspondant aux favoris (J14)
- [x] Sauvegarde des préférences dans Firebase `UserProfiles/{uid}/NotificationPreferences`

**Fichiers créés/modifiés :**
- `functions/src/scheduled-reminders.ts` (nouveau — 4 Cloud Functions cron)
- `functions/src/index.ts` (export des nouvelles fonctions)
- `functions/tsconfig.json` (target ES2020)
- `DonTroc/Models/UserProfile.cs` (ajout `LastActiveAt`, `NotificationPreferences`)
- `DonTroc/Services/FirebaseService.cs` (ajout `UpdateLastActiveAsync`, `UpdateNotificationPreferencesAsync`)
- `DonTroc/App.xaml.cs` (appel `UpdateLastActiveAsync` dans OnStart/OnResume)
- `DonTroc/ViewModels/EditProfileViewModel.cs` (4 toggles + Load/SaveNotificationPreferences)
- `DonTroc/Views/EditProfileView.xaml` (section 🔔 Rappels push avec 4 Switch)

**Déploiement :** `cd functions && firebase deploy --only functions`

---

### ✅ 9. Système de missions/quêtes hebdomadaires — TERMINÉ
> **Impact : ★★★☆☆ | Effort : ★★★☆☆**

**Problème résolu :** Les défis quotidiens existaient (`Challenge` model, `DailyChallenges` dans RewardsViewModel) mais il n'y avait pas de quêtes à plus long terme pour créer un engagement sur la durée. L'utilisateur n'avait aucune raison de revenir régulièrement au-delà des défis du jour.

**Solution implémentée :**
- [x] Quêtes hebdomadaires avec progression (2 quêtes aléatoires/semaine) :
  - Troqueur de la Semaine (3 transactions) — 150 XP + 1 boost
  - Producteur (5 annonces) — 200 XP + 1 boost
  - Ambassadeur (20 messages) — 100 XP
  - Fidélité (7 jours streak) — 300 XP + 2 boosts
  - Évaluateur (3 évaluations) — 75 XP
- [x] Quête spéciale mensuelle avec badge exclusif (1 quête/mois) :
  - Maître du Troc (10 transactions) — 500 XP + 3 boosts + 🏅 Badge "Troqueur du Mois"
  - Éditeur Prolifique (15 annonces) — 400 XP + 2 boosts + 📰 Badge "Éditeur du Mois"
  - Ambassadeur Social (100 messages) — 350 XP + 2 boosts + 🌟 Badge "Star Sociale du Mois"
  - Grand Explorateur (50 annonces consultées) — 300 XP + 1 boost + 🗺️ Badge "Explorateur du Mois"
- [x] Récompenses d'XP plus élevées : hebdo 75-300 XP (vs 15-40 quotidien), mensuel 300-500 XP
- [x] Auto-génération quand aucun défi actif du type (reset auto via ExpiresAt)
- [x] Badge exclusif débloqué automatiquement à la complétion de la quête mensuelle
- [x] UI dans la page Récompenses :
  - Section "📅 Quêtes de la Semaine" (thème bleu) avec barre de progression, compteur, temps restant
  - Section "👑 Quête du Mois" (thème violet/doré premium) avec badge à débloquer, barre de progression, récompenses boost
- [x] Converter `PercentToProgressConverter` (0-100% → 0-1 pour ProgressBar)

**Fichiers créés/modifiés :**
- `Models/GamificationModels.cs` (ajout `Monthly` dans `ChallengeType`, `ExclusiveBadgeId` dans `Challenge`)
- `Configuration/GamificationConfig.cs` (ajout `MonthlyChallengeTemplates`, 4 badges exclusifs, XP reward mensuel)
- `Services/GamificationService.cs` (ajout `GenerateMonthlyChallengesAsync`, auto-génération hebdo/mensuel, badge exclusif auto-unlock)
- `ViewModels/RewardsViewModel.cs` (ajout `WeeklyChallenges`, `MonthlyChallenges`, résumés, badge info)
- `Views/RewardsPage.xaml` (2 nouvelles sections UI : hebdo bleu + mensuel violet/doré)
- `Converters/BadgeConverters.cs` (ajout `PercentToProgressConverter`)
- `App.xaml` (enregistrement du converter)

---

### ✅ 10. Notifications de messages non lus améliorées — TERMINÉ
> **Impact : ★★★☆☆ | Effort : ★☆☆☆☆**

**Problème résolu :** `UnreadMessageService` comptait les messages non lus et `GlobalNotificationService` notifiait en temps réel (< 30s), mais si l'utilisateur ne répondait pas, aucun rappel ne partait. Les messages restaient indéfiniment sans réponse.

**Solution implémentée :**
- [x] **Rappel doux (2h-24h)** : "💬 Nouveau message non lu" / "💬 X messages non lus" — notification générique encourageante
- [x] **Rappel urgent (24h+)** : "💬 X attend votre réponse depuis hier/X jours" — mention du nom de l'expéditeur pour obligation sociale
- [x] Agrégation intelligente : si plusieurs conversations non lues, message groupé ("X et Y autre(s)")
- [x] Priorité : urgent > doux (pas les 2 dans le même cycle)
- [x] Cooldowns séparés (`unread_urgent` et `unread_reminder`, 20h chacun)
- [x] Ne double-pas avec `GlobalNotificationService` (ignore les messages < 2h)
- [x] Badge numérique sur l'onglet Chat ✅ (déjà existant via `BadgeView` + `UnreadMessageService`)

**Fichiers modifiés :**
- `Services/RetentionNotificationService.cs` (ajout 5ème type `CheckUnreadMessagesAsync` + méthodes d'envoi urgent/doux)

---

## 📊 Matrice Impact/Effort

```
Impact ★★★★★ │  [1]✅ Détail     [3]✅ Pop-up    [2]✅ Notif
              │      annonce      reward          rétention
              │
Impact ★★★★☆ │  [4] Dashboard   [5] Alertes    [8] Push
              │      perso         UI             serveur
              │
Impact ★★★☆☆ │  [6] Leaderboard [7] Onboarding [9] Quêtes
              │                                    hebdo
              │
Impact ★★☆☆☆ │  [10] Msg
              │       rappels
              │
              └──────────────────────────────────────────
                Effort ★☆    Effort ★★    Effort ★★★   Effort ★★★★
```

## 🎯 Ordre de réalisation recommandé

| # | Feature | Impact | Effort | Statut |
|---|---------|--------|--------|--------|
| 1 | Page détail annonce | ★★★★★ | ★★★ | ✅ Fait |
| 2 | Notifications de rétention | ★★★★★ | ★★★ | ✅ Fait |
| 3 | Pop-up récompense quotidienne | ★★★★★ | ★★ | ✅ Fait |
| 4 | Feed Dashboard personnalisé | ★★★★ | ★★★ | ✅ Fait |
| **5** | **UI alertes annonces** | **★★★★** | **★★★** | **✅ Fait** |
| **6** | **Leaderboard** | **★★★** | **★★★** | **✅ Fait + Optimisé** |
| 7 | Onboarding amélioré | ★★★ | ★★ | ✅ Fait |
| 8 | Push serveur (Cloud Functions) | ★★★★ | ★★★★ | ✅ Fait |
| 9 | Quêtes hebdomadaires | ★★★ | ★★★ | ✅ Fait |
| 10 | Rappels messages non lus | ★★★ | ★ | ✅ Fait |

---

### ✅ 11. Deep Links Notifications Push — TERMINÉ
> **Impact : ★★★☆☆ | Effort : ★★☆☆☆**

**Problème résolu :** Les notifications push (locales et FCM serveur) créaient des intents Android avec des extras (`conversationId`, `openQuiz`, `click_action`, etc.) mais aucun code ne traitait ces extras au lancement. L'utilisateur cliquait sur une notification et arrivait toujours sur le Dashboard, jamais sur la page concernée.

**Solution implémentée :**
- [x] `OnNewIntent()` override dans `MainActivity.cs` pour les intents reçus app ouverte
- [x] `HandleNotificationIntent()` qui traite les extras :
  - `conversationId` → ChatView
  - `openQuiz` → QuizPage
  - `openModeration` → ModerationPage
  - `notificationType` (streak_danger, achievement, level_up, challenge) → RewardsPage
  - `notificationType` (proximity_annonce) → AnnonceDetailView
  - `click_action` (OPEN_CONVERSATION, OPEN_ANNONCES, OPEN_REWARDS, OPEN_DASHBOARD, OPEN_ADMIN_REPORTS, OPEN_TRANSACTION, OPEN_ANNONCE) → page correspondante
- [x] Route `SocialView` ajoutée dans `AppShell.xaml.cs`

**Fichiers modifiés :**
- `Platforms/Android/MainActivity.cs` (ajout OnNewIntent + HandleNotificationIntent)
- `AppShell.xaml.cs` (ajout route SocialView)

---

## 📁 Architecture des services existants (référence)

```
Services/
├── AuthService.cs                    # Authentification Firebase
├── FirebaseService.cs                # CRUD annonces, users
├── GamificationService.cs           # XP, badges, streaks, roue, défis
├── FavoritesService.cs              # Favoris, listes, alertes
├── NotificationService.cs           # Notifications locales (canaux Android)
├── SmartNotificationService.cs      # Suggestions personnalisées
├── RetentionNotificationService.cs  # ✅ Notifications de rétention (4 types)
├── ProximityNotificationService.cs  # Notif de proximité géographique
├── GlobalNotificationService.cs     # Notifications messages en temps réel
├── PushNotificationService.cs       # FCM push
├── SocialService.cs                 # Parrainage, amis, partage
├── TipsService.cs                   # Conseils première utilisation
├── AdMobService.cs                  # Publicités
├── AppRatingService.cs              # Demande de notation store
├── UnreadMessageService.cs          # Messages non lus
└── FileLoggerService.cs             # Logs fichier
```


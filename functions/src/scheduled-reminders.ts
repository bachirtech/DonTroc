/**
 * Cloud Functions schedulées pour les rappels push de rétention DonTroc
 *
 * 4 fonctions cron qui envoient des notifications FCM aux utilisateurs inactifs :
 *   - reminderJ1 (toutes les 6h)  : nouvelles annonces dans la zone
 *   - reminderJ3 (toutes les 12h) : streak en danger
 *   - reminderJ7 (1x/jour)        : résumé hebdomadaire des vues
 *   - reminderJ14 (1x/jour)       : annonces correspondant aux favoris
 *
 * Chaque fonction vérifie :
 *   1. Le FCM token existe
 *   2. L'utilisateur n'est pas suspendu
 *   3. Les préférences de notification autorisent ce type
 *   4. Le dernier rappel de ce type date de > X heures (anti-spam)
 */

import * as admin from "firebase-admin";
import { onSchedule } from "firebase-functions/v2/scheduler";
import { logger } from "firebase-functions/v2";

// ============================================================
// Types
// ============================================================

interface UserProfile {
  Id?: string;
  Name?: string;
  FcmToken?: string;
  IsSuspended?: boolean;
  GeoHash?: string;
  LastLatitude?: number;
  LastLongitude?: number;
  LastActiveAt?: number; // epoch ms
  NotificationPreferences?: Record<string, boolean>;
}

interface Annonce {
  Id?: string;
  Titre?: string;
  Categorie?: string;
  DateCreation?: string;
  GeoHash?: string;
  UtilisateurId?: string;
  NombreVues?: number;
}

interface FavoriteItem {
  AnnonceId?: string;
  AnnonceCategory?: string;
  UserId?: string;
}

// ============================================================
// Constantes
// ============================================================

const REGION = "europe-west1";
const CHANNEL_RETENTION = "dontroc_gamification";

const HOURS_MS = 60 * 60 * 1000;
const DAY_MS = 24 * HOURS_MS;

// Cooldowns entre rappels du même type (en ms)
const COOLDOWNS: Record<string, number> = {
  reminder_j1: 20 * HOURS_MS,   // 20h entre deux rappels J1
  reminder_j3: 48 * HOURS_MS,   // 48h entre deux rappels J3
  reminder_j7: 6 * DAY_MS,      // 6 jours entre deux rappels J7
  reminder_j14: 12 * DAY_MS,    // 12 jours entre deux rappels J14
};

// ============================================================
// Helpers
// ============================================================

const db = () => admin.database();

async function sendFcm(
  fcmToken: string,
  title: string,
  body: string,
  data?: Record<string, string>
): Promise<boolean> {
  try {
    await admin.messaging().send({
      token: fcmToken,
      notification: { title, body },
      android: {
        priority: "high",
        notification: {
          sound: "default",
          icon: "ic_notification",
          channelId: CHANNEL_RETENTION,
        },
      },
      apns: {
        payload: { aps: { sound: "default", badge: 1 } },
      },
      data: data || undefined,
    });
    return true;
  } catch (error: unknown) {
    const code = (error as { code?: string }).code;
    if (code === "messaging/registration-token-not-registered" ||
        code === "messaging/invalid-argument") {
      logger.warn(`[Retention] Token FCM invalide: ${fcmToken.substring(0, 10)}...`);
    } else {
      logger.error("[Retention] Erreur FCM", { error });
    }
    return false;
  }
}

/**
 * Vérifie si un utilisateur est éligible pour un rappel
 */
function isEligible(
  user: UserProfile,
  reminderType: string,
  now: number
): boolean {
  // Pas de token FCM → impossible d'envoyer
  if (!user.FcmToken) return false;

  // Utilisateur suspendu → pas de rappel
  if (user.IsSuspended) return false;

  // Préférences de notification : opt-out explicite ?
  if (user.NotificationPreferences?.[reminderType] === false) return false;

  return true;
}

/**
 * Vérifie le cooldown et met à jour le timestamp du dernier rappel
 */
async function checkAndSetCooldown(
  userId: string,
  reminderType: string,
  now: number
): Promise<boolean> {
  const ref = db().ref(`RemindersSent/${userId}/${reminderType}`);
  const snap = await ref.get();
  const lastSent = snap.val() as number | null;

  if (lastSent && (now - lastSent) < (COOLDOWNS[reminderType] || 20 * HOURS_MS)) {
    return false; // Cooldown pas écoulé
  }

  await ref.set(now);
  return true;
}

/**
 * Récupère tous les profils utilisateurs
 */
async function getAllUsers(): Promise<Record<string, UserProfile>> {
  const snap = await db().ref("UserProfiles").get();
  return snap.val() || {};
}

/**
 * Récupère les annonces récentes (dernières N heures)
 */
async function getRecentAnnonces(hoursAgo: number): Promise<Annonce[]> {
  const cutoff = new Date(Date.now() - hoursAgo * HOURS_MS).toISOString();
  const snap = await db().ref("Annonces").get();
  const data = snap.val() || {};

  return Object.entries(data)
    .map(([key, val]) => ({ ...(val as Annonce), Id: key }))
    .filter((a) => a.DateCreation && a.DateCreation >= cutoff);
}

// ============================================================
// RAPPEL J1 : Nouvelles annonces dans votre zone (toutes les 6h)
// Cible : utilisateurs inactifs depuis 20-28h
// ============================================================

export const reminderJ1 = onSchedule(
  {
    schedule: "every 6 hours",
    region: REGION,
    timeZone: "Europe/Paris",
    retryCount: 1,
  },
  async () => {
    const now = Date.now();
    logger.info("[Retention J1] Démarrage vérification nouvelles annonces zone");

    const users = await getAllUsers();
    const recentAnnonces = await getRecentAnnonces(24);

    if (recentAnnonces.length === 0) {
      logger.info("[Retention J1] Aucune annonce récente, arrêt");
      return;
    }

    let sent = 0;

    for (const [userId, user] of Object.entries(users)) {
      // Inactif depuis 20h-48h
      if (!user.LastActiveAt) continue;
      const inactiveMs = now - user.LastActiveAt;
      if (inactiveMs < 20 * HOURS_MS || inactiveMs > 48 * HOURS_MS) continue;

      if (!isEligible(user, "reminder_j1", now)) continue;

      // Compter les annonces dans la zone (GeoHash préfixe commun = même zone ~39km)
      const userGeoPrefix = user.GeoHash?.substring(0, 4);
      if (!userGeoPrefix) continue;

      const nearbyCount = recentAnnonces.filter(
        (a) => a.GeoHash?.startsWith(userGeoPrefix) && a.UtilisateurId !== userId
      ).length;

      if (nearbyCount === 0) continue;

      // Vérifier le cooldown
      if (!(await checkAndSetCooldown(userId, "reminder_j1", now))) continue;

      const success = await sendFcm(
        user.FcmToken!,
        "📍 Nouvelles annonces près de chez vous !",
        `${nearbyCount} nouvelle${nearbyCount > 1 ? "s" : ""} annonce${nearbyCount > 1 ? "s" : ""} dans votre zone. Venez découvrir !`,
        { type: "retention_j1", click_action: "OPEN_ANNONCES" }
      );

      if (success) sent++;
    }

    logger.info(`[Retention J1] Terminé : ${sent} notification(s) envoyée(s)`);
  }
);

// ============================================================
// RAPPEL J3 : Votre streak va se casser ! (toutes les 12h)
// Cible : utilisateurs inactifs depuis 60h-96h
// ============================================================

export const reminderJ3 = onSchedule(
  {
    schedule: "every 12 hours",
    region: REGION,
    timeZone: "Europe/Paris",
    retryCount: 1,
  },
  async () => {
    const now = Date.now();
    logger.info("[Retention J3] Démarrage vérification streak en danger");

    const users = await getAllUsers();
    let sent = 0;

    for (const [userId, user] of Object.entries(users)) {
      // Inactif depuis 60h-96h (environ 2.5 à 4 jours)
      if (!user.LastActiveAt) continue;
      const inactiveMs = now - user.LastActiveAt;
      if (inactiveMs < 60 * HOURS_MS || inactiveMs > 96 * HOURS_MS) continue;

      if (!isEligible(user, "reminder_j3", now)) continue;
      if (!(await checkAndSetCooldown(userId, "reminder_j3", now))) continue;

      const success = await sendFcm(
        user.FcmToken!,
        "🔥 Votre série est en danger !",
        "Revenez vite réclamer votre récompense quotidienne avant de perdre votre progression !",
        { type: "retention_j3", click_action: "OPEN_REWARDS" }
      );

      if (success) sent++;
    }

    logger.info(`[Retention J3] Terminé : ${sent} notification(s) envoyée(s)`);
  }
);

// ============================================================
// RAPPEL J7 : Résumé hebdomadaire (1x/jour à 10h)
// Cible : utilisateurs inactifs depuis 6-10 jours
// ============================================================

export const reminderJ7 = onSchedule(
  {
    schedule: "0 10 * * *", // Tous les jours à 10h
    region: REGION,
    timeZone: "Europe/Paris",
    retryCount: 1,
  },
  async () => {
    const now = Date.now();
    logger.info("[Retention J7] Démarrage résumé hebdomadaire");

    const users = await getAllUsers();

    // Charger toutes les annonces pour compter les vues
    const snap = await db().ref("Annonces").get();
    const allAnnonces = snap.val() || {};

    let sent = 0;

    for (const [userId, user] of Object.entries(users)) {
      // Inactif depuis 6-10 jours
      if (!user.LastActiveAt) continue;
      const inactiveMs = now - user.LastActiveAt;
      if (inactiveMs < 6 * DAY_MS || inactiveMs > 10 * DAY_MS) continue;

      if (!isEligible(user, "reminder_j7", now)) continue;

      // Compter les vues totales des annonces de cet utilisateur
      let totalVues = 0;
      let annonceCount = 0;
      for (const [, annonce] of Object.entries(allAnnonces)) {
        const a = annonce as Annonce;
        if (a.UtilisateurId === userId) {
          totalVues += a.NombreVues || 0;
          annonceCount++;
        }
      }

      // Pas d'annonces → message différent
      if (annonceCount === 0) {
        if (!(await checkAndSetCooldown(userId, "reminder_j7", now))) continue;

        const success = await sendFcm(
          user.FcmToken!,
          "👋 Vous nous manquez !",
          "De nouvelles annonces vous attendent. Revenez découvrir ce qui est disponible près de chez vous !",
          { type: "retention_j7", click_action: "OPEN_DASHBOARD" }
        );
        if (success) sent++;
      } else {
        if (!(await checkAndSetCooldown(userId, "reminder_j7", now))) continue;

        const success = await sendFcm(
          user.FcmToken!,
          "📊 Votre résumé de la semaine",
          `Vos ${annonceCount} annonce${annonceCount > 1 ? "s" : ""} ${annonceCount > 1 ? "ont" : "a"} été vue${totalVues > 1 ? "s" : ""} ${totalVues} fois ! Revenez voir les nouveautés.`,
          { type: "retention_j7", click_action: "OPEN_DASHBOARD" }
        );
        if (success) sent++;
      }
    }

    logger.info(`[Retention J7] Terminé : ${sent} notification(s) envoyée(s)`);
  }
);

// ============================================================
// RAPPEL J14 : Annonces correspondant aux favoris (1x/jour à 14h)
// Cible : utilisateurs inactifs depuis 13-21 jours
// ============================================================

export const reminderJ14 = onSchedule(
  {
    schedule: "0 14 * * *", // Tous les jours à 14h
    region: REGION,
    timeZone: "Europe/Paris",
    retryCount: 1,
  },
  async () => {
    const now = Date.now();
    logger.info("[Retention J14] Démarrage vérification favoris/correspondances");

    const users = await getAllUsers();
    const recentAnnonces = await getRecentAnnonces(7 * 24); // Annonces des 7 derniers jours

    if (recentAnnonces.length === 0) {
      logger.info("[Retention J14] Aucune annonce récente, arrêt");
      return;
    }

    // Indexer les catégories des annonces récentes
    const categoriesRecentes = new Set(
      recentAnnonces.map((a) => a.Categorie?.toLowerCase()).filter(Boolean)
    );

    let sent = 0;

    for (const [userId, user] of Object.entries(users)) {
      // Inactif depuis 13-21 jours
      if (!user.LastActiveAt) continue;
      const inactiveMs = now - user.LastActiveAt;
      if (inactiveMs < 13 * DAY_MS || inactiveMs > 21 * DAY_MS) continue;

      if (!isEligible(user, "reminder_j14", now)) continue;

      // Charger les favoris de cet utilisateur (structure plate, filtrée par UserId)
      let matchCount = 0;
      try {
        const favSnap = await db().ref("favorites")
          .orderByChild("UserId")
          .equalTo(userId)
          .get();
        const favorites = favSnap.val() || {};

        // Extraire les catégories des favoris
        const userFavCategories = new Set<string>();
        for (const [, fav] of Object.entries(favorites)) {
          const f = fav as FavoriteItem;
          if (f.AnnonceCategory) userFavCategories.add(f.AnnonceCategory.toLowerCase());
        }

        // Compter les correspondances
        for (const cat of userFavCategories) {
          if (categoriesRecentes.has(cat)) matchCount++;
        }
      } catch {
        continue; // Pas de favoris
      }

      if (matchCount === 0) continue;

      if (!(await checkAndSetCooldown(userId, "reminder_j14", now))) continue;

      const success = await sendFcm(
        user.FcmToken!,
        "❤️ De nouvelles annonces pour vous !",
        `${matchCount} catégorie${matchCount > 1 ? "s" : ""} de vos favoris ${matchCount > 1 ? "ont" : "a"} de nouvelles annonces. Revenez les découvrir !`,
        { type: "retention_j14", click_action: "OPEN_ANNONCES" }
      );

      if (success) sent++;
    }

    logger.info(`[Retention J14] Terminé : ${sent} notification(s) envoyée(s)`);
  }
);


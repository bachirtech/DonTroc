/**
 * Firebase Cloud Functions pour DonTroc
 * 
 * SÉCURITÉ : Toute la logique d'envoi de notifications push est exécutée côté serveur.
 * La clé privée Firebase Admin SDK reste sur le serveur et n'est JAMAIS exposée au client.
 * 
 * L'authentification de l'appelant est vérifiée via le Firebase ID Token (verifyIdToken).
 */

import * as admin from "firebase-admin";
import { onRequest } from "firebase-functions/v2/https";
import { logger } from "firebase-functions/v2";

// Initialisation Firebase Admin (utilise les credentials du projet automatiquement)
admin.initializeApp();

// Exporter les Cloud Functions de rappels push de rétention (cron)
export { reminderJ1, reminderJ3, reminderJ7, reminderJ14 } from "./scheduled-reminders";

// ============================================================
// Interfaces pour les requêtes
// ============================================================

interface SendNotificationRequest {
  fcmToken: string;
  title: string;
  body: string;
  data?: Record<string, string>;
}

interface SendMessageNotificationRequest {
  fcmToken: string;
  senderName: string;
  messagePreview: string;
  conversationId: string;
}

interface SendReportNotificationRequest {
  fcmToken: string;
  reporterName: string;
  reason: string;
  reportId: string;
}

interface SendFavoriteNotificationRequest {
  fcmToken: string;
  userName: string;
  annonceTitre: string;
}

interface SendTransactionNotificationRequest {
  fcmToken: string;
  title: string;
  message: string;
  transactionId: string;
}

interface SendBroadcastRequest {
  fcmTokens: string[];
  title: string;
  body: string;
  data?: Record<string, string>;
}

interface SendToTopicRequest {
  topic: string;
  title: string;
  body: string;
  data?: Record<string, string>;
}

// ============================================================
// Helper : vérification de l'authentification
// ============================================================

async function verifyAuth(
  authHeader: string | undefined
): Promise<admin.auth.DecodedIdToken | null> {
  if (!authHeader || !authHeader.startsWith("Bearer ")) {
    return null;
  }
  const idToken = authHeader.split("Bearer ")[1];
  try {
    return await admin.auth().verifyIdToken(idToken);
  } catch (error) {
    logger.warn("Token d'authentification invalide", { error });
    return null;
  }
}

// ============================================================
// Helper : envoi d'une notification FCM unitaire
// ============================================================

// Canaux de notification Android correspondant aux canaux définis dans NotificationService.cs
const CHANNELS = {
  messages: "dontroc_messages",
  quiz: "dontroc_quiz",
  gamification: "dontroc_gamification",
  proximity: "dontroc_proximity",
  default: "dontroc_messages",
} as const;

async function sendFcmNotification(
  fcmToken: string,
  title: string,
  body: string,
  data?: Record<string, string>,
  channelId: string = CHANNELS.default
): Promise<boolean> {
  try {
    const message: admin.messaging.Message = {
      token: fcmToken,
      notification: { title, body },
      android: {
        priority: "high",
        notification: {
          sound: "default",
          icon: "ic_notification",
          channelId: channelId,
        },
      },
      apns: {
        payload: {
          aps: { sound: "default", badge: 1 },
        },
      },
      data: data || undefined,
    };

    await admin.messaging().send(message);
    return true;
  } catch (error: unknown) {
    const errorCode = (error as { code?: string }).code;
    if (
      errorCode === "messaging/registration-token-not-registered" ||
      errorCode === "messaging/invalid-argument"
    ) {
      logger.warn("Token FCM invalide ou expiré", { fcmToken: fcmToken.substring(0, 10) + "..." });
    } else {
      logger.error("Erreur FCM", { error });
    }
    return false;
  }
}

// ============================================================
// Cloud Function : sendNotification
// Envoi générique d'une notification push à un appareil
// ============================================================

export const sendNotification = onRequest(
  { region: "europe-west1", cors: true },
  async (req, res) => {
    // Vérifier la méthode HTTP
    if (req.method !== "POST") {
      res.status(405).json({ error: "Méthode non autorisée" });
      return;
    }

    // Vérifier l'authentification
    const decodedToken = await verifyAuth(req.headers.authorization);
    if (!decodedToken) {
      res.status(401).json({ error: "Non authentifié" });
      return;
    }

    const { fcmToken, title, body, data } = req.body as SendNotificationRequest;

    // Validation des paramètres
    if (!fcmToken || !title || !body) {
      res.status(400).json({ error: "Paramètres manquants: fcmToken, title, body requis" });
      return;
    }

    const success = await sendFcmNotification(fcmToken, title, body, data);
    res.status(success ? 200 : 500).json({ success });
  }
);

// ============================================================
// Cloud Function : sendMessageNotification
// Notification de nouveau message de chat
// ============================================================

export const sendMessageNotification = onRequest(
  { region: "europe-west1", cors: true },
  async (req, res) => {
    if (req.method !== "POST") {
      res.status(405).json({ error: "Méthode non autorisée" });
      return;
    }

    const decodedToken = await verifyAuth(req.headers.authorization);
    if (!decodedToken) {
      res.status(401).json({ error: "Non authentifié" });
      return;
    }

    const { fcmToken, senderName, messagePreview, conversationId } =
      req.body as SendMessageNotificationRequest;

    if (!fcmToken || !senderName || !conversationId) {
      res.status(400).json({ error: "Paramètres manquants" });
      return;
    }

    const preview =
      messagePreview && messagePreview.length > 100
        ? messagePreview.substring(0, 100) + "..."
        : messagePreview || "";

    const data = {
      type: "message",
      conversationId,
      click_action: "OPEN_CONVERSATION",
    };

    const success = await sendFcmNotification(
      fcmToken,
      `💬 Message de ${senderName}`,
      preview,
      data,
      CHANNELS.messages
    );

    res.status(success ? 200 : 500).json({ success });
  }
);

// ============================================================
// Cloud Function : sendReportNotification
// Notification de signalement aux administrateurs
// ============================================================

export const sendReportNotification = onRequest(
  { region: "europe-west1", cors: true },
  async (req, res) => {
    if (req.method !== "POST") {
      res.status(405).json({ error: "Méthode non autorisée" });
      return;
    }

    const decodedToken = await verifyAuth(req.headers.authorization);
    if (!decodedToken) {
      res.status(401).json({ error: "Non authentifié" });
      return;
    }

    const { fcmToken, reporterName, reason, reportId } =
      req.body as SendReportNotificationRequest;

    if (!fcmToken || !reporterName || !reason || !reportId) {
      res.status(400).json({ error: "Paramètres manquants" });
      return;
    }

    const data = {
      type: "report",
      reportId,
      click_action: "OPEN_ADMIN_REPORTS",
    };

    const success = await sendFcmNotification(
      fcmToken,
      "🚨 Nouveau signalement",
      `${reporterName} a signalé une annonce: ${reason}`,
      data,
      CHANNELS.messages
    );

    res.status(success ? 200 : 500).json({ success });
  }
);

// ============================================================
// Cloud Function : sendFavoriteNotification
// Notification quand une annonce est ajoutée aux favoris
// ============================================================

export const sendFavoriteNotification = onRequest(
  { region: "europe-west1", cors: true },
  async (req, res) => {
    if (req.method !== "POST") {
      res.status(405).json({ error: "Méthode non autorisée" });
      return;
    }

    const decodedToken = await verifyAuth(req.headers.authorization);
    if (!decodedToken) {
      res.status(401).json({ error: "Non authentifié" });
      return;
    }

    const { fcmToken, userName, annonceTitre } =
      req.body as SendFavoriteNotificationRequest;

    if (!fcmToken || !userName || !annonceTitre) {
      res.status(400).json({ error: "Paramètres manquants" });
      return;
    }

    const data = {
      type: "favorite",
      click_action: "OPEN_ANNONCE",
    };

    const success = await sendFcmNotification(
      fcmToken,
      "⭐ Nouvelle mise en favoris",
      `${userName} a ajouté '${annonceTitre}' à ses favoris`,
      data,
      CHANNELS.messages
    );

    res.status(success ? 200 : 500).json({ success });
  }
);

// ============================================================
// Cloud Function : sendTransactionNotification
// Notification de mise à jour de transaction
// ============================================================

export const sendTransactionNotification = onRequest(
  { region: "europe-west1", cors: true },
  async (req, res) => {
    if (req.method !== "POST") {
      res.status(405).json({ error: "Méthode non autorisée" });
      return;
    }

    const decodedToken = await verifyAuth(req.headers.authorization);
    if (!decodedToken) {
      res.status(401).json({ error: "Non authentifié" });
      return;
    }

    const { fcmToken, title, message, transactionId } =
      req.body as SendTransactionNotificationRequest;

    if (!fcmToken || !title || !message || !transactionId) {
      res.status(400).json({ error: "Paramètres manquants" });
      return;
    }

    const data = {
      type: "transaction",
      transactionId,
      click_action: "OPEN_TRANSACTION",
    };

    const success = await sendFcmNotification(fcmToken, title, message, data, CHANNELS.messages);
    res.status(success ? 200 : 500).json({ success });
  }
);

// ============================================================
// Cloud Function : sendBroadcast
// Envoi à plusieurs appareils
// ============================================================

export const sendBroadcast = onRequest(
  { region: "europe-west1", cors: true },
  async (req, res) => {
    if (req.method !== "POST") {
      res.status(405).json({ error: "Méthode non autorisée" });
      return;
    }

    const decodedToken = await verifyAuth(req.headers.authorization);
    if (!decodedToken) {
      res.status(401).json({ error: "Non authentifié" });
      return;
    }

    // Vérifier que l'utilisateur est admin (optionnel, pour sécuriser le broadcast)
    const userRecord = await admin.auth().getUser(decodedToken.uid);
    const isAdmin = userRecord.customClaims?.admin === true;

    const { fcmTokens, title, body, data } = req.body as SendBroadcastRequest;

    if (!fcmTokens || !Array.isArray(fcmTokens) || fcmTokens.length === 0 || !title || !body) {
      res.status(400).json({ error: "Paramètres manquants" });
      return;
    }

    // Limiter le nombre de tokens pour éviter les abus (sauf admin)
    const maxTokens = isAdmin ? 1000 : 50;
    const tokens = fcmTokens.slice(0, maxTokens).filter((t) => t && t.trim());

    let successCount = 0;
    const batchSize = 500; // FCM supporte 500 messages par batch

    for (let i = 0; i < tokens.length; i += batchSize) {
      const batch = tokens.slice(i, i + batchSize);
      const messages: admin.messaging.Message[] = batch.map((token) => ({
        token,
        notification: { title, body },
        android: {
          priority: "high" as const,
          notification: {
            sound: "default",
            icon: "ic_notification",
            channelId: CHANNELS.messages,
          },
        },
        apns: {
          payload: {
            aps: { sound: "default", badge: 1 },
          },
        },
        data: data || undefined,
      }));

      try {
        const response = await admin.messaging().sendEach(messages);
        successCount += response.successCount;
      } catch (error) {
        logger.error("Erreur batch FCM", { error });
      }
    }

    res.status(200).json({ success: true, successCount, totalTokens: tokens.length });
  }
);

// ============================================================
// Cloud Function : sendToTopic
// Envoi à un topic FCM
// ============================================================

export const sendToTopic = onRequest(
  { region: "europe-west1", cors: true },
  async (req, res) => {
    if (req.method !== "POST") {
      res.status(405).json({ error: "Méthode non autorisée" });
      return;
    }

    const decodedToken = await verifyAuth(req.headers.authorization);
    if (!decodedToken) {
      res.status(401).json({ error: "Non authentifié" });
      return;
    }

    const { topic, title, body, data } = req.body as SendToTopicRequest;

    if (!topic || !title || !body) {
      res.status(400).json({ error: "Paramètres manquants: topic, title, body requis" });
      return;
    }

    try {
      const message: admin.messaging.Message = {
        topic,
        notification: { title, body },
        android: { priority: "high" },
        data: data || undefined,
      };

      await admin.messaging().send(message);
      res.status(200).json({ success: true });
    } catch (error) {
      logger.error("Erreur envoi topic", { error, topic });
      res.status(500).json({ success: false, error: "Erreur d'envoi au topic" });
    }
  }
);


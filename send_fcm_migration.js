#!/usr/bin/env node
/**
 * Envoie une notification FCM au topic "all_users" pour annoncer
 * la migration hors Play Store et inviter à télécharger la nouvelle version.
 *
 * Usage : node send_fcm_migration.js [--dry-run]
 */
const admin = require('firebase-admin');
const path = require('path');

const SERVICE_ACCOUNT = path.join(
  __dirname,
  'DonTroc',
  'dontroc-55570-fbbe4cc1107f.json'
);

const DOWNLOAD_URL = 'https://bachirtech.github.io/DonTroc/download.html';

const message = {
  topic: 'all_users',
  notification: {
    title: '🆕 DonTroc 2.1 disponible',
    body:
      "Une mise à jour importante de DonTroc est disponible. " +
      "Téléchargez la nouvelle version (2.1) depuis notre site officiel.",
  },
  data: {
    action: 'open_url',
    url: DOWNLOAD_URL,
    type: 'app_update',
    version: '2.1',
    version_code: '31',
  },
  android: {
    priority: 'high',
    notification: {
      channelId: 'dontroc_default',
      clickAction: 'FLUTTER_NOTIFICATION_CLICK',
      icon: 'ic_notification',
    },
  },
  apns: {
    payload: {
      aps: {
        sound: 'default',
        badge: 1,
        'mutable-content': 1,
      },
    },
  },
};

(async () => {
  const dryRun = process.argv.includes('--dry-run');

  admin.initializeApp({
    credential: admin.credential.cert(require(SERVICE_ACCOUNT)),
  });

  console.log('Projet :', admin.app().options.credential.projectId || 'dontroc-55570');
  console.log('Topic  :', message.topic);
  console.log('Titre  :', message.notification.title);
  console.log('Corps  :', message.notification.body);
  console.log('URL    :', DOWNLOAD_URL);
  console.log('Dry-run:', dryRun);

  try {
    const id = await admin.messaging().send(message, dryRun);
    console.log('\n✅ FCM envoyé avec succès. messageId =', id);
  } catch (e) {
    console.error('\n❌ Échec envoi FCM :', e);
    process.exit(1);
  }
})();


using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DonTroc.Services
{
    /// <summary>
    /// Service pour envoyer des notifications push via Firebase Cloud Messaging.
    /// </summary>
    public class PushNotificationService
    {
        private readonly ILogger<PushNotificationService> _logger;
        private readonly HttpClient _httpClient;
        private const string FCM_API_URL = "https://fcm.googleapis.com/fcm/send";
        
        // TODO: Remplacer par votre clé serveur Firebase (Server Key, pas la clé VAPID ni la clé API)
        // IMPORTANT: La clé que vous avez fournie (AIzaSyCMAQf1wqkpI-G6cqtZS7esPa8juh71UJw) est une clé API Firebase, PAS une Server Key FCM
        // 
        // Pour obtenir la vraie Server Key FCM :
        // 1. Allez dans la console Firebase : https://console.firebase.google.com
        // 2. Sélectionnez votre projet "DonTroc"
        // 3. Allez dans Paramètres du projet (icône engrenage) > Cloud Messaging
        // 4. Cherchez "Server Key" dans la section "Cloud Messaging API (Legacy)"
        // 5. Si vous ne voyez pas de Server Key, vous devez activer l'API Cloud Messaging (Legacy)
        // 6. La clé commence généralement par "AAAA" et est très longue (environ 150+ caractères)
        //
        // Format attendu: AAAAxxxxxxx:APA91bHxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
        private const string FCM_SERVER_KEY = ""; // Laissez vide pour l'instant - les notifications seront désactivées

        public PushNotificationService(ILogger<PushNotificationService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            
            // N'ajouter l'en-tête d'autorisation que si la clé est configurée
            if (!string.IsNullOrWhiteSpace(FCM_SERVER_KEY))
            {
                try
                {
                    // Important : Pour l'API Legacy FCM, l'en-tête Authorization doit être au format "key=YOUR_SERVER_KEY"
                    // Mais on ne doit pas avoir "key=key=" dans la valeur
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"key={FCM_SERVER_KEY}");
                    _logger.LogInformation("✅ Client FCM configuré avec succès");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la configuration de la clé FCM. Vérifiez que vous utilisez la Server Key (pas la VAPID key).");
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Clé serveur FCM non configurée. Les notifications push ne seront pas envoyées. Consultez NOTIFICATIONS_ADMIN_GUIDE.md pour la configuration.");
            }
        }

        /// <summary>
        /// Envoie une notification push à un token FCM spécifique.
        /// </summary>
        public async Task<bool> SendNotificationAsync(string fcmToken, string title, string body, object? data = null)
        {
            // Vérifier si la clé serveur est configurée
            if (string.IsNullOrWhiteSpace(FCM_SERVER_KEY))
            {
                _logger.LogWarning("Les notifications push sont désactivées car la clé serveur FCM n'est pas configurée.");
                return false;
            }

            try
            {
                var payload = new
                {
                    to = fcmToken,
                    notification = new
                    {
                        title,
                        body,
                        sound = "default",
                        badge = "1"
                    },
                    data,
                    priority = "high"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(FCM_API_URL, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var tokenPreview = fcmToken.Length > 10 ? fcmToken.Substring(0, 10) + "..." : fcmToken;
                    _logger.LogInformation($"✅ Notification push envoyée avec succès au token: {tokenPreview}");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Erreur FCM ({response.StatusCode}): {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception lors de l'envoi de la notification push");
                return false;
            }
        }

        /// <summary>
        /// Envoie une notification de signalement à un administrateur.
        /// </summary>
        public async Task<bool> SendReportNotificationToAdminAsync(string fcmToken, string reporterName, string reason, string reportId)
        {
            var data = new
            {
                type = "report",
                reportId,
                click_action = "FLUTTER_NOTIFICATION_CLICK" // Pour Android
            };

            return await SendNotificationAsync(
                fcmToken,
                "🚨 Nouveau signalement",
                $"{reporterName} a signalé une annonce : {reason}",
                data
            );
        }

        /// <summary>
        /// Envoie une notification à plusieurs tokens (broadcast aux admins).
        /// </summary>
        public async Task<int> SendBroadcastNotificationAsync(string[] fcmTokens, string title, string body, object? data = null)
        {
            int successCount = 0;
            
            foreach (var token in fcmTokens)
            {
                if (await SendNotificationAsync(token, title, body, data))
                {
                    successCount++;
                }
            }

            _logger.LogInformation($"📊 Notifications envoyées: {successCount}/{fcmTokens.Length}");
            return successCount;
        }
    }
}

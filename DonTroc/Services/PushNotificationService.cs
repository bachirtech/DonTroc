using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DonTroc.Services
{
    /// <summary>
    /// Service pour envoyer des notifications push via Firebase Cloud Functions.
    /// 
    /// SÉCURITÉ: La clé privée Firebase Admin SDK est désormais côté serveur uniquement
    /// (dans les Cloud Functions). Le client s'authentifie avec son Firebase ID Token
    /// et délègue l'envoi au serveur.
    /// 
    /// Architecture:
    /// Client (MAUI) → Cloud Function (HTTPS + Auth) → FCM API V1 → Appareil destinataire
    /// </summary>
    public class PushNotificationService
    {
        private readonly ILogger<PushNotificationService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AuthService _authService;

        // URL de base des Cloud Functions (europe-west1)
        private const string FUNCTIONS_BASE_URL = "https://europe-west1-dontroc-55570.cloudfunctions.net";

        // Noms des Cloud Functions
        private const string FUNC_SEND_NOTIFICATION = "sendNotification";
        private const string FUNC_SEND_MESSAGE = "sendMessageNotification";
        private const string FUNC_SEND_REPORT = "sendReportNotification";
        private const string FUNC_SEND_FAVORITE = "sendFavoriteNotification";
        private const string FUNC_SEND_TRANSACTION = "sendTransactionNotification";
        private const string FUNC_SEND_BROADCAST = "sendBroadcast";
        private const string FUNC_SEND_TOPIC = "sendToTopic";

        // Configuration de retry
        private const int MAX_RETRIES = 3;
        private static readonly TimeSpan[] RetryDelays = 
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public PushNotificationService(
            ILogger<PushNotificationService> logger,
            IHttpClientFactory httpClientFactory,
            AuthService authService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _authService = authService;
        }

        /// <summary>
        /// Le service est toujours configuré (la config est côté serveur).
        /// L'authentification est vérifiée à chaque appel.
        /// </summary>
        public bool IsConfigured => true;

        /// <summary>
        /// Obtient le Firebase ID Token de l'utilisateur courant pour authentifier
        /// les appels aux Cloud Functions.
        /// </summary>
        private async Task<string?> GetAuthTokenAsync()
        {
            try
            {
                var token = await _authService.GetAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Impossible d'obtenir le token d'authentification Firebase");
                }
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du token d'authentification");
                return null;
            }
        }

        /// <summary>
        /// Envoie une requête POST authentifiée à une Cloud Function avec retry exponentiel.
        /// </summary>
        /// <typeparam name="TResponse">Type de la réponse attendue</typeparam>
        /// <param name="functionName">Nom de la Cloud Function</param>
        /// <param name="payload">Corps de la requête</param>
        /// <returns>La réponse désérialisée ou default en cas d'erreur</returns>
        private async Task<TResponse?> CallCloudFunctionAsync<TResponse>(string functionName, object payload) where TResponse : class
        {
            var authToken = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(authToken))
                return null;

            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var url = $"{FUNCTIONS_BASE_URL}/{functionName}";

            for (int attempt = 0; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    using var client = _httpClientFactory.CreateClient("PushNotification");
                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        return JsonSerializer.Deserialize<TResponse>(responseContent, JsonOptions);
                    }

                    // Erreurs non-retryables (4xx sauf 429)
                    var statusCode = (int)response.StatusCode;
                    if (statusCode >= 400 && statusCode < 500 && statusCode != 429)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Erreur Cloud Function {Function} ({StatusCode}): {Error}",
                            functionName, response.StatusCode, errorBody);
                        return null;
                    }

                    // Erreurs retryables (5xx, 429)
                    if (attempt < MAX_RETRIES)
                    {
                        var delay = RetryDelays[attempt];
                        // Ajouter du jitter (±25%)
                        var jitter = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * (Random.Shared.NextDouble() * 0.5 - 0.25));
                        _logger.LogWarning("Retry {Attempt}/{Max} pour {Function} après {Delay}ms",
                            attempt + 1, MAX_RETRIES, functionName, (delay + jitter).TotalMilliseconds);
                        await Task.Delay(delay + jitter);
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Échec après {Max} retries pour {Function}: {StatusCode} - {Error}",
                            MAX_RETRIES, functionName, response.StatusCode, errorBody);
                    }
                }
                catch (TaskCanceledException) when (attempt < MAX_RETRIES)
                {
                    // Timeout - retry
                    _logger.LogWarning("Timeout pour {Function}, retry {Attempt}/{Max}",
                        functionName, attempt + 1, MAX_RETRIES);
                    await Task.Delay(RetryDelays[attempt]);
                }
                catch (HttpRequestException ex) when (attempt < MAX_RETRIES)
                {
                    // Erreur réseau - retry
                    _logger.LogWarning(ex, "Erreur réseau pour {Function}, retry {Attempt}/{Max}",
                        functionName, attempt + 1, MAX_RETRIES);
                    await Task.Delay(RetryDelays[attempt]);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception lors de l'appel à {Function}", functionName);
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Réponse simple des Cloud Functions
        /// </summary>
        private class CloudFunctionResponse
        {
            public bool Success { get; set; }
        }

        /// <summary>
        /// Réponse du broadcast avec compteur
        /// </summary>
        private class BroadcastResponse
        {
            public bool Success { get; set; }
            public int SuccessCount { get; set; }
            public int TotalTokens { get; set; }
        }

        /// <summary>
        /// Envoie une notification push à un appareil via la Cloud Function.
        /// </summary>
        /// <param name="fcmToken">Token FCM unique de l'appareil destinataire</param>
        /// <param name="title">Titre de la notification</param>
        /// <param name="body">Corps du message</param>
        /// <param name="data">Données supplémentaires (optionnel)</param>
        /// <returns>True si envoyé avec succès, false sinon</returns>
        public async Task<bool> SendNotificationAsync(string fcmToken, string title, string body, object? data = null)
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
                return false;

            var payload = new
            {
                fcmToken,
                title,
                body,
                data = data != null ? ConvertToStringDictionary(data) : null
            };

            var response = await CallCloudFunctionAsync<CloudFunctionResponse>(FUNC_SEND_NOTIFICATION, payload);
            return response?.Success ?? false;
        }

        /// <summary>
        /// Convertit un objet en dictionnaire string/string
        /// </summary>
        private static Dictionary<string, string>? ConvertToStringDictionary(object obj)
        {
            try
            {
                if (obj is Dictionary<string, string> dict)
                    return dict;

                var json = JsonSerializer.Serialize(obj);
                var objDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                return objDict?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Envoie une notification de nouveau message de chat
        /// </summary>
        public async Task<bool> SendMessageNotificationAsync(string fcmToken, string senderName, string messagePreview, string conversationId)
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
                return false;

            var payload = new
            {
                fcmToken,
                senderName,
                messagePreview,
                conversationId
            };

            var response = await CallCloudFunctionAsync<CloudFunctionResponse>(FUNC_SEND_MESSAGE, payload);
            return response?.Success ?? false;
        }

        /// <summary>
        /// Envoie une notification de signalement aux administrateurs
        /// </summary>
        public async Task<bool> SendReportNotificationToAdminAsync(string fcmToken, string reporterName, string reason, string reportId)
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
                return false;

            var payload = new
            {
                fcmToken,
                reporterName,
                reason,
                reportId
            };

            var response = await CallCloudFunctionAsync<CloudFunctionResponse>(FUNC_SEND_REPORT, payload);
            return response?.Success ?? false;
        }

        /// <summary>
        /// Envoie une notification quand une annonce est ajoutée aux favoris
        /// </summary>
        public async Task<bool> SendFavoriteNotificationAsync(string fcmToken, string userName, string annonceTitre)
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
                return false;

            var payload = new
            {
                fcmToken,
                userName,
                annonceTitre
            };

            var response = await CallCloudFunctionAsync<CloudFunctionResponse>(FUNC_SEND_FAVORITE, payload);
            return response?.Success ?? false;
        }

        /// <summary>
        /// Envoie une notification de mise à jour de transaction
        /// </summary>
        public async Task<bool> SendTransactionNotificationAsync(string fcmToken, string title, string message, string transactionId)
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
                return false;

            var payload = new
            {
                fcmToken,
                title,
                message,
                transactionId
            };

            var response = await CallCloudFunctionAsync<CloudFunctionResponse>(FUNC_SEND_TRANSACTION, payload);
            return response?.Success ?? false;
        }

        /// <summary>
        /// Envoie une notification à plusieurs appareils (broadcast) via la Cloud Function.
        /// Le serveur gère le batching et le rate limiting.
        /// </summary>
        /// <param name="fcmTokens">Liste des tokens FCM destinataires</param>
        /// <param name="title">Titre de la notification</param>
        /// <param name="body">Corps du message</param>
        /// <param name="data">Données supplémentaires (optionnel)</param>
        /// <returns>Nombre de notifications envoyées avec succès</returns>
        public async Task<int> SendBroadcastNotificationAsync(string[] fcmTokens, string title, string body, object? data = null)
        {
            if (fcmTokens == null || fcmTokens.Length == 0)
                return 0;

            var payload = new
            {
                fcmTokens,
                title,
                body,
                data = data != null ? ConvertToStringDictionary(data) : null
            };

            var response = await CallCloudFunctionAsync<BroadcastResponse>(FUNC_SEND_BROADCAST, payload);
            return response?.SuccessCount ?? 0;
        }

        /// <summary>
        /// Envoie une notification à tous les abonnés d'un topic FCM
        /// </summary>
        /// <param name="topic">Nom du topic (ex: "news", "promotions")</param>
        /// <param name="title">Titre de la notification</param>
        /// <param name="body">Corps du message</param>
        /// <param name="data">Données supplémentaires (optionnel)</param>
        public async Task<bool> SendToTopicAsync(string topic, string title, string body, object? data = null)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return false;

            var payload = new
            {
                topic,
                title,
                body,
                data = data != null ? ConvertToStringDictionary(data) : null
            };

            var response = await CallCloudFunctionAsync<CloudFunctionResponse>(FUNC_SEND_TOPIC, payload);
            return response?.Success ?? false;
        }
    }
}

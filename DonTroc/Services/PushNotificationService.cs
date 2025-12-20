using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DonTroc.Services
{
    /// <summary>
    /// Service pour envoyer des notifications push via Firebase Cloud Messaging API V1.
    /// Utilise l'authentification OAuth2 avec un compte de service Firebase Admin SDK.
    /// </summary>
    public class PushNotificationService
    {
        private readonly ILogger<PushNotificationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly bool _isConfigured;
        private string? _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        
        // Configuration Firebase
        private const string PROJECT_ID = "dontroc-55570";
        private const string FCM_V1_URL = $"https://fcm.googleapis.com/v1/projects/{PROJECT_ID}/messages:send";
        private const string TOKEN_URL = "https://oauth2.googleapis.com/token";
        
        // Identifiants du compte de service Firebase Admin SDK
        private const string SERVICE_ACCOUNT_EMAIL = "firebase-adminsdk-fbsvc@dontroc-55570.iam.gserviceaccount.com";
        private const string PRIVATE_KEY = "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDe/AcAAAP/mUVO\nAqAjAftaAtKK+iSB9Xq1+UjRvCZSjG14mOc+nPDaeQTNGg9A1fTmt/GY933azmBf\nQEaID5WThF2LG94MJH8OrJ/2FDMDIM0jPXEKkVbyfeonvgDnxL63UpjsdV2YWYOh\numYYwH/LviATG4JNtiCU1JeQNzDLoJNFikl6XufwErHszPeqN8bGwOtkeswcWyX4\nT9WE6u2y5qQCZigGglcMr0+Xd0T6CLw9c2ImvqSOQuVDi+OR6f5LpbWz4AHH9mVm\n1zH9uWApmeebjEEhhkg1AcLMTvmjf4gC4omPCBxftL8jznXzfSs8ksAjx69PDpHI\nYwxabRyRAgMBAAECggEAAfWcU3g6yadrsbUEa+M4PMSU6ijVqfVamM2nbETVirVE\nhsXD3DZvuZn3Tg6HkOUQajDS76plFUd4FuG9YHslPlYM334sMlZzw18i9ZiAEOv4\niJxoYjgqu1DFDIiTcpJgIVHSYKJwuHEACatybKuzeuUvlPfYlPJ1F/KZZz3MBk4I\nKA7JgijxM2NSmrTUDpJIuCc13ogKPywFmY06BQAMnU7dtSOQOyGN15k8N5d0PSo2\nU9IQA60QI34tna5DDOaiPIuWYZo5z2U2viK2ISML59BCdlnVT/liX1ay2y7xnhke\nxlUSJ5u3pG1LhIxFT/dhOSkuFfVBTwUBClDEedhl7QKBgQD+8AfzROOTRj38HccD\nE9/6Y4eyznL9bCZDPsiKYrQzYtFn88jb7Y3G4XZaUyFMm82Dr9Kj/CoWM7OU5XsT\n1OAAWMg+fUmddGlySyx3ytI4RI0eVSjTveDd/ogUTnnfHSLDjapHAsPRRME2mNvy\nKToHQ9hj7Qks9Krqzqcq9PnjmwKBgQDf6eiS73196Eboki1getP9SgPqteEiESSW\nwF9UBa8SBdSSvP8tKmM2s3YWu6pbqu87/wHfOidh/9VnDUPOFeNOYOP3/kHlW+/F\ngwphjssIZTLkqchJqlzNarNiSEgHcNham1IGFhQfk3JlsTi2wZt35UkkcCJ1lim8\n6WvPPm/RQwKBgQCX4155O8PyzNjFSuB1HvRE8+O3TnUIM9UgH1nPyTrfmrJ0orQ7\nA++CXHXtHrYqNHFfUfPHq8dPbwJBZe/MQvoqerrjMDYZz2+7nrohrP9Octk5BzfJ\n38kHukxM/OxzV6KMq+yVXjpYhgQviScRwipGhc94yZK77BGgz/qdB1OSnQKBgGwQ\nSoNne8whJt+ldKrkfJz4dK14+99iIKN00k0NtTFgiPgMqKaWl21V7T8JcS1ucKkm\n6DNgsJMWUlq6xyeV3q78Cems1waneS98j60HqisyE/7Rhe0vgDxPK6XaNpEIwBHy\ndgKj8zBOC97SSgnBpJOXn9YrHCZdw2T9zl8lxfdbAoGAAZezRKUnsGjY/jwKV9uT\nKTLHghrX8k5h3LPlrDz+emo7Fbwtd9SR7onkA4/03/WhCbrOA5hBS/l945tafArt\n7aqBECkX/yjq9+QWWqxYkVYpOPTeslSz0bjneeJADX5oOBcjcun49z4LN471VPmk\nH077Fo1fR28PKgPkn5SmISU=\n-----END PRIVATE KEY-----\n";

        public PushNotificationService(ILogger<PushNotificationService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _isConfigured = !string.IsNullOrWhiteSpace(SERVICE_ACCOUNT_EMAIL) && !string.IsNullOrWhiteSpace(PRIVATE_KEY);
            
            if (!_isConfigured)
            {
                _logger.LogWarning("Notifications push désactivées - Compte de service non configuré");
            }
        }

        /// <summary>
        /// Vérifie si le service est configuré et prêt à envoyer des notifications
        /// </summary>
        public bool IsConfigured => _isConfigured;

        /// <summary>
        /// Obtient un token d'accès OAuth2 pour l'API FCM V1.
        /// 
        /// Processus d'authentification :
        /// 1. Crée un JWT (JSON Web Token) signé avec la clé privée du compte de service
        /// 2. Le JWT contient : émetteur (client_email), scope (firebase.messaging), audience (token URL)
        /// 3. Échange le JWT contre un access token OAuth2 auprès de Google
        /// 4. Le token est mis en cache et réutilisé jusqu'à expiration (1h moins 60s de marge)
        /// </summary>
        /// <returns>Token d'accès OAuth2 ou null en cas d'erreur</returns>
        private async Task<string?> GetAccessTokenAsync()
        {
            if (!_isConfigured) return null;
            
            // Réutiliser le token s'il est encore valide
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
            {
                return _accessToken;
            }

            try
            {
                // Créer le JWT avec les claims requis par Google OAuth2
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var expiry = now + 3600; // Validité de 1 heure

                var header = new { alg = "RS256", typ = "JWT" };
                var payload = new
                {
                    iss = SERVICE_ACCOUNT_EMAIL,  // Émetteur : email du compte de service
                    scope = "https://www.googleapis.com/auth/firebase.messaging",  // Scope FCM
                    aud = TOKEN_URL,  // Audience : endpoint d'échange de token
                    iat = now,        // Issued at : timestamp de création
                    exp = expiry      // Expiration : timestamp d'expiration
                };

                // Encoder header et payload en Base64URL puis signer
                var headerBase64 = Base64UrlEncode(JsonSerializer.Serialize(header));
                var payloadBase64 = Base64UrlEncode(JsonSerializer.Serialize(payload));
                var unsignedToken = $"{headerBase64}.{payloadBase64}";
                var signature = SignWithPrivateKey(unsignedToken, PRIVATE_KEY);
                var jwt = $"{unsignedToken}.{signature}";

                // Échanger le JWT contre un access token
                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                    new KeyValuePair<string, string>("assertion", jwt)
                });

                var response = await _httpClient.PostAsync(TOKEN_URL, tokenRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    _accessToken = doc.RootElement.GetProperty("access_token").GetString();
                    var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);
                    return _accessToken;
                }

                _logger.LogError("Erreur obtention token OAuth2: {Response}", responseContent);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception lors de l'obtention du token OAuth2");
                return null;
            }
        }

        /// <summary>
        /// Encode une chaîne en Base64URL (RFC 4648).
        /// Différent du Base64 standard : utilise - au lieu de +, _ au lieu de /, et pas de padding =
        /// </summary>
        private static string Base64UrlEncode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        /// <summary>
        /// Signe des données avec une clé privée RSA au format PKCS#8.
        /// 
        /// Étapes :
        /// 1. Nettoie la clé PEM (supprime headers/footers et retours à la ligne)
        /// 2. Décode la clé Base64 en bytes
        /// 3. Importe la clé dans un objet RSA
        /// 4. Signe les données avec SHA256 et padding PKCS1
        /// 5. Retourne la signature en Base64URL
        /// </summary>
        private static string SignWithPrivateKey(string data, string privateKeyPem)
        {
            var privateKey = privateKeyPem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\\n", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();

            var keyBytes = Convert.FromBase64String(privateKey);
            
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            return Convert.ToBase64String(signatureBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        /// <summary>
        /// Envoie une notification push à un appareil via FCM V1.
        /// </summary>
        /// <param name="fcmToken">Token FCM unique de l'appareil destinataire</param>
        /// <param name="title">Titre de la notification</param>
        /// <param name="body">Corps du message</param>
        /// <param name="data">Données supplémentaires (optionnel) - converties en dictionnaire string/string</param>
        /// <returns>True si envoyé avec succès, false sinon</returns>
        public async Task<bool> SendNotificationAsync(string fcmToken, string title, string body, object? data = null)
        {
            if (!_isConfigured || string.IsNullOrWhiteSpace(fcmToken))
                return false;

            try
            {
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                    return false;

                // Structure du message FCM V1 avec configurations Android et iOS
                var message = new
                {
                    message = new
                    {
                        token = fcmToken,
                        notification = new { title, body },
                        android = new
                        {
                            priority = "high",
                            notification = new
                            {
                                sound = "default",
                                icon = "ic_notification",
                                channel_id = "dontroc_channel"
                            }
                        },
                        apns = new
                        {
                            payload = new
                            {
                                aps = new { sound = "default", badge = 1 }
                            }
                        },
                        data = data != null ? ConvertToStringDictionary(data) : null
                    }
                };

                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, FCM_V1_URL);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                    return true;

                var responseBody = await response.Content.ReadAsStringAsync();
                if (responseBody.Contains("UNREGISTERED") || responseBody.Contains("INVALID_ARGUMENT"))
                {
                    _logger.LogWarning("Token FCM invalide ou expiré pour la notification: {Title}", title);
                }
                else
                {
                    _logger.LogError("Erreur FCM V1 ({StatusCode}): {Response}", response.StatusCode, responseBody);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception lors de l'envoi de la notification");
                return false;
            }
        }

        /// <summary>
        /// Convertit un objet anonyme en dictionnaire string/string (requis par FCM pour les données)
        /// </summary>
        private static Dictionary<string, string>? ConvertToStringDictionary(object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return dict?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "");
        }

        /// <summary>
        /// Envoie une notification de nouveau message de chat
        /// </summary>
        public async Task<bool> SendMessageNotificationAsync(string fcmToken, string senderName, string messagePreview, string conversationId)
        {
            var data = new { type = "message", conversationId, click_action = "OPEN_CONVERSATION" };
            var preview = messagePreview.Length > 100 ? messagePreview[..100] + "..." : messagePreview;
            return await SendNotificationAsync(fcmToken, $"💬 Message de {senderName}", preview, data);
        }

        /// <summary>
        /// Envoie une notification de signalement aux administrateurs
        /// </summary>
        public async Task<bool> SendReportNotificationToAdminAsync(string fcmToken, string reporterName, string reason, string reportId)
        {
            var data = new { type = "report", reportId, click_action = "OPEN_ADMIN_REPORTS" };
            return await SendNotificationAsync(fcmToken, "🚨 Nouveau signalement", $"{reporterName} a signalé une annonce: {reason}", data);
        }

        /// <summary>
        /// Envoie une notification quand une annonce est ajoutée aux favoris
        /// </summary>
        public async Task<bool> SendFavoriteNotificationAsync(string fcmToken, string userName, string annonceTitre)
        {
            var data = new { type = "favorite", click_action = "OPEN_ANNONCE" };
            return await SendNotificationAsync(fcmToken, "⭐ Nouvelle mise en favoris", $"{userName} a ajouté '{annonceTitre}' à ses favoris", data);
        }

        /// <summary>
        /// Envoie une notification de mise à jour de transaction
        /// </summary>
        public async Task<bool> SendTransactionNotificationAsync(string fcmToken, string title, string message, string transactionId)
        {
            var data = new { type = "transaction", transactionId, click_action = "OPEN_TRANSACTION" };
            return await SendNotificationAsync(fcmToken, title, message, data);
        }

        /// <summary>
        /// Envoie une notification à plusieurs appareils (broadcast)
        /// </summary>
        /// <param name="fcmTokens">Liste des tokens FCM destinataires</param>
        /// <param name="title">Titre de la notification</param>
        /// <param name="body">Corps du message</param>
        /// <param name="data">Données supplémentaires (optionnel)</param>
        /// <returns>Nombre de notifications envoyées avec succès</returns>
        public async Task<int> SendBroadcastNotificationAsync(string[] fcmTokens, string title, string body, object? data = null)
        {
            if (!_isConfigured || fcmTokens == null || fcmTokens.Length == 0)
                return 0;

            int successCount = 0;
            foreach (var token in fcmTokens)
            {
                if (!string.IsNullOrWhiteSpace(token) && await SendNotificationAsync(token, title, body, data))
                    successCount++;

                await Task.Delay(50); // Éviter le rate limiting
            }
            return successCount;
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
            if (!_isConfigured)
                return false;

            try
            {
                var accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                    return false;

                var message = new
                {
                    message = new
                    {
                        topic,
                        notification = new { title, body },
                        android = new { priority = "high" },
                        data = data != null ? ConvertToStringDictionary(data) : null
                    }
                };

                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, FCM_V1_URL);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception lors de l'envoi au topic {Topic}", topic);
                return false;
            }
        }
    }
}

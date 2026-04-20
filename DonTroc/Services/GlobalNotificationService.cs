using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;

namespace DonTroc.Services
{
    /// <summary>
    /// Service global pour gérer les notifications de messages dans toute l'application
    /// </summary>
    public class GlobalNotificationService : IDisposable
    {
        private readonly FirebaseService _firebaseService;
        private readonly AuthService _authService;
        private readonly NotificationService _notificationService;
        private readonly ILogger<GlobalNotificationService> _logger;
        
        private readonly Dictionary<string, IDisposable> _conversationSubscriptions = new();
        private readonly LinkedList<string> _processedMessageIds = new();
        private readonly HashSet<string> _processedMessageIdSet = new();
        private const int MAX_PROCESSED_IDS = 500;
        private bool _isInitialized = false;
        private string? _activeConversationId;

        public GlobalNotificationService(
            FirebaseService firebaseService, 
            AuthService authService, 
            NotificationService notificationService,
            ILogger<GlobalNotificationService> logger)
        {
            _firebaseService = firebaseService;
            _authService = authService;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Initialise le service de notifications globales
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                var currentUserId = _authService.GetUserId();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    _logger.LogWarning("Impossible d'initialiser les notifications : utilisateur non connecté");
                    return;
                }

                // Récupérer toutes les conversations de l'utilisateur
                var conversations = await _firebaseService.GetUserConversationsAsync(currentUserId);
                
                // S'abonner aux messages de chaque conversation
                foreach (var conversation in conversations)
                {
                    SubscribeToConversationMessages(conversation.Id);
                }

                _isInitialized = true;
                _logger.LogInformation("Service de notifications globales initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation du service de notifications globales");
            }
        }

        /// <summary>
        /// S'abonne aux messages d'une conversation spécifique
        /// </summary>
        private void SubscribeToConversationMessages(string conversationId)
        {
            if (_conversationSubscriptions.ContainsKey(conversationId))
                return;

            try
            {
                var subscription = _firebaseService.SubscribeToMessages(conversationId, async (message) =>
                {
                    await HandleNewMessage(message, conversationId);
                });

                _conversationSubscriptions[conversationId] = subscription;
                _logger.LogDebug($"Abonnement aux messages de la conversation {conversationId} créé");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'abonnement aux messages de la conversation {conversationId}");
            }
        }

        /// <summary>
        /// Traite un nouveau message et affiche une notification si nécessaire
        /// </summary>
        private async Task HandleNewMessage(Message message, string conversationId)
        {
            try
            {
                // Éviter de traiter le même message plusieurs fois (cache FIFO borné)
                if (_processedMessageIdSet.Contains(message.Id))
                    return;

                _processedMessageIdSet.Add(message.Id);
                _processedMessageIds.AddLast(message.Id);

                // Supprimer les plus anciens quand le cache est plein (FIFO)
                while (_processedMessageIds.Count > MAX_PROCESSED_IDS)
                {
                    var oldest = _processedMessageIds.First!.Value;
                    _processedMessageIds.RemoveFirst();
                    _processedMessageIdSet.Remove(oldest);
                }

                var currentUserId = _authService.GetUserId();
                
                // Ne pas notifier pour nos propres messages
                if (message.SenderId == currentUserId)
                    return;

                // Ne notifier que les messages récents (moins de 30 secondes)
                var messageAge = DateTime.UtcNow - message.Timestamp;
                if (messageAge.TotalSeconds > 30)
                {
                    _logger.LogDebug($"Message ignoré car trop ancien: {messageAge.TotalSeconds}s");
                    return;
                }

                // Vérifier si l'utilisateur est actuellement sur cette conversation
                if (IsUserOnConversation(conversationId))
                    return;

                // Récupérer les informations de l'expéditeur
                var senderProfile = await _firebaseService.GetUserProfileAsync(message.SenderId);
                var senderName = senderProfile?.Name ?? "Utilisateur inconnu";

                // Afficher la notification
                await _notificationService.ShowMessageNotificationAsync(
                    senderName, 
                    message.Text ?? "Nouveau message", 
                    conversationId
                );

                _logger.LogInformation($"Notification affichée pour le message de {senderName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement du nouveau message pour notification");
            }
        }

        /// <summary>
        /// Vérifie si l'utilisateur est actuellement sur une conversation donnée
        /// </summary>
        private bool IsUserOnConversation(string conversationId)
        {
            try
            {
                // Vérifier si cette conversation est marquée comme active
                if (_activeConversationId == conversationId)
                    return true;

                // Amélioration : vérifier si l'utilisateur est actuellement sur cette conversation
                // En récupérant l'état de navigation actuel
                if (Shell.Current?.CurrentState?.Location?.OriginalString?.Contains("ChatView") == true)
                {
                    // L'utilisateur est sur une page de chat
                    // Si on a l'ID de conversation active, on peut comparer
                    return _activeConversationId == conversationId;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Définit la conversation actuellement active (pour éviter les notifications)
        /// </summary>
        public void SetActiveConversation(string? conversationId)
        {
            _activeConversationId = conversationId;
        }

        /// <summary>
        /// Ajoute une nouvelle conversation au système de notifications
        /// </summary>
        public void AddConversation(string conversationId)
        {
            if (!_isInitialized)
                return;

            SubscribeToConversationMessages(conversationId);
        }

        /// <summary>
        /// Supprime une conversation du système de notifications
        /// </summary>
        public void RemoveConversation(string conversationId)
        {
            if (_conversationSubscriptions.TryGetValue(conversationId, out var subscription))
            {
                subscription.Dispose();
                _conversationSubscriptions.Remove(conversationId);
                _logger.LogDebug($"Abonnement aux messages de la conversation {conversationId} supprimé");
            }
        }

        /// <summary>
        /// Arrête temporairement les notifications (par exemple quand l'app passe en arrière-plan)
        /// </summary>
        public void PauseNotifications()
        {
            // Cette méthode peut être utilisée pour optimiser les performances
            // quand l'app n'est pas active
        }

        /// <summary>
        /// Reprend les notifications
        /// </summary>
        public void ResumeNotifications()
        {
            // Reprendre les notifications quand l'app redevient active
        }

        /// <summary>
        /// Nettoie les ressources
        /// </summary>
        public void Dispose()
        {
            foreach (var subscription in _conversationSubscriptions.Values)
            {
                subscription.Dispose();
            }
            _conversationSubscriptions.Clear();
            _processedMessageIds.Clear();
            _processedMessageIdSet.Clear();
            _isInitialized = false;
            
            _logger.LogInformation("Service de notifications globales nettoyé");
        }
    }
}

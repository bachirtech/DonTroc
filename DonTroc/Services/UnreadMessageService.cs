using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace DonTroc.Services
{
    /// <summary>
    /// Service pour gérer le comptage des messages non lus
    /// </summary>
    public class UnreadMessageService : INotifyPropertyChanged, IDisposable
    {
        private readonly FirebaseService _firebaseService;
        private readonly AuthService _authService;
        private readonly ILogger<UnreadMessageService> _logger;
        
        private readonly Dictionary<string, int> _unreadCounts = new();
        private readonly Dictionary<string, IDisposable> _conversationSubscriptions = new();
        private readonly Dictionary<string, DateTime> _lastReadTimes = new();
        
        private int _totalUnreadCount;
        private bool _isInitialized;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Nombre total de messages non lus dans toutes les conversations
        /// </summary>
        public int TotalUnreadCount
        {
            get => _totalUnreadCount;
            private set
            {
                if (_totalUnreadCount != value)
                {
                    _totalUnreadCount = value;
                    OnPropertyChanged(nameof(TotalUnreadCount));
                    OnPropertyChanged(nameof(HasUnreadMessages));
                }
            }
        }

        /// <summary>
        /// Indique s'il y a des messages non lus
        /// </summary>
        public bool HasUnreadMessages => TotalUnreadCount > 0;

        public UnreadMessageService(
            FirebaseService firebaseService,
            AuthService authService,
            ILogger<UnreadMessageService> logger)
        {
            _firebaseService = firebaseService;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Initialise le service de comptage des messages non lus
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
                    _logger.LogWarning("Impossible d'initialiser le comptage des messages non lus : utilisateur non connecté");
                    return;
                }

                // Charger les temps de dernière lecture depuis les préférences
                await LoadLastReadTimesAsync();

                // Récupérer toutes les conversations de l'utilisateur
                var conversations = await _firebaseService.GetUserConversationsAsync(currentUserId);

                // Initialiser le comptage pour chaque conversation
                foreach (var conversation in conversations)
                {
                    await InitializeConversationUnreadCount(conversation.Id);
                    SubscribeToConversationMessages(conversation.Id);
                }

                _isInitialized = true;
                UpdateTotalUnreadCount();
                
                _logger.LogInformation("Service de comptage des messages non lus initialisé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation du service de comptage des messages non lus");
            }
        }

        /// <summary>
        /// Initialise le comptage des messages non lus pour une conversation
        /// </summary>
        private async Task InitializeConversationUnreadCount(string conversationId)
        {
            try
            {
                var messages = await _firebaseService.GetMessagesAsync(conversationId);
                var currentUserId = _authService.GetUserId();
                var lastReadTime = GetLastReadTime(conversationId);

                var unreadCount = messages
                    .Where(m => m.SenderId != currentUserId) // Pas nos propres messages
                    .Where(m => m.SentDateTime > lastReadTime) // Messages plus récents que la dernière lecture
                    .Count();

                _unreadCounts[conversationId] = unreadCount;
                _logger.LogDebug($"Conversation {conversationId}: {unreadCount} messages non lus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'initialisation du comptage pour la conversation {conversationId}");
                _unreadCounts[conversationId] = 0;
            }
        }

        /// <summary>
        /// S'abonne aux nouveaux messages d'une conversation
        /// </summary>
        private void SubscribeToConversationMessages(string conversationId)
        {
            if (_conversationSubscriptions.ContainsKey(conversationId))
                return;

            try
            {
                var subscription = _firebaseService.SubscribeToMessages(conversationId, (message) =>
                {
                    HandleNewMessage(message, conversationId);
                });

                _conversationSubscriptions[conversationId] = subscription;
                _logger.LogDebug($"Abonnement aux messages de la conversation {conversationId} créé pour le comptage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'abonnement aux messages de la conversation {conversationId}");
            }
        }

        /// <summary>
        /// Traite un nouveau message pour mettre à jour le compteur
        /// </summary>
        private void HandleNewMessage(Message message, string conversationId)
        {
            try
            {
                var currentUserId = _authService.GetUserId();
                
                // Ignorer nos propres messages
                if (message.SenderId == currentUserId)
                    return;

                var lastReadTime = GetLastReadTime(conversationId);
                
                // Si le message est plus récent que la dernière lecture, incrémenter le compteur
                if (message.SentDateTime > lastReadTime)
                {
                    if (!_unreadCounts.ContainsKey(conversationId))
                        _unreadCounts[conversationId] = 0;

                    _unreadCounts[conversationId]++;
                    UpdateTotalUnreadCount();
                    
                    _logger.LogDebug($"Nouveau message non lu dans la conversation {conversationId}. Total: {_unreadCounts[conversationId]}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement du nouveau message pour le comptage");
            }
        }

        /// <summary>
        /// Marque tous les messages d'une conversation comme lus
        /// </summary>
        public async Task MarkConversationAsReadAsync(string conversationId)
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                _lastReadTimes[conversationId] = currentTime;
                _unreadCounts[conversationId] = 0;
                
                // Sauvegarder le temps de lecture
                await SaveLastReadTimeAsync(conversationId, currentTime);
                
                UpdateTotalUnreadCount();
                
                _logger.LogDebug($"Conversation {conversationId} marquée comme lue");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du marquage de la conversation {conversationId} comme lue");
            }
        }

        /// <summary>
        /// Obtient le nombre de messages non lus pour une conversation
        /// </summary>
        public int GetUnreadCount(string conversationId)
        {
            return _unreadCounts.TryGetValue(conversationId, out var count) ? count : 0;
        }

        /// <summary>
        /// Ajoute une nouvelle conversation au système de comptage
        /// </summary>
        public async Task AddConversationAsync(string conversationId)
        {
            if (!_isInitialized)
                return;

            await InitializeConversationUnreadCount(conversationId);
            SubscribeToConversationMessages(conversationId);
            UpdateTotalUnreadCount();
        }

        /// <summary>
        /// Supprime une conversation du système de comptage
        /// </summary>
        public void RemoveConversation(string conversationId)
        {
            if (_conversationSubscriptions.TryGetValue(conversationId, out var subscription))
            {
                subscription.Dispose();
                _conversationSubscriptions.Remove(conversationId);
            }

            _unreadCounts.Remove(conversationId);
            _lastReadTimes.Remove(conversationId);
            UpdateTotalUnreadCount();
            
            _logger.LogDebug($"Conversation {conversationId} supprimée du système de comptage");
        }

        /// <summary>
        /// Met à jour le compteur total
        /// </summary>
        private void UpdateTotalUnreadCount()
        {
            TotalUnreadCount = _unreadCounts.Values.Sum();
        }

        /// <summary>
        /// Obtient le temps de dernière lecture pour une conversation
        /// </summary>
        private DateTime GetLastReadTime(string conversationId)
        {
            return _lastReadTimes.TryGetValue(conversationId, out var time) ? time : DateTime.MinValue;
        }

        /// <summary>
        /// Charge les derniers temps de lecture depuis les préférences
        /// </summary>
        private Task LoadLastReadTimesAsync()
        {
            try
            {
                // Pour l'instant, utiliser des préférences locales
                // Dans une version plus avancée, on pourrait synchroniser avec Firebase
                var currentUserId = _authService.GetUserId();
                var key = $"LastReadTimes_{currentUserId}";
                
                // Simuler le chargement depuis les préférences
                // TODO: Implémenter le chargement réel des préférences
                
                return Task.CompletedTask;
            }
            catch
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Sauvegarde le temps de dernière lecture
        /// </summary>
        private Task SaveLastReadTimeAsync(string conversationId, DateTime time)
        {
            try
            {
                // Sauvegarder dans les préférences locales
                var currentUserId = _authService.GetUserId();
                var key = $"LastReadTime_{currentUserId}_{conversationId}";
                
                // En pratique, utiliser Preferences.Set
                Preferences.Set(key, time.ToBinary());
                
                _logger.LogDebug($"Temps de dernière lecture sauvegardé pour la conversation {conversationId}");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la sauvegarde du temps de lecture pour {conversationId}");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Déclenche l'événement PropertyChanged
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            _unreadCounts.Clear();
            _lastReadTimes.Clear();
            _isInitialized = false;
            
            _logger.LogInformation("Service de comptage des messages non lus nettoyé");
        }
    }
}

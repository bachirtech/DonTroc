using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Models;
using Firebase.Database;
using Firebase.Database.Query;

namespace DonTroc.Services
{
    // Service pour gérer les interactions avec les APIs externes - Version Production Optimisée
    public class FirebaseService
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly SecureCloudinaryService _secureCloudinaryService;
        private readonly AuthService _authService;
        private readonly PerformanceService _performanceService;
        private readonly CacheService _cacheService;


        // Le constructeur reçoit maintenant les services d'optimisation
        public FirebaseService(SecureCloudinaryService secureCloudinaryService, AuthService authService,
            PerformanceService performanceService, CacheService cacheService)
        {
            // Initialisation du client Firebase avec configuration optimisée
            _firebaseClient = new FirebaseClient(
                ConfigurationService.FirebaseUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = async () => await authService.GetAuthTokenAsync() ?? string.Empty
                });

            _secureCloudinaryService = secureCloudinaryService;
            _authService = authService;
            _performanceService = performanceService;
            _cacheService = cacheService;
        }

        // Méthode sécurisée pour envoyer plusieurs images vers Cloudinary
        public async Task<List<string>> UploadImagesAsync(List<byte[]> photosData, string userId,
            string? annonceId = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("ID utilisateur requis pour l'upload sécurisé");

            return await _secureCloudinaryService.UploadImagesSecureAsync(photosData, userId, annonceId);
        }

        // Méthode sécurisée pour envoyer des fichiers audio vers Cloudinary
        public async Task<List<string>> UploadAudioAsync(List<byte[]> audioData, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("ID utilisateur requis pour l'upload sécurisé");

            return await _secureCloudinaryService.UploadAudioSecureAsync(audioData, userId);
        }

        // Méthode sécurisée pour supprimer une image
        public async Task<bool> DeleteImageAsync(string imageUrl, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("ID utilisateur requis pour la suppression sécurisée");

            return await _secureCloudinaryService.DeleteImageSecureAsync(imageUrl, userId);
        }

        // Méthode pour ajouter une nouvelle annonce à Firebase
        public async Task AddAnnonceAsync(Annonce annonce)
        {
            // Récupérer le token d'authentification
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Utilisateur non authentifié");
            }

            // Correction : Utiliser le client partagé
            await _firebaseClient
                .Child("Annonces")
                .Child(annonce.Id) // Forcer l'utilisation de l'ID généré localement
                .PutAsync(annonce);
        }

        // Méthode pour récupérer toutes les annonces de Firebase
        public async Task<List<Annonce>> GetAnnoncesAsync()
        {
            return await _performanceService.MeasureAsync<List<Annonce>>("GetAnnonces", async () =>
            {
                // Tentative de récupération depuis le cache
                var cached = await _cacheService.GetAnnoncesAsync("all_annonces", async () =>
                {
                    var currentUser = await _authService.GetCurrentUserAsync();
                    if (currentUser == null)
                        throw new UnauthorizedAccessException("Utilisateur non authentifié");

                    var annonces = await _firebaseClient // Utiliser le client partagé
                        .Child("Annonces")
                        .OnceAsync<Annonce>();

                    var result = annonces?.Select(item =>
                    {
                        var annonce = item.Object;
                        if (annonce != null)
                        {
                            annonce.Id = item.Key;
                            // Cache chaque annonce individuellement pour un accès rapide
                            _cacheService.CacheAnnonce(annonce);
                        }

                        return annonce;
                    }).Where(a => a != null).Cast<Annonce>().ToList() ?? new List<Annonce>();

                    return result;
                });

                return cached ?? new List<Annonce>();
            });
        }

        // Méthode pour récupérer toutes les annonces (pour le Dashboard) - CORRIGÉE
        public async Task<IEnumerable<Annonce>> GetAllAnnoncesAsync()
        {
            // Récupérer le token d'authentification
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Utilisateur non authentifié");
            }

            var annonces = await _firebaseClient // Utiliser le client partagé
                .Child("Annonces")
                .OnceAsync<Annonce>();

            var result = annonces?.Select(item =>
            {
                var annonce = item.Object;
                if (annonce != null)
                {
                    annonce.Id = item.Key; // Assigner l'ID de Firebase à l'annonce
                    return annonce;
                }

                return null;
            }).Where(a => a != null).Cast<Annonce>().ToList() ?? new List<Annonce>();

            return result;
        }

        // Méthode pour récupérer une annonce spécifique par son ID - CORRIGÉE
        public async Task<Annonce?> GetAnnonceAsync(string annonceId)
        {
            var annonce = await _firebaseClient
                .Child("Annonces")
                .Child(annonceId)
                .OnceSingleAsync<Annonce>();

            if (annonce == null) return annonce;
            annonce.Id = annonceId;
            return annonce;
        }

        // Méthode pour récupérer les annonces d'un utilisateur spécifique - CORRIGÉE
        public async Task<List<Annonce>> GetAnnoncesForUserAsync(string userId)
        {
            // Récupérer le token d'authentification
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Utilisateur non authentifié");
            }

            var annonces = await _firebaseClient // Utiliser le client partagé
                .Child("Annonces")
                .OrderBy("UtilisateurId") // Ordonne les annonces par l'ID de l'utilisateur
                .EqualTo(userId) // Filtre pour ne garder que celles de l'utilisateur connecté
                .OnceAsync<Annonce>();

            return annonces.Select(item => new Annonce
            {
                Id = item.Key,
                Titre = item.Object.Titre,
                Description = item.Object.Description,
                Categorie = item.Object.Categorie,
                Type = item.Object.Type,
                UtilisateurId = item.Object.UtilisateurId,
                DateCreation = item.Object.DateCreation,
                DateModification = item.Object.DateModification,
                Localisation = item.Object.Localisation,
                BoostExpirationDate = item.Object.BoostExpirationDate,
                // Correction : s'assure que la liste des URLs des photos est bien récupérée
                PhotosUrls = item.Object.PhotosUrls
            }).ToList();
        }

        // Méthode pour supprimer une annonce de Firebase - CORRIGÉE
        public async Task DeleteAnnonceAsync(string annonceId)
        {
            // Récupérer le token d'authentification
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Utilisateur non authentifié");
            }

            // Cible l'annonce spécifique par son ID et la supprime
            await _firebaseClient // Utiliser le client partagé
                .Child("Annonces")
                .Child(annonceId)
                .DeleteAsync();
        }

        // Méthode pour mettre à jour une annonce dans Firebase - CORRIGÉE
        public async Task UpdateAnnonceAsync(string annonceId, Annonce annonce)
        {
            // Récupérer le token d'authentification
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Utilisateur non authentifié");
            }

            // Cible l'annonce spécifique par son ID et la met à jour
            await _firebaseClient // Utiliser le client partagé
                .Child("Annonces")
                .Child(annonceId)
                .PutAsync(annonce);
        }

        public async Task UpdateAnnonceImageUrlAsync(string annonceId, List<string> photosUrls)
        {
            var updates = new Dictionary<string, object>
            {
                { "PhotosUrls", photosUrls }
            };
            await _firebaseClient.Child("Annonces").Child(annonceId).PatchAsync(updates);
        }

        /// <summary>
        /// Met à jour une annonce pour la "booster" pendant 24 heures - CORRIGÉE
        /// </summary>
        /// <param name="annonceId">L'ID de l'annonce à booster.</param>
        public async Task BoostAnnonceAsync(string annonceId)
        {
            // Récupérer le token d'authentification
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Utilisateur non authentifié");
            }

            var boostExpiration = DateTime.UtcNow.AddHours(24);
            var updates = new Dictionary<string, object>
            {
                { "BoostExpirationDate", boostExpiration }
            };

            await _firebaseClient // Utiliser le client partagé
                .Child("Annonces")
                .Child(annonceId)
                .PatchAsync(updates);
        }

        // Méthode pour envoyer une image vers Cloudinary
        public async Task<string> UploadImageAsync(byte[] imageData, string fileName, string userId,
            string? annonceId = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("ID utilisateur requis pour l'upload sécurisé");

            // Le paramètre est déjà un byte[], il peut être utilisé directement.
            var urls = await _secureCloudinaryService.UploadImagesSecureAsync(new List<byte[]> { imageData }, userId,
                annonceId);
            return urls.FirstOrDefault() ?? string.Empty;
        }

        // --- Méthodes pour le Profil Utilisateur ---

        public async Task SaveUserProfileAsync(UserProfile userProfile)
        {
            try
            {
                // Validation des données avant sauvegarde
                if (userProfile == null)
                    throw new ArgumentNullException(nameof(userProfile));

                if (string.IsNullOrEmpty(userProfile.Id))
                    throw new ArgumentException("L'ID du profil utilisateur est requis");

                // Vérifier que l'utilisateur est authentifié
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                    throw new UnauthorizedAccessException("Utilisateur non authentifié");

                // Vérifier que l'utilisateur peut modifier ce profil
                if (userProfile.Id != currentUser.Uid)
                    throw new UnauthorizedAccessException("Vous ne pouvez modifier que votre propre profil");

                // S'assurer que les champs requis sont présents
                if (string.IsNullOrEmpty(userProfile.Name))
                    userProfile.Name = "Utilisateur";

                if (userProfile.DateInscription == default)
                    userProfile.DateInscription = DateTime.UtcNow;


                await _firebaseClient
                    .Child("UserProfiles")
                    .Child(userProfile.Id)
                    .PutAsync(userProfile);

            }
            catch (Firebase.Database.FirebaseException ex) when (ex.Message.Contains("Permission denied"))
            {
                throw new UnauthorizedAccessException(
                    "Accès refusé lors de la sauvegarde du profil. Vérifiez votre authentification.", ex);
            }
        }

        public async Task<string?> UploadProfilePictureAsync(Stream imageStream, string fileName, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("ID utilisateur requis pour l'upload sécurisé");

            // Convertir le stream en byte array pour utiliser le service sécurisé
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();

            var urls = await _secureCloudinaryService.UploadImagesSecureAsync(new List<byte[]> { imageData },
                userId); // Pas d'annonceId pour la photo de profil
            return urls.FirstOrDefault();
        }

        // --- Méthodes pour la Messagerie ---

        /// <summary>
        /// Récupère une conversation existante pour une annonce et un acheteur, ou en crée une nouvelle.
        /// Version corrigée pour éviter les erreurs de requête Firebase complexe.
        /// </summary>
        public async Task<Conversation> GetOrCreateConversationAsync(string annonceId)
        {
            try
            {
                // 1. Récupérer les informations de l'annonce et des utilisateurs
                var annonce = await GetAnnonceAsync(annonceId);
                if (annonce == null)
                {
                    throw new Exception("L'annonce est introuvable.");
                }

                var sellerId = annonce.UtilisateurId;
                var buyerId = _authService.GetUserId();

                if (string.IsNullOrEmpty(buyerId))
                {
                    throw new Exception("L'utilisateur actuel n'est pas authentifié.");
                }

                // Éviter qu'un utilisateur crée une conversation avec lui-même
                if (sellerId == buyerId)
                {
                    throw new Exception("Vous ne pouvez pas créer une conversation avec vous-même.");
                }

                // 2. Récupérer le token et créer le client authentifié
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    throw new UnauthorizedAccessException("Utilisateur non authentifié");
                }

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                // 3. Recherche de conversation existante avec gestion d'erreur améliorée
                Conversation? existingConversation = null;

                try
                {
                    var allConversations = await authenticatedClient
                        .Child("Conversations")
                        .OnceAsync<Conversation>();

                    if (allConversations != null && allConversations.Any())
                    {
                        existingConversation = allConversations
                            .Where(item => item.Object != null)
                            .Select(item =>
                            {
                                var conv = item.Object;
                                conv.Id = item.Key;
                                return conv;
                            })
                            .FirstOrDefault(c =>
                                !string.IsNullOrEmpty(c.AnnonceId) &&
                                c.AnnonceId == annonceId &&
                                ((!string.IsNullOrEmpty(c.BuyerId) && !string.IsNullOrEmpty(c.SellerId)) &&
                                 ((c.BuyerId == buyerId && c.SellerId == sellerId) ||
                                  (c.BuyerId == sellerId && c.SellerId == buyerId))));
                    }
                }
                catch (Exception)
                {
                    // En cas d'erreur de lecture, on continue pour créer une nouvelle conversation
                }

                if (existingConversation != null)
                {
                    return existingConversation;
                }

                // 4. Créer une nouvelle conversation avec structure améliorée
                var newConversation = new Conversation
                {
                    AnnonceId = annonceId,
                    SellerId = sellerId,
                    BuyerId = buyerId,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageTimestamp = DateTime.UtcNow,
                    ParticipantIds = new Dictionary<string, bool>
                    {
                        { sellerId, true },
                        { buyerId, true }
                    },
                    AnnonceTitre = annonce.Titre,
                    AnnonceImageUrl = annonce.PhotosUrls?.FirstOrDefault() ?? annonce.FirstPhotoUrl ?? string.Empty,
                    LastMessage = "Conversation démarrée"
                };

                try
                {
                    var result = await authenticatedClient
                        .Child("Conversations")
                        .PostAsync(newConversation);

                    newConversation.Id = result.Key;
                    return newConversation;
                }
                catch (Exception createEx)
                {
                    throw new Exception($"Impossible de créer la conversation: {createEx.Message}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors du démarrage de la conversation: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère toutes les conversations d'un utilisateur en cherchant celles où il est soit acheteur soit vendeur
        /// Version corrigée pour éviter les erreurs de requête Firebase complexe.
        /// </summary>
        public async Task<List<Conversation>> GetUserConversationsAsync(string userId)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                    throw new UnauthorizedAccessException("Utilisateur non authentifié");

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                // Approche simplifiée : récupérer TOUTES les conversations et filtrer localement
                // Cela évite les problèmes de requête Firebase complexe avec OrderBy et EqualTo
                List<Conversation> allConversations;

                try
                {
                    var conversationsData = await authenticatedClient
                        .Child("Conversations")
                        .OnceAsync<Conversation>();

                    allConversations = conversationsData
                        .Where(item => item.Object != null)
                        .Select(item =>
                        {
                            var conversation = item.Object;
                            conversation.Id = item.Key;
                            return conversation;
                        })
                        .Where(c => c.BuyerId == userId || c.SellerId == userId)
                        .ToList();

                }
                catch (Exception)
                {
                    // Retourner une liste vide en cas d'erreur plutôt que de faire planter l'app
                    return new List<Conversation>();
                }

                // Enrichir les conversations avec les images des annonces si manquantes
                // ET compter les messages non lus
                foreach (var conversation in allConversations)
                {
                    // Compter les messages non lus pour cette conversation
                    try
                    {
                        var messages = await authenticatedClient
                            .Child("Messages")
                            .Child(conversation.Id)
                            .OnceAsync<Message>();
                        
                        // Compter les messages non lus (envoyés par l'autre utilisateur et non lus)
                        conversation.UnreadCount = messages
                            .Where(m => m.Object != null)
                            .Select(m => m.Object)
                            .Count(m => m.SenderId != userId && !m.IsRead);
                    }
                    catch
                    {
                        conversation.UnreadCount = 0;
                    }
                    
                    // Enrichir avec l'image de l'annonce si manquante
                    if (string.IsNullOrEmpty(conversation.AnnonceImageUrl) && !string.IsNullOrEmpty(conversation.AnnonceId))
                    {
                        try
                        {
                            var annonce = await GetAnnonceAsync(conversation.AnnonceId);
                            if (annonce != null)
                            {
                                conversation.AnnonceImageUrl = annonce.PhotosUrls?.FirstOrDefault() ?? annonce.FirstPhotoUrl ?? string.Empty;
                                
                                // Mettre aussi à jour le titre si vide
                                if (string.IsNullOrEmpty(conversation.AnnonceTitre))
                                {
                                    conversation.AnnonceTitre = annonce.Titre;
                                }
                                
                                // Optionnel: sauvegarder l'image dans la conversation pour les prochaines fois
                                try
                                {
                                    var updates = new Dictionary<string, object>
                                    {
                                        { "AnnonceImageUrl", conversation.AnnonceImageUrl },
                                        { "AnnonceTitre", conversation.AnnonceTitre }
                                    };
                                    await authenticatedClient
                                        .Child("Conversations")
                                        .Child(conversation.Id)
                                        .PatchAsync(updates);
                                }
                                catch
                                {
                                    // Ignorer l'erreur de mise à jour - ce n'est pas critique
                                }
                            }
                        }
                        catch
                        {
                            // Ignorer l'erreur si l'annonce n'existe plus
                        }
                    }
                }

                // Trier par timestamp du dernier message (plus récent en premier)
                return allConversations
                    .OrderByDescending(c => c.LastMessageTimestamp)
                    .ToList();
            }
            catch (Exception)
            {
                // Retourner une liste vide pour éviter le crash de l'application
                return new List<Conversation>();
            }
        }

        /// <summary>
        /// Supprime une conversation et tous ses messages associés de Firebase.
        /// </summary>
        public async Task DeleteConversationAsync(string conversationId)
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
                throw new UnauthorizedAccessException("Utilisateur non authentifié");

            // Supprimer les messages de la conversation
            try
            {
                await _firebaseClient
                    .Child("Messages")
                    .Child(conversationId)
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Firebase] Erreur suppression messages de {conversationId}: {ex.Message}");
                // Continuer même si les messages n'existent pas
            }

            // Supprimer la conversation elle-même
            await _firebaseClient
                .Child("Conversations")
                .Child(conversationId)
                .DeleteAsync();
        }

        public async Task<List<Message>>
            GetMessagesAsync(string conversationId) // Récupre les messages d'une conversation
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
                throw new UnauthorizedAccessException("Utilisateur non authentifié");

            var tokenResult = await currentUser.GetIdTokenResultAsync();
            var authToken = tokenResult.Token;
            var authenticatedClient = new FirebaseClient(
                ConfigurationService.FirebaseUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                });

            var messages = await authenticatedClient
                .Child("Messages")
                .Child(conversationId)
                .OrderBy("Timestamp")
                .OnceAsync<Message>();
            return messages.Select(m =>
            {
                var message = m.Object;
                message.Id = m.Key;
                return message;
            }).ToList();
        }

        /// <summary>
        /// S'abonne aux messages en temps réel pour une conversation donnée
        /// </summary>
        public IDisposable SubscribeToMessages(string conversationId, Action<Message> onMessageReceived,
            Action<Exception>? onError = null)
        {
            try
            {
                var subscription = _firebaseClient
                    .Child("Messages")
                    .Child(conversationId)
                    .OrderBy("Timestamp")
                    .AsObservable<Message>()
                    .Subscribe(
                        d =>
                        {
                            if (d?.Object == null) return;
                            var message = d.Object;
                            message.Id = d.Key;
                            onMessageReceived(message);
                        },
                        ex => { onError?.Invoke(ex); }
                    );

                return subscription;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
                return new EmptyDisposable();
            }
        }

        /// <summary>
        /// Classe helper pour retourner un IDisposable vide en cas d'erreur
        /// </summary>
        private class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        /// <summary>
        /// Indique qu'un utilisateur est en train d'écrire
        /// </summary>
        public async Task SetTypingIndicatorAsync(string conversationId, string userId, bool isTyping)
        {
            if (isTyping)
            {
                // L'utilisateur est en train d'écrire, on met à jour le timestamp
                await _firebaseClient
                    .Child("TypingIndicators")
                    .Child(conversationId)
                    .Child(userId)
                    .PutAsync(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            else
            {
                // L'utilisateur n'est plus en train d'écrire, on supprime l'entrée
                await _firebaseClient
                    .Child("TypingIndicators")
                    .Child(conversationId)
                    .Child(userId)
                    .DeleteAsync();
            }
        }

        /// <summary>
        /// S'abonne aux indicateurs d'écriture en temps réel
        /// </summary>
        public IDisposable SubscribeToTypingIndicators(string conversationId, string currentUserId,
            Action<bool> onTypingChanged)
        {
            return _firebaseClient
                .Child("TypingIndicators")
                .Child(conversationId)
                .AsObservable<Dictionary<string, long>>()
                .Subscribe(
                    d =>
                    {
                        try
                        {
                            if (d?.Object != null)
                            {
                                // Vérifier si quelqu'un d'autre que l'utilisateur actuel est en train d'écrire
                                var now = DateTime.UtcNow.Ticks;
                                var someoneTyping = d.Object
                                    .Where(kvp => kvp.Key != currentUserId) // Exclure l'utilisateur actuel
                                    .Any(kvp => (now - kvp.Value) <
                                                TimeSpan.FromSeconds(3).Ticks); // Indicateur valide si < 3 secondes

                                onTypingChanged(someoneTyping);
                            }
                            else
                            {
                                onTypingChanged(false);
                            }
                        }
                        catch (Exception)
                        {
                            onTypingChanged(false);
                        }
                    },
                    ex =>
                    {
                        // Gestion des erreurs de streaming (ex: permissions Firebase)
                        // Ne pas propager l'erreur pour éviter le crash
                        onTypingChanged(false);
                    });
        }

        /// <summary>
        /// Envoie un message dans une conversation et met à jour les métadonnées de la conversation
        /// </summary>
        public async Task SendMessageAsync(Message message)
        {
            try
            {
                // Validation du ConversationId
                if (string.IsNullOrEmpty(message.ConversationId))
                {
                    throw new InvalidOperationException("ConversationId est requis pour envoyer un message");
                }


                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                    throw new UnauthorizedAccessException("Utilisateur non authentifié");

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                // Définir les propriétés par défaut
                message.Status = MessageStatus.Sending;
                message.Timestamp = DateTime.UtcNow;

                // Envoyer le message
                var result = await authenticatedClient
                    .Child("Messages")
                    .Child(message.ConversationId)
                    .PostAsync(message);

                message.Id = result.Key;
                message.Status = MessageStatus.Sent;

                // Mettre à jour le statut
                await authenticatedClient
                    .Child("Messages")
                    .Child(message.ConversationId)
                    .Child(message.Id)
                    .PutAsync(message);

                // Mettre à jour la conversation avec le dernier message
                var updates = new Dictionary<string, object>
                {
                    { "LastMessage", message.Text },
                    { "LastMessageTimestamp", message.Timestamp },
                    { "LastMessageType", message.Type.ToString() }
                };

                await authenticatedClient
                    .Child("Conversations")
                    .Child(message.ConversationId)
                    .PatchAsync(updates);
            }
            catch (Exception)
            {
                message.Status = MessageStatus.Failed;
                throw;
            }
        }

        /// <summary>
        /// Supprime un message d'une conversation
        /// </summary>
        public async Task DeleteMessageAsync(string messageId, string conversationId)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                    throw new UnauthorizedAccessException("Utilisateur non authentifié");

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                await authenticatedClient
                    .Child("Messages")
                    .Child(conversationId)
                    .Child(messageId)
                    .DeleteAsync();
            }
            catch (Exception)
            {
                // Ne pas relancer l'exception pour éviter de faire planter l'application en cas d'échec de suppression
            }
        }

        /// <summary>
        /// Marque tous les messages non lus d'une conversation comme lus par l'utilisateur
        /// </summary>
        public async Task MarkMessagesAsReadAsync(string conversationId, string currentUserId)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                    return;

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                // Récupérer tous les messages de la conversation
                var messages = await authenticatedClient
                    .Child("Messages")
                    .Child(conversationId)
                    .OnceAsync<Message>();

                foreach (var messageItem in messages)
                {
                    var message = messageItem.Object;
                    // Ne marquer comme lu que les messages envoyés par l'autre utilisateur
                    if (message.SenderId != currentUserId && message.Status != MessageStatus.Read)
                    {
                        var updates = new Dictionary<string, object>
                        {
                            { "Status", MessageStatus.Read.ToString() },
                            { "IsRead", true },
                            { "ReadAt", DateTime.UtcNow.ToString("o") }
                        };

                        await authenticatedClient
                            .Child("Messages")
                            .Child(conversationId)
                            .Child(messageItem.Key)
                            .PatchAsync(updates);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du marquage des messages comme lus: {ex.Message}");
            }
        }

        /// <summary>
        /// Marque un message spécifique comme livré
        /// </summary>
        public async Task MarkMessageAsDeliveredAsync(string conversationId, string messageId)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                    return;

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                var updates = new Dictionary<string, object>
                {
                    { "Status", MessageStatus.Delivered.ToString() },
                    { "IsDelivered", true },
                    { "DeliveredAt", DateTime.UtcNow.ToString("o") }
                };

                await authenticatedClient
                    .Child("Messages")
                    .Child(conversationId)
                    .Child(messageId)
                    .PatchAsync(updates);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du marquage du message comme livré: {ex.Message}");
            }
        }


        // === MÉTHODES POUR LES TRANSACTIONS ===

        /// <summary>
        /// Récupérer une transaction par son ID
        /// </summary>
        public async Task<Transaction?> GetTransactionAsync(string transactionId)
        {
            try
            {
                var transaction = await _firebaseClient
                    .Child("Transactions")
                    .Child(transactionId)
                    .OnceSingleAsync<Transaction>();

                if (transaction != null)
                {
                    transaction.Id = transactionId;
                }

                return transaction;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Récupérer les transactions d'un utilisateur
        /// </summary>
        public async Task<List<Transaction>> GetUserTransactionsAsync(string userId)
        {
            try
            {
                // Récupérer les transactions avec lesquelles l'utilisateur est propriétaire
                var ownerTransactions = await _firebaseClient
                    .Child("Transactions")
                    .OrderBy("ProprietaireId")
                    .EqualTo(userId)
                    .OnceAsync<Transaction>();

                // Récupérer les transactions avec lesquelles l'utilisateur est demandeur
                var buyerTransactions = await _firebaseClient
                    .Child("Transactions")
                    .OrderBy("DemandeurId")
                    .EqualTo(userId)
                    .OnceAsync<Transaction>();

                var allTransactions = new List<Transaction>();
                if (allTransactions == null)
                    throw
                        new ArgumentNullException(
                            nameof(allTransactions)); // si la transaction est null, on renvoie une liste vide

                // Ajouter les transactions en tant que propriétaire
                allTransactions.AddRange(ownerTransactions.Select(item => new Transaction
                {
                    Id = item.Key,
                    Type = item.Object.Type,
                    Statut = item.Object.Statut,
                    AnnonceId = item.Object.AnnonceId,
                    ProprietaireId = item.Object.ProprietaireId,
                    DemandeurId = item.Object.DemandeurId,
                    AnnonceEchangeId = item.Object.AnnonceEchangeId,
                    DateCreation = item.Object.DateCreation,
                    DateConfirmation = item.Object.DateConfirmation
                }));

                // Ajouter les transactions en tant que demandeur (éviter les doublons)
                allTransactions.AddRange(buyerTransactions
                    .Where(item =>
                    {
                        if (item == null) throw new ArgumentNullException(nameof(item));
                        return allTransactions.All(t => t.Id != item.Key);
                    })
                    .Select(item => new Transaction
                    {
                        Id = item.Key,
                        Type = item.Object.Type,
                        Statut = item.Object.Statut,
                        AnnonceId = item.Object.AnnonceId,
                        ProprietaireId = item.Object.ProprietaireId,
                        DemandeurId = item.Object.DemandeurId,
                        AnnonceEchangeId = item.Object.AnnonceEchangeId,
                        DateCreation = item.Object.DateCreation,
                        DateConfirmation = item.Object.DateConfirmation
                    }));

                return allTransactions.OrderByDescending(t => t.DateCreation).ToList();
            }
            catch (Exception)
            {
                return new List<Transaction>();
            }
        }

        /// <summary>
        /// Créer une nouvelle transaction
        /// </summary>
        public async Task<string?> CreateTransactionAsync(Transaction transaction)
        {
            try
            {
                var transactionId = Guid.NewGuid().ToString(); // Générer un ID unique
                transaction.Id = transactionId;
                transaction.DateCreation = DateTime.UtcNow;

                var currentUser = await _authService.GetCurrentUserAsync(); // Récupérer l'utilisateur actuel
                if (currentUser == null)
                    throw new UnauthorizedAccessException("Utilisateur non authentifié");

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                await authenticatedClient
                    .Child("Transactions")
                    .Child(transactionId)
                    .PutAsync(transaction);

                return transactionId;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Mettre à jour le statut d'une transaction
        /// </summary>
        public async Task<bool> UpdateTransactionStatusAsync(string transactionId, StatutTransaction nouveauStatut)
        {
            try
            {
                var transaction = await GetTransactionAsync(transactionId);
                if (transaction == null) return false;

                transaction.Statut = nouveauStatut;

                // Si la transaction est confirmée, enregistrer la date
                if (nouveauStatut == StatutTransaction.Confirmee)
                {
                    transaction.DateConfirmation = DateTime.UtcNow;
                }

                await _firebaseClient
                    .Child("Transactions")
                    .Child(transactionId)
                    .PutAsync(transaction);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // === MÉTHODES POUR LES PROFILS UTILISATEURS ===

        /// <summary>
        /// Récupérer un profil utilisateur par son ID
        /// </summary>
        public async Task<UserProfile?> GetUserProfileAsync(string userId)
        {
            try
            {
                // Récupérer le token d'authentification
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    throw new UnauthorizedAccessException("Utilisateur non authentifié");
                }

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                var profile = await authenticatedClient
                    .Child("UserProfiles")
                    .Child(userId)
                    .OnceSingleAsync<UserProfile>();

                if (profile != null)
                {
                    profile.Id = userId;
                }

                return profile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetUserProfile] ❌ Erreur pour userId={userId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Mettre à jour les statistiques de notation d'un profil utilisateur
        /// </summary>
        public async Task<bool> UpdateUserRatingStatsAsync(string userId, double noteMoyenne, int nombreEvaluations,
            int nombreEchanges)
        {
            try
            {
                var profile = await GetUserProfileAsync(userId);
                if (profile == null) return false;

                // Mettre à jour les statistiques
                profile.NoteMoyenne = noteMoyenne;
                profile.NombreEvaluations = nombreEvaluations;
                profile.NombreEchangesReussis = nombreEchanges;

                // Recalculer le badge
                profile.CalculerBadgeConfiance();

                await _firebaseClient
                    .Child("UserProfiles")
                    .Child(userId)
                    .PutAsync(profile);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task UpdateUserPointsAsync(string userId, int points) // Méthode pour les points de l'utilisateur
        {
            var userProfile = await GetUserProfileAsync(userId);
            if (userProfile != null)
            {
                userProfile.Points += points;
                await SaveUserProfileAsync(userProfile);
            }
        }

        public async Task<List<UserAction>>
            GetUserActionsAsync(string userId, int limit) // Méthode pour les actions de l'utilisateur'
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null || currentUser.Uid != userId)
                {
                    throw new UnauthorizedAccessException("Utilisateur non authentifié ou accès non autorisé.");
                }

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                var actions = await authenticatedClient
                    .Child("UserActions")
                    .Child(userId)
                    .OrderBy("Timestamp")
                    .LimitToLast(limit)
                    .OnceAsync<UserAction>();

                return actions.Select(item => item.Object).OrderByDescending(a => a.Timestamp).ToList();
            }
            catch (Exception)
            {
                return new List<UserAction>();
            }
        }

        public async Task<UserStats?> GetUserStatsAsync(string userId)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null || currentUser.Uid != userId)
                {
                    throw new UnauthorizedAccessException("Utilisateur non authentifié ou accès non autorisé.");
                }

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                var stats = await authenticatedClient
                    .Child("UserStats")
                    .Child(userId)
                    .OnceSingleAsync<UserStats>();

                return stats;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task
            SaveUserStatsAsync(string userId,
                UserStats stats) // Méthode pour sauvegarder les statistiques de l'utilisateur
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null || currentUser.Uid != userId)
                {
                    throw new UnauthorizedAccessException("Utilisateur non authentifié ou accès non autorisé.");
                }

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                await authenticatedClient
                    .Child("UserStats")
                    .Child(userId)
                    .PutAsync(stats);
            }
            catch (Exception)
            {
                // Gérer l'erreur si nécessaire
            }
        }

        public async Task SaveUserActionAsync(UserAction action)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null || currentUser.Uid != action.UserId)
                {
                    throw new UnauthorizedAccessException("Utilisateur non authentifié ou accès non autorisé.");
                }

                var tokenResult = await currentUser.GetIdTokenResultAsync();
                var authToken = tokenResult.Token;
                var authenticatedClient = new FirebaseClient(
                    ConfigurationService.FirebaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(authToken)
                    });

                // Utiliser PostAsync pour générer un ID unique pour chaque action
                await authenticatedClient
                    .Child("UserActions")
                    .Child(action.UserId)
                    .PostAsync(action);
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas faire planter l'application
                Debug.WriteLine($"Erreur lors de la sauvegarde de l'action utilisateur : {ex.Message}");
            }
        }

        /// <summary>
        /// Supprime toutes les données utilisateur de Firebase Firestore
        /// </summary>
        public async Task<bool> DeleteAllUserDataAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    throw new ArgumentException("ID utilisateur requis pour la suppression");
                }

                // Supprimer toutes les annonces de l'utilisateur
                var userAnnonces = await _firebaseClient
                    .Child("Annonces")
                    .OrderBy("UtilisateurId")
                    .EqualTo(userId)
                    .OnceAsync<Annonce>();

                foreach (var annonce in userAnnonces)
                {
                    await _firebaseClient
                        .Child("Annonces")
                        .Child(annonce.Key)
                        .DeleteAsync();
                }

                // Supprimer le profil utilisateur
                await _firebaseClient
                    .Child("UserProfiles")
                    .Child(userId)
                    .DeleteAsync();

                // Supprimer les conversations de l'utilisateur
                var userConversations = await _firebaseClient
                    .Child("Conversations")
                    .OrderBy("UtilisateurId")
                    .EqualTo(userId)
                    .OnceAsync<Conversation>();

                var userConversations2 = await _firebaseClient
                    .Child("Conversations")
                    .OrderBy("UtilisateurId")
                    .EqualTo(userId)
                    .OnceAsync<Conversation>();

                foreach (var conversation in userConversations.Concat(userConversations2))
                {
                    await _firebaseClient
                        .Child("Conversations")
                        .Child(conversation.Key)
                        .DeleteAsync();
                }

                // Supprimer les messages de l'utilisateur
                var userMessages = await _firebaseClient
                    .Child("Messages")
                    .OrderBy("SenderId")
                    .EqualTo(userId)
                    .OnceAsync<Message>();

                foreach (var message in userMessages)
                {
                    await _firebaseClient
                        .Child("Messages")
                        .Child(message.Key)
                        .DeleteAsync();
                }

                // Supprimer les transactions de l'utilisateur
                var userTransactions = await _firebaseClient
                    .Child("Transactions")
                    .OrderBy("SenderId")
                    .EqualTo(userId)
                    .OnceAsync<Transaction>();

                var userTransactions2 = await _firebaseClient
                    .Child("Transactions")
                    .OrderBy("ReceiverId")
                    .EqualTo(userId)
                    .OnceAsync<Transaction>();

                foreach (var transaction in userTransactions.Concat(userTransactions2))
                {
                    await _firebaseClient
                        .Child("Transactions")
                        .Child(transaction.Key)
                        .DeleteAsync();
                }

                // Supprimer les favoris de l'utilisateur
                var userFavorites = await _firebaseClient
                    .Child("Favorites")
                    .OrderBy("UtilisateurId")
                    .EqualTo(userId)
                    .OnceAsync<Favorite>();

                foreach (var favorite in userFavorites)
                {
                    await _firebaseClient
                        .Child("Favorites")
                        .Child(favorite.Key)
                        .DeleteAsync();
                }

                // Supprimer les évaluations données et reçues par l'utilisateur
                var userRatings = await _firebaseClient
                    .Child("Ratings")
                    .OrderBy("EvaluateurId")
                    .EqualTo(userId)
                    .OnceAsync<Rating>();

                var userRatings2 = await _firebaseClient
                    .Child("Ratings")
                    .OrderBy("EvalueId")
                    .EqualTo(userId)
                    .OnceAsync<Rating>();

                foreach (var rating in userRatings.Concat(userRatings2))
                {
                    await _firebaseClient
                        .Child("Ratings")
                        .Child(rating.Key)
                        .DeleteAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la suppression des données utilisateur: {ex.Message}");
                return false;
            }
        }

        public async Task SaveData(string s, Report report) // Méthode de sauvegarde donnés
        {
            try
            {
                await _firebaseClient
                    .Child(s)
                    .Child(report.Id)
                    .PutAsync(report);
            }
            catch (Exception ex)
            {
                // Log exception
                throw new Exception($"Erreur lors de la sauvegarde du rapport: {ex.Message}");
            }
        }

        /// <summary>
        /// Méthode générique pour sauvegarder n'importe quel objet dans Firebase
        /// </summary>
        public async Task SaveDataAsync<T>(string path, T data)
        {
            try
            {
                await _firebaseClient
                    .Child(path)
                    .PutAsync(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la sauvegarde: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateData(string s, Dictionary<string, object> updates)
        {
            try
            {
                await _firebaseClient
                    .Child(s)
                    .PatchAsync(updates);
            }
            catch (Exception ex)
            {
                // Log exception
                throw new Exception($"Erreur lors de la mise à jour des données: {ex.Message}");
            }
        }

        public async Task<Dictionary<string, T>?> GetData<T>(string path) // méthode de récuperation donnés
        {
            try
            {
                var data = await _firebaseClient
                    .Child(path)
                    .OnceAsync<T>();

                if (data == null || !data.Any())
                {
                    Debug.WriteLine($"[FirebaseService] GetData: Aucune donnée trouvée dans {path}");
                    return null;
                }

                var result = data.ToDictionary(item => item.Key, item => item.Object);
                Debug.WriteLine($"[FirebaseService] GetData: {result.Count} éléments trouvés dans {path}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FirebaseService] Erreur GetData({path}): {ex.Message}");
                return null;
            }
        }

        // === MÉTHODES POUR LES NOTIFICATIONS DE PROXIMITÉ ===

        /// <summary>
        /// Met à jour la position de l'utilisateur dans son profil
        /// </summary>
        public async Task UpdateUserLocationAsync(string userId, double latitude, double longitude)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    { "LastLatitude", latitude },
                    { "LastLongitude", longitude },
                    { "LastLocationUpdate", DateTime.UtcNow.ToString("o") }
                };

                await _firebaseClient
                    .Child("UserProfiles")
                    .Child(userId)
                    .PatchAsync(updates);
            }
            catch (Exception ex) when (ex.Message.Contains("Permission denied"))
            {
                try
                {
                    var profile = await GetUserProfileAsync(userId);
                    if (profile != null)
                    {
                        profile.LastLatitude = latitude;
                        profile.LastLongitude = longitude;
                        profile.LastLocationUpdate = DateTime.UtcNow;
                        await SaveUserProfileAsync(profile);
                    }
                    else
                    {
                        var currentUser = await _authService.GetCurrentUserAsync();
                        var newProfile = new UserProfile
                        {
                            Id = userId,
                            Name = currentUser?.DisplayName ?? "Utilisateur",
                            LastLatitude = latitude,
                            LastLongitude = longitude,
                            LastLocationUpdate = DateTime.UtcNow,
                            DateInscription = DateTime.UtcNow
                        };
                        await SaveUserProfileAsync(newProfile);
                    }
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"[Location] Erreur fallback: {fallbackEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Location] Erreur MAJ position: {ex.Message}");
                // Ne pas relancer - la mise à jour de position n'est pas critique
            }
        }

        /// <summary>
        /// Met à jour les préférences de notification de proximité
        /// </summary>
        public async Task UpdateProximityPreferencesAsync(string userId, bool enabled, double radiusKm)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    { "ProximityNotificationsEnabled", enabled },
                    { "NotificationRadius", radiusKm }
                };

                await _firebaseClient
                    .Child("UserProfiles")
                    .Child(userId)
                    .PatchAsync(updates);
            }
            catch (Exception ex) when (ex.Message.Contains("Permission denied"))
            {
                try
                {
                    var profile = await GetUserProfileAsync(userId);
                    if (profile != null)
                    {
                        profile.ProximityNotificationsEnabled = enabled;
                        profile.NotificationRadius = radiusKm;
                        await SaveUserProfileAsync(profile);
                    }
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"[Proximity] Erreur fallback: {fallbackEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Proximity] Erreur MAJ préférences: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère tous les profils utilisateurs (pour les notifications de proximité)
        /// </summary>
        public async Task<List<UserProfile>> GetAllUserProfilesAsync()
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    throw new UnauthorizedAccessException("Utilisateur non authentifié");
                }

                var profiles = await _firebaseClient
                    .Child("UserProfiles")
                    .OnceAsync<UserProfile>();

                return profiles?.Select(item =>
                {
                    var profile = item.Object;
                    if (profile != null)
                    {
                        profile.Id = item.Key;
                    }
                    return profile;
                }).Where(p => p != null).Cast<UserProfile>().ToList() ?? new List<UserProfile>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la récupération des profils: {ex.Message}");
                return new List<UserProfile>();
            }
        }

        /// <summary>
        /// Sauvegarde les statistiques de notification de proximité
        /// </summary>
        public async Task SaveProximityNotificationStatsAsync(ProximityNotificationStats stats)
        {
            try
            {
                await _firebaseClient
                    .Child("ProximityNotificationStats")
                    .Child(stats.AnnonceId)
                    .PutAsync(stats);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la sauvegarde des stats de notification: {ex.Message}");
            }
        }
    }
}

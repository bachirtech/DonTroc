using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace DonTroc.Services
{
    /// <summary>
    /// Service pour gérer l'intégration sociale : parrainage, amis, partages
    /// </summary>
    public class SocialService
    {
        private readonly FirebaseService _firebaseService;
        private readonly AuthService _authService;
        private readonly FirebaseClient _firebaseClient;

        public SocialService(FirebaseService firebaseService, AuthService authService)
        {
            _firebaseService = firebaseService;
            _authService = authService;
            // Créer notre propre client Firebase authentifié pour les opérations sociales
            _firebaseClient = new FirebaseClient(
                ConfigurationService.FirebaseUrl,
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = async () => await authService.GetAuthTokenAsync() ?? string.Empty
                });
        }

        #region Système de parrainage

        /// <summary>
        /// Génère un code de parrainage unique pour l'utilisateur
        /// </summary>
        public async Task<ReferralCode> GenerateReferralCodeAsync()
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) throw new UnauthorizedAccessException();

            // Vérifier si l'utilisateur a déjà un code actif
            var existingCode = await GetUserReferralCodeAsync(user.Uid);
            if (existingCode != null && existingCode.IsActive)
                return existingCode;

            var code = new ReferralCode
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Uid,
                Code = GenerateUniqueCode(),
                DateCreation = DateTime.UtcNow,
                IsActive = true
            };

            // Utiliser directement le client Firebase
            await _firebaseClient
                .Child("referral_codes")
                .Child(code.Id)
                .PutAsync(code);
            
            return code;
        }

        /// <summary>
        /// Utilise un code de parrainage lors de l'inscription
        /// </summary>
        public async Task<bool> UseReferralCodeAsync(string code)
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return false;

            var referralCode = await GetReferralCodeByCodeAsync(code);
            if (referralCode == null || !referralCode.IsActive || 
                referralCode.NbUtilisations >= referralCode.MaxUtilisations ||
                referralCode.UserId == user.Uid) // Ne peut pas utiliser son propre code
                return false;

            // Créer l'amitié
            var friendship = new Friendship
            {
                Id = Guid.NewGuid().ToString(),
                UserId = referralCode.UserId,
                FriendId = user.Uid,
                DateCreation = DateTime.UtcNow,
                Status = FriendshipStatus.Accepted,
                ReferralCodeUsed = code
            };

            // Mettre à jour le code de parrainage
            referralCode.NbUtilisations++;
            referralCode.UsersReferred.Add(user.Uid);

            // Sauvegarder directement avec Firebase
            await _firebaseClient
                .Child("friendships")
                .Child(friendship.Id)
                .PutAsync(friendship);
                
            await _firebaseClient
                .Child("referral_codes")
                .Child(referralCode.Id)
                .PutAsync(referralCode);

            // Ajouter des points aux deux utilisateurs
            await AddPointsForReferralAsync(referralCode.UserId, user.Uid);

            // Créer l'activité
            await CreateActivityAsync(referralCode.UserId, ActivityType.MilestoneReached, 
                $"Nouveau parrainage réussi !", points: 50);

            return true;
        }

        /// <summary>
        /// Récupère le code de parrainage d'un utilisateur
        /// </summary>
        public async Task<ReferralCode?> GetUserReferralCodeAsync(string userId)
        {
            var codes = await _firebaseClient
                .Child("referral_codes")
                .OnceAsync<ReferralCode>();
            
            return codes.Select(item => 
            {
                var code = item.Object;
                code.Id = item.Key;
                return code;
            }).FirstOrDefault(c => c.UserId == userId && c.IsActive);
        }

        /// <summary>
        /// Trouve un code de parrainage par son code
        /// </summary>
        public async Task<ReferralCode?> GetReferralCodeByCodeAsync(string code)
        {
            var codes = await _firebaseClient
                .Child("referral_codes")
                .OnceAsync<ReferralCode>();
            
            return codes.Select(item => 
            {
                var referralCode = item.Object;
                referralCode.Id = item.Key;
                return referralCode;
            }).FirstOrDefault(c => c.Code == code && c.IsActive);
        }

        #endregion

        #region Gestion des amis

        /// <summary>
        /// Récupère la liste des amis d'un utilisateur
        /// </summary>
        public async Task<List<UserProfile>> GetFriendsAsync(string userId)
        {
            var friendships = await _firebaseClient
                .Child("friendships")
                .OnceAsync<Friendship>();
            
            var userFriendships = friendships
                .Select(item => 
                {
                    var friendship = item.Object;
                    friendship.Id = item.Key;
                    return friendship;
                })
                .Where(f => (f.UserId == userId || f.FriendId == userId) && 
                           f.Status == FriendshipStatus.Accepted)
                .ToList();

            var friends = new List<UserProfile>();
            foreach (var friendship in userFriendships)
            {
                var friendId = friendship.UserId == userId ? friendship.FriendId : friendship.UserId;
                try
                {
                    var friend = await _firebaseClient
                        .Child("UserProfiles")
                        .Child(friendId)
                        .OnceSingleAsync<UserProfile>();
                    
                    if (friend != null)
                    {
                        friend.Id = friendId;
                        friends.Add(friend);
                    }
                }
                catch
                {
                    // Ignorer les erreurs de récupération d'amis
                }
            }

            return friends;
        }

        /// <summary>
        /// Récupère l'activité des amis
        /// </summary>
        public async Task<List<FriendActivity>> GetFriendsActivityAsync(string userId, int limit = 20)
        {
            var friends = await GetFriendsAsync(userId);
            var friendIds = friends.Select(f => f.Id).ToList();

            var activities = await _firebaseClient
                .Child("friend_activities")
                .OnceAsync<FriendActivity>();
            
            return activities
                .Select(item => 
                {
                    var activity = item.Object;
                    activity.Id = item.Key;
                    return activity;
                })
                .Where(a => friendIds.Contains(a.UserId))
                .OrderByDescending(a => a.DateActivite)
                .Take(limit)
                .ToList();
        }

        /// <summary>
        /// Crée une activité pour un utilisateur
        /// </summary>
        public async Task CreateActivityAsync(string userId, ActivityType type, string description, 
            string? annonceId = null, string? annonceTitle = null, string? annoncePhotoUrl = null, int points = 0)
        {
            try
            {
                var user = await _firebaseClient
                    .Child("UserProfiles")
                    .Child(userId)
                    .OnceSingleAsync<UserProfile>();

                if (user == null) return;

                var activity = new FriendActivity
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    UserName = user.Name ?? "Utilisateur",
                    UserPhotoUrl = user.ProfilePictureUrl ?? "",
                    Type = type,
                    DateActivite = DateTime.UtcNow,
                    Description = description,
                    AnnonceId = annonceId,
                    AnnonceTitle = annonceTitle,
                    AnnoncePhotoUrl = annoncePhotoUrl,
                    Points = points
                };

                await _firebaseClient
                    .Child("friend_activities")
                    .Child(activity.Id)
                    .PutAsync(activity);
            }
            catch
            {
                // Ignorer les erreurs de création d'activité
            }
        }

        #endregion

        #region Partage sur réseaux sociaux

        /// <summary>
        /// Partage une annonce sur les réseaux sociaux
        /// </summary>
        public async Task<bool> ShareAnnonceAsync(Annonce annonce, SocialPlatform platform)
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return false;

            var shareUrl = GenerateShareUrl(annonce);
            var shareText = GenerateShareText(annonce);

            bool success = false;

            try
            {
                switch (platform)
                {
                    case SocialPlatform.WhatsApp:
                        success = await ShareToWhatsAppAsync(shareText, shareUrl);
                        break;
                    case SocialPlatform.Telegram:
                        success = await ShareToTelegramAsync(shareText, shareUrl);
                        break;
                    case SocialPlatform.Facebook:
                        success = await ShareToFacebookAsync(shareUrl);
                        break;
                    case SocialPlatform.Twitter:
                        success = await ShareToTwitterAsync(shareText, shareUrl);
                        break;
                    case SocialPlatform.Email:
                        success = await ShareByEmailAsync(shareText, shareUrl, annonce.Titre);
                        break;
                    case SocialPlatform.SMS:
                        success = await ShareBySMSAsync(shareText, shareUrl);
                        break;
                    default:
                        // Utiliser le partage natif par défaut
                        success = await ShareNativeAsync(shareText, shareUrl, annonce.Titre);
                        break;
                }

                if (success)
                {
                    // Enregistrer le partage
                    await RecordShareAsync(user.Uid, annonce.Id, platform, shareUrl);

                    // Créer l'activité
                    await CreateActivityAsync(user.Uid, ActivityType.MilestoneReached, 
                        $"A partagé une annonce", annonce.Id, annonce.Titre, annonce.FirstPhotoUrl, 10);

                    // Ajouter des points pour le partage
                    await AddPointsForShareAsync(user.Uid);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du partage: {ex.Message}");
                // En cas d'erreur, essayer le partage natif
                try
                {
                    success = await ShareNativeAsync(shareText, shareUrl, annonce.Titre);
                    if (success)
                    {
                        await RecordShareAsync(user.Uid, annonce.Id, platform, shareUrl);
                    }
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur partage fallback: {fallbackEx.Message}");
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Enregistre un partage dans Firebase
        /// </summary>
        private async Task RecordShareAsync(string userId, string annonceId, SocialPlatform platform, string shareUrl)
        {
            try
            {
                var share = new SocialShare
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    AnnonceId = annonceId,
                    Platform = platform,
                    DatePartage = DateTime.UtcNow,
                    ShareUrl = shareUrl
                };

                await _firebaseClient
                    .Child("social_shares")
                    .Child(share.Id)
                    .PutAsync(share);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur enregistrement partage: {ex.Message}");
                // Ne pas faire échouer le partage si l'enregistrement échoue
            }
        }

        /// <summary>
        /// Partage natif utilisant l'API Share de MAUI
        /// </summary>
        private async Task<bool> ShareNativeAsync(string text, string url, string title)
        {
            try
            {
                var shareRequest = new ShareTextRequest
                {
                    Text = $"{text}\n\n{url}",
                    Title = title
                };

                await Share.RequestAsync(shareRequest);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur partage natif: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Partage vers WhatsApp
        /// </summary>
        private async Task<bool> ShareToWhatsAppAsync(string text, string url)
        {
            try
            {
                var message = $"{text}\n\n{url}";
                var encoded = Uri.EscapeDataString(message);
                var whatsappUrl = $"https://wa.me/?text={encoded}";
                
                await Browser.OpenAsync(whatsappUrl, BrowserLaunchMode.External);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Partage vers Telegram
        /// </summary>
        private async Task<bool> ShareToTelegramAsync(string text, string url)
        {
            try
            {
                var message = $"{text}\n\n{url}";
                var encoded = Uri.EscapeDataString(message);
                var telegramUrl = $"https://t.me/share/url?url={Uri.EscapeDataString(url)}&text={Uri.EscapeDataString(text)}";
                
                await Browser.OpenAsync(telegramUrl, BrowserLaunchMode.External);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Partage vers Facebook
        /// </summary>
        private async Task<bool> ShareToFacebookAsync(string url)
        {
            try
            {
                var facebookUrl = $"https://www.facebook.com/sharer/sharer.php?u={Uri.EscapeDataString(url)}";
                await Browser.OpenAsync(facebookUrl, BrowserLaunchMode.External);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Partage vers Twitter
        /// </summary>
        private async Task<bool> ShareToTwitterAsync(string text, string url)
        {
            try
            {
                var twitterUrl = $"https://twitter.com/intent/tweet?text={Uri.EscapeDataString(text)}&url={Uri.EscapeDataString(url)}";
                await Browser.OpenAsync(twitterUrl, BrowserLaunchMode.External);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Partage par email
        /// </summary>
        private async Task<bool> ShareByEmailAsync(string text, string url, string subject)
        {
            try
            {
                var message = new EmailMessage
                {
                    Subject = $"Découvrez cette annonce sur DonTroc : {subject}",
                    Body = $"{text}\n\nLien : {url}\n\nTéléchargez DonTroc pour découvrir plus d'annonces !"
                };
                
                await Email.ComposeAsync(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Partage par SMS
        /// </summary>
        private async Task<bool> ShareBySMSAsync(string text, string url)
        {
            try
            {
                var messageText = $"{text}\n\n{url}";
                var message = new SmsMessage(messageText, new List<string>());
                
                await Sms.ComposeAsync(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Statistiques sociales

        /// <summary>
        /// Récupère les statistiques sociales d'un utilisateur
        /// </summary>
        public async Task<SocialStats> GetSocialStatsAsync(string userId)
        {
            var friendships = await _firebaseClient
                .Child("friendships")
                .OnceAsync<Friendship>();
            
            var referralCodes = await _firebaseClient
                .Child("referral_codes")
                .OnceAsync<ReferralCode>();
            
            var shares = await _firebaseClient
                .Child("social_shares")
                .OnceAsync<SocialShare>();
            
            var activities = await _firebaseClient
                .Child("friend_activities")
                .OnceAsync<FriendActivity>();

            var userFriends = friendships.Count(f => 
                (f.Object.UserId == userId || f.Object.FriendId == userId) && 
                f.Object.Status == FriendshipStatus.Accepted);

            var userReferrals = referralCodes.Where(r => r.Object.UserId == userId).Sum(r => r.Object.NbUtilisations);
            var userShares = shares.Count(s => s.Object.UserId == userId);
            var lastActivity = activities.Where(a => a.Object.UserId == userId)
                                       .OrderByDescending(a => a.Object.DateActivite)
                                       .FirstOrDefault()?.Object.DateActivite ?? DateTime.MinValue;

            return new SocialStats
            {
                UserId = userId,
                NombreAmis = userFriends,
                NombreParrainages = userReferrals,
                NombrePartages = userShares,
                PointsGagnesParrainage = userReferrals * 50, // 50 points par parrainage
                PointsGagnesPartage = userShares * 10, // 10 points par partage
                DerniereActivite = lastActivity
            };
        }

        #endregion

        #region Méthodes utilitaires privées

        /// <summary>
        /// Génère un code de parrainage unique
        /// </summary>
        private string GenerateUniqueCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Génère l'URL de partage pour une annonce
        /// </summary>
        private string GenerateShareUrl(Annonce annonce)
        {
            // URL profonde vers l'annonce dans l'app
            return $"https://dontroc.app/annonce/{annonce.Id}";
        }

        /// <summary>
        /// Génère le texte de partage pour une annonce
        /// </summary>
        private string GenerateShareText(Annonce annonce)
        {
            var typeText = annonce.Type == "Don" ? "🎁 Don gratuit" : "🔄 Troc";
            return $"{typeText} : {annonce.Titre}\n\n📝 {annonce.Description}\n🏷️ {annonce.Categorie}\n\n" +
                   $"Découvrez cette annonce et bien d'autres sur DonTroc ! 🌱♻️";
        }

        /// <summary>
        /// Ajoute des points pour un parrainage réussi
        /// </summary>
        private Task AddPointsForReferralAsync(string referrerId, string newUserId)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Ajoute des points pour un partage
        /// </summary>
        private Task AddPointsForShareAsync(string userId)
        {
            return Task.CompletedTask;
        }

        #endregion

        public async Task ShareAnnonceAsync(Annonce annonce)
        {
            await ShareAnnonceAsync(annonce, SocialPlatform.Native);
        }
    }
}

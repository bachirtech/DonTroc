// Fichier: DonTroc/Services/AdminService.cs
// Service centralisé pour la gestion administrative

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DonTroc.Models;

namespace DonTroc.Services
{
    /// <summary>
    /// Service centralisé pour toutes les opérations administratives
    /// </summary>
    public class AdminService
    {
        private readonly FirebaseService _firebaseService;
        private readonly AuthService _authService;
        private readonly NotificationService _notificationService;
        private readonly PushNotificationService _pushNotificationService;
        
        private UserProfile? _currentAdmin;

        public AdminService(
            FirebaseService firebaseService, 
            AuthService authService,
            NotificationService notificationService,
            PushNotificationService pushNotificationService)
        {
            _firebaseService = firebaseService;
            _authService = authService;
            _notificationService = notificationService;
            _pushNotificationService = pushNotificationService;
        }

        #region Vérification des permissions

        /// <summary>
        /// Charge le profil admin courant
        /// </summary>
        public async Task<UserProfile?> GetCurrentAdminAsync()
        {
            if (_currentAdmin != null) return _currentAdmin;

            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return null;

            _currentAdmin = await _firebaseService.GetUserProfileAsync(user.Uid);
            return _currentAdmin;
        }

        /// <summary>
        /// Vérifie si l'utilisateur courant est admin
        /// </summary>
        public async Task<bool> IsCurrentUserAdminAsync()
        {
            var profile = await GetCurrentAdminAsync();
            return profile?.IsAdmin ?? false;
        }

        /// <summary>
        /// Vérifie si l'utilisateur courant est modérateur ou admin
        /// </summary>
        public async Task<bool> IsCurrentUserModeratorAsync()
        {
            var profile = await GetCurrentAdminAsync();
            return profile?.IsModerator ?? false;
        }

        /// <summary>
        /// Vérifie si l'utilisateur courant peut accéder au panneau admin
        /// </summary>
        public async Task<bool> CanAccessAdminPanelAsync()
        {
            var profile = await GetCurrentAdminAsync();
            return profile?.CanAccessAdminPanel ?? false;
        }

        /// <summary>
        /// Invalide le cache du profil admin
        /// </summary>
        public void InvalidateAdminCache()
        {
            _currentAdmin = null;
        }

        #endregion

        #region Statistiques

        /// <summary>
        /// Récupère les statistiques globales pour le dashboard admin
        /// </summary>
        public async Task<AdminStats> GetAdminStatsAsync()
        {
            var stats = new AdminStats();

            try
            {
                // Récupérer tous les utilisateurs
                var users = await GetAllUsersAsync();
                stats.TotalUsers = users.Count;
                stats.SuspendedUsers = users.Count(u => u.IsSuspended);
                stats.ModeratorCount = users.Count(u => u.Role == UserRole.Moderator);
                stats.AdminCount = users.Count(u => u.Role == UserRole.Admin);
                
                var today = DateTime.UtcNow.Date;
                stats.NewUsersToday = users.Count(u => u.DateInscription.Date == today);
                
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                stats.ActiveUsers = users.Count(u => u.LastLocationUpdate.HasValue && u.LastLocationUpdate.Value >= thirtyDaysAgo);

                // Récupérer toutes les annonces
                var annonces = await _firebaseService.GetAnnoncesAsync();
                stats.TotalAnnonces = annonces.Count;
                stats.ActiveAnnonces = annonces.Count(a => a.Statut == StatutAnnonce.Disponible);
                stats.NewAnnoncesToday = annonces.Count(a => a.DateCreation.Date == today);

                // Récupérer tous les signalements
                var reports = await GetAllReportsAsync();
                stats.TotalReports = reports.Count;
                stats.PendingReports = reports.Count(r => r.Status == "Pending");
                stats.ResolvedReports = reports.Count(r => r.Status == "Resolved" || r.Status == "ActionTaken");

                stats.LastUpdated = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur GetAdminStatsAsync: {ex.Message}");
            }

            return stats;
        }

        #endregion

        #region Gestion des utilisateurs

        /// <summary>
        /// Récupère tous les utilisateurs
        /// </summary>
        public async Task<List<UserProfile>> GetAllUsersAsync()
        {
            try
            {
                var usersData = await _firebaseService.GetData<UserProfile>("UserProfiles");
                if (usersData != null && usersData.Count > 0)
                {
                    Debug.WriteLine($"[AdminService] GetAllUsersAsync: {usersData.Count} utilisateurs trouvés");
                    
                    // Ajouter l'ID à chaque profil (la clé du dictionnaire)
                    foreach (var kvp in usersData)
                    {
                        if (kvp.Value != null && string.IsNullOrEmpty(kvp.Value.Id))
                        {
                            kvp.Value.Id = kvp.Key;
                        }
                    }
                    
                    return usersData.Values.Where(u => u != null).ToList();
                }
                
                Debug.WriteLine("[AdminService] GetAllUsersAsync: Aucun utilisateur trouvé");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur GetAllUsersAsync: {ex.Message}");
            }
            return new List<UserProfile>();
        }

        /// <summary>
        /// Recherche des utilisateurs avec filtres
        /// </summary>
        public async Task<List<UserProfile>> SearchUsersAsync(UserSearchFilter filter)
        {
            var users = await GetAllUsersAsync();

            // Appliquer les filtres
            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                var query = filter.SearchQuery.ToLower();
                users = users.Where(u => 
                    (u.Name?.ToLower().Contains(query) ?? false) ||
                    (u.Email?.ToLower().Contains(query) ?? false) ||
                    (u.Id?.ToLower().Contains(query) ?? false)
                ).ToList();
            }

            if (filter.Role.HasValue)
                users = users.Where(u => u.Role == filter.Role.Value).ToList();

            if (filter.IsSuspended.HasValue)
                users = users.Where(u => u.IsSuspended == filter.IsSuspended.Value).ToList();

            if (filter.RegisteredAfter.HasValue)
                users = users.Where(u => u.DateInscription >= filter.RegisteredAfter.Value).ToList();

            if (filter.RegisteredBefore.HasValue)
                users = users.Where(u => u.DateInscription <= filter.RegisteredBefore.Value).ToList();

            // Pagination
            return users
                .OrderByDescending(u => u.DateInscription)
                .Skip(filter.Skip)
                .Take(filter.Take)
                .ToList();
        }

        /// <summary>
        /// Suspend un utilisateur
        /// </summary>
        public async Task<bool> SuspendUserAsync(string userId, string reason, DateTime? endDate = null)
        {
            if (!await IsCurrentUserModeratorAsync()) return false;

            try
            {
                var admin = await GetCurrentAdminAsync();
                
                var updates = new Dictionary<string, object>
                {
                    { "IsSuspended", true },
                    { "SuspensionReason", reason },
                    { "SuspensionDate", DateTime.UtcNow },
                    { "SuspendedBy", admin?.Id ?? "unknown" }
                };

                if (endDate.HasValue)
                    updates["SuspensionEndDate"] = endDate.Value;

                await _firebaseService.UpdateData($"UserProfiles/{userId}", updates);

                // Enregistrer le log
                await LogAdminActionAsync(AdminActionType.UserSuspended, userId, "User", $"Raison: {reason}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur SuspendUserAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lève la suspension d'un utilisateur
        /// </summary>
        public async Task<bool> UnsuspendUserAsync(string userId)
        {
            if (!await IsCurrentUserModeratorAsync()) return false;

            try
            {
                var updates = new Dictionary<string, object>
                {
                    { "IsSuspended", false },
                    { "SuspensionReason", null! },
                    { "SuspensionDate", null! },
                    { "SuspensionEndDate", null! },
                    { "SuspendedBy", null! }
                };

                await _firebaseService.UpdateData($"UserProfiles/{userId}", updates);
                await LogAdminActionAsync(AdminActionType.UserUnsuspended, userId, "User");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur UnsuspendUserAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Change le rôle d'un utilisateur
        /// </summary>
        public async Task<bool> ChangeUserRoleAsync(string userId, UserRole newRole)
        {
            // Seuls les admins peuvent changer les rôles
            if (!await IsCurrentUserAdminAsync()) return false;

            try
            {
                var updates = new Dictionary<string, object> { { "Role", (int)newRole } };
                await _firebaseService.UpdateData($"UserProfiles/{userId}", updates);

                var actionType = newRole switch
                {
                    UserRole.Admin => AdminActionType.UserPromotedToAdmin,
                    UserRole.Moderator => AdminActionType.UserPromotedToModerator,
                    _ => AdminActionType.UserDemoted
                };

                await LogAdminActionAsync(actionType, userId, "User", $"Nouveau rôle: {newRole}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur ChangeUserRoleAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Supprime un compte utilisateur
        /// </summary>
        public async Task<bool> DeleteUserAsync(string userId)
        {
            if (!await IsCurrentUserAdminAsync()) return false;

            try
            {
                // Supprimer les annonces de l'utilisateur
                var annonces = await _firebaseService.GetAnnoncesAsync();
                foreach (var annonce in annonces.Where(a => a.UtilisateurId == userId))
                {
                    await _firebaseService.DeleteAnnonceAsync(annonce.Id);
                }
                
                // Supprimer le profil (en le marquant comme supprimé)
                var updates = new Dictionary<string, object>
                {
                    { "IsDeleted", true },
                    { "DeletedAt", DateTime.UtcNow }
                };
                await _firebaseService.UpdateData($"UserProfiles/{userId}", updates);

                await LogAdminActionAsync(AdminActionType.UserDeleted, userId, "User");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur DeleteUserAsync: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Gestion des annonces

        /// <summary>
        /// Supprime une annonce
        /// </summary>
        public async Task<bool> DeleteAnnonceAsync(string annonceId, string reason)
        {
            if (!await IsCurrentUserModeratorAsync()) return false;

            try
            {
                await _firebaseService.DeleteAnnonceAsync(annonceId);
                await LogAdminActionAsync(AdminActionType.AnnonceDeleted, annonceId, "Annonce", reason);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur DeleteAnnonceAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Marque une annonce comme inappropriée
        /// </summary>
        public async Task<bool> FlagAnnonceAsync(string annonceId, string reason)
        {
            if (!await IsCurrentUserModeratorAsync()) return false;

            try
            {
                var updates = new Dictionary<string, object>
                {
                    { "IsFlagged", true },
                    { "FlagReason", reason },
                    { "FlaggedAt", DateTime.UtcNow }
                };
                
                await _firebaseService.UpdateData($"Annonces/{annonceId}", updates);
                await LogAdminActionAsync(AdminActionType.AnnonceFlagged, annonceId, "Annonce", reason);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur FlagAnnonceAsync: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Gestion des signalements

        /// <summary>
        /// Récupère tous les signalements
        /// </summary>
        public async Task<List<Report>> GetAllReportsAsync()
        {
            try
            {
                var reportsData = await _firebaseService.GetData<Report>("reports");
                if (reportsData != null && reportsData.Count > 0)
                {
                    return reportsData.Values.Where(r => r != null).OrderByDescending(r => r.Timestamp).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur GetAllReportsAsync: {ex.Message}");
            }
            return new List<Report>();
        }

        /// <summary>
        /// Recherche des signalements avec filtres
        /// </summary>
        public async Task<List<Report>> SearchReportsAsync(ReportSearchFilter filter)
        {
            var reports = await GetAllReportsAsync();

            if (!string.IsNullOrEmpty(filter.Status))
                reports = reports.Where(r => r.Status == filter.Status).ToList();

            if (!string.IsNullOrEmpty(filter.ReportType))
                reports = reports.Where(r => r.ReportedItemType == filter.ReportType).ToList();

            if (filter.DateFrom.HasValue)
                reports = reports.Where(r => r.Timestamp >= filter.DateFrom.Value).ToList();

            if (filter.DateTo.HasValue)
                reports = reports.Where(r => r.Timestamp <= filter.DateTo.Value).ToList();

            return reports
                .OrderByDescending(r => r.Timestamp)
                .Skip(filter.Skip)
                .Take(filter.Take)
                .ToList();
        }

        /// <summary>
        /// Met à jour le statut d'un signalement
        /// </summary>
        public async Task<bool> UpdateReportStatusAsync(string reportId, string status, string? notes = null)
        {
            if (!await IsCurrentUserModeratorAsync()) return false;

            try
            {
                var admin = await GetCurrentAdminAsync();
                var updates = new Dictionary<string, object>
                {
                    { "Status", status },
                    { "ReviewedBy", admin?.Id ?? "unknown" },
                    { "ReviewedAt", DateTime.UtcNow }
                };

                if (!string.IsNullOrEmpty(notes))
                    updates["Notes"] = notes;

                await _firebaseService.UpdateData($"reports/{reportId}", updates);

                var actionType = status switch
                {
                    "Resolved" or "ActionTaken" => AdminActionType.ReportResolved,
                    "Dismissed" => AdminActionType.ReportDismissed,
                    _ => AdminActionType.ReportReviewed
                };

                await LogAdminActionAsync(actionType, reportId, "Report", notes);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur UpdateReportStatusAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Action en masse sur plusieurs signalements
        /// </summary>
        public async Task<int> BulkUpdateReportsAsync(List<string> reportIds, string status, string? notes = null)
        {
            if (!await IsCurrentUserModeratorAsync()) return 0;

            int successCount = 0;
            foreach (var reportId in reportIds)
            {
                if (await UpdateReportStatusAsync(reportId, status, notes))
                    successCount++;
            }

            if (successCount > 0)
            {
                await LogAdminActionAsync(
                    AdminActionType.BulkAction, 
                    string.Join(",", reportIds), 
                    "Reports", 
                    $"Mise à jour de {successCount} signalements vers '{status}'"
                );
            }

            return successCount;
        }

        #endregion

        #region Logs d'administration

        /// <summary>
        /// Enregistre une action administrative dans le journal
        /// </summary>
        public async Task LogAdminActionAsync(AdminActionType actionType, string? targetId, string? targetType, string? details = null)
        {
            try
            {
                var admin = await GetCurrentAdminAsync();
                var log = new AdminLog
                {
                    ActionType = actionType,
                    AdminId = admin?.Id ?? "unknown",
                    AdminName = admin?.Name ?? "Unknown Admin",
                    TargetId = targetId,
                    TargetType = targetType,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                await _firebaseService.SaveDataAsync($"admin_logs/{log.Id}", log);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur LogAdminActionAsync: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère les logs d'administration
        /// </summary>
        public async Task<List<AdminLog>> GetAdminLogsAsync(int limit = 100)
        {
            try
            {
                var logsData = await _firebaseService.GetData<AdminLog>("admin_logs");
                if (logsData != null && logsData.Count > 0)
                {
                    return logsData.Values
                        .Where(l => l != null)
                        .OrderByDescending(l => l.Timestamp)
                        .Take(limit)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur GetAdminLogsAsync: {ex.Message}");
            }
            return new List<AdminLog>();
        }

        /// <summary>
        /// Récupère les logs d'un admin spécifique
        /// </summary>
        public async Task<List<AdminLog>> GetAdminLogsByAdminIdAsync(string adminId, int limit = 50)
        {
            var allLogs = await GetAdminLogsAsync(500);
            return allLogs
                .Where(l => l.AdminId == adminId)
                .Take(limit)
                .ToList();
        }

        #endregion

        #region Utilitaires

        /// <summary>
        /// Initialise le premier admin (à appeler une seule fois)
        /// </summary>
        public async Task<bool> InitializeFirstAdminAsync(string userId)
        {
            try
            {
                // Vérifier qu'aucun admin n'existe
                var users = await GetAllUsersAsync();
                if (users.Any(u => u.Role == UserRole.Admin))
                {
                    Debug.WriteLine("[AdminService] Un admin existe déjà");
                    return false;
                }

                // Promouvoir l'utilisateur
                var updates = new Dictionary<string, object> { { "Role", (int)UserRole.Admin } };
                await _firebaseService.UpdateData($"UserProfiles/{userId}", updates);

                Debug.WriteLine($"[AdminService] Premier admin créé: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur InitializeFirstAdminAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Promouvoir un utilisateur en admin (la validation de la clé secrète est faite côté UI)
        /// </summary>
        public async Task<bool> PromoteToAdminWithKeyAsync(string userId)
        {
            try
            {
                // Vérifier que le profil existe
                var profile = await _firebaseService.GetUserProfileAsync(userId);
                if (profile == null)
                {
                    Debug.WriteLine("[AdminService] Profil non trouvé pour la promotion");
                    return false;
                }

                // Promouvoir l'utilisateur en admin
                var updates = new Dictionary<string, object> { { "Role", (int)UserRole.Admin } };
                await _firebaseService.UpdateData($"UserProfiles/{userId}", updates);

                Debug.WriteLine($"[AdminService] Utilisateur promu admin: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur PromoteToAdminWithKeyAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Crée un profil admin complet pour un utilisateur Firebase Auth existant
        /// À utiliser si le UserProfile n'existe pas encore
        /// </summary>
        public async Task<bool> CreateAdminProfileAsync(string userId, string name, string email)
        {
            try
            {
                var profile = new UserProfile
                {
                    Id = userId,
                    Name = name,
                    Email = email,
                    Role = UserRole.Admin,
                    DateInscription = DateTime.UtcNow
                };

                await _firebaseService.SaveDataAsync($"UserProfiles/{userId}", profile);

                Debug.WriteLine($"[AdminService] Profil admin créé: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminService] Erreur CreateAdminProfileAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Vérifie si un UserProfile existe pour un UID donné
        /// </summary>
        public async Task<bool> UserProfileExistsAsync(string userId)
        {
            try
            {
                var profile = await _firebaseService.GetUserProfileAsync(userId);
                return profile != null;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}


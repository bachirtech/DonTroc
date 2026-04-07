using DonTroc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DonTroc.Services
{
    public class ReportService
    {
        private readonly FirebaseService _firebaseService;
        private readonly NotificationService _notificationService;
        private readonly PushNotificationService _pushNotificationService;

        public ReportService(FirebaseService firebaseService, NotificationService notificationService, PushNotificationService pushNotificationService)
        {
            _firebaseService = firebaseService;
            _notificationService = notificationService;
            _pushNotificationService = pushNotificationService;
        }

        public async Task<bool> CreateReport(Report report)
        {
            try
            {
                report.Id = Guid.NewGuid().ToString();
                report.Timestamp = DateTime.UtcNow;
                report.Status = "Pending";
                await _firebaseService.SaveData($"reports/{report.Id}", report);
                
                // Envoyer une notification aux administrateurs
                await NotifyAdministrators(report);
                
                return true;
            }
            catch (Exception )
            {
                // Log exception
                return false;
            }
        }

        /// <summary>
        /// Envoie une notification à tous les administrateurs lors d'un nouveau signalement.
        /// </summary>
        private async Task NotifyAdministrators(Report report)
        {
            try
            {
                // Récupérer les informations de l'utilisateur qui signale
                var reporterProfile = await _firebaseService.GetUserProfileAsync(report.ReporterId);
                var reporterName = reporterProfile?.DisplayName ?? "Un utilisateur";
                
                // Récupérer tous les utilisateurs admin depuis UserProfiles
                var usersData = await _firebaseService.GetData<UserProfile>("UserProfiles");
                
                if (usersData != null)
                {
                    var admins = usersData.Values.Where(u => u != null && u.Role == UserRole.Admin).ToList();
                    
                    foreach (var admin in admins)
                    {
                        // Envoyer une notification push via FCM si l'admin a un token FCM
                        if (!string.IsNullOrEmpty(admin.FcmToken))
                        {
                            await _pushNotificationService.SendReportNotificationToAdminAsync(
                                admin.FcmToken, 
                                reporterName, 
                                report.Reason, 
                                report.Id
                            );
                        }
                    }
                }
                
                // Afficher aussi une notification locale si l'utilisateur actuel est admin
                await _notificationService.ShowReportNotificationAsync(reporterName, report.Reason, report.Id);
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas faire échouer le signalement
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la notification des administrateurs: {ex.Message}");
            }
        }

        public async Task<List<Report>> GetAllReports()
        {
            try
            {
                var reportsData = await _firebaseService.GetData<Report>("reports");
                
                if (reportsData == null || reportsData.Count == 0)
                    return new List<Report>();
                
                return reportsData.Values.Where(r => r != null).ToList();
            }
            catch (Exception )
            {
                // Log exception
                return new List<Report>();
            }
        }

        public async Task<bool> UpdateReportStatus(string reportId, string status, string notes)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    { "Status", status },
                    { "Notes", notes }
                };
                await _firebaseService.UpdateData($"reports/{reportId}", updates);
                return true;
            }
            catch (Exception )
            {
                // Log exception
                return false;
            }
        }
    }
}

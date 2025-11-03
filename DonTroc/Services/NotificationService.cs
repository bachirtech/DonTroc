using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

#if ANDROID
using AndroidX.Core.App;
using Android.App;
using Android.Content;
using Android.Graphics;
using AndroidX.Core.Content;
using Android.Content.PM;
#endif

#if IOS
using UserNotifications;
using Foundation;
#endif

namespace DonTroc.Services
{
    /// <summary>
    /// Service pour envoyer des notifications locales natives.
    /// </summary>
    public class NotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private const string CHANNEL_ID = "dontroc_messages";
        private const string CHANNEL_NAME = "Messages DonTroc";

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
            _ = InitializeNotificationChannelAsync();
        }

        /// <summary>
        /// Initialise le canal de notification (Android) ou demande les permissions (iOS).
        /// </summary>
        private async Task InitializeNotificationChannelAsync()
        {
#if ANDROID
            CreateNotificationChannel();
            await Task.CompletedTask; // Pour éviter le warning CS1998 sur Android
#elif IOS
            await RequestNotificationPermissionAsync();
#endif
        }

#if ANDROID
        /// <summary>
        /// Crée le canal de notification pour Android.
        /// </summary>
        private void CreateNotificationChannel()
        {
            try
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var notificationManager = NotificationManagerCompat.From(context);

                var channel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.Default)
                {
                    Description = "Notifications pour les nouveaux messages DonTroc"
                };

                notificationManager.CreateNotificationChannel(channel);
                _logger.LogInformation("Canal de notification Android créé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du canal de notification Android");
            }
        }
#endif

#if IOS
        /// <summary>
        /// Demande la permission pour les notifications sur iOS.
        /// </summary>
        public async Task RequestNotificationPermissionAsync()
        {
            try
            {
                var center = UNUserNotificationCenter.Current;
                var result = await center.RequestAuthorizationAsync(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge);
                
                if (result.Item1)
                {
                    _logger.LogInformation("Permission de notification iOS accordée");
                }
                else
                {
                    _logger.LogWarning("Permission de notification iOS refusée");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la demande de permission iOS");
            }
        }
#else
        public Task RequestNotificationPermissionAsync()
        {
            // Pas d'implémentation nécessaire pour les autres plateformes ici
            return Task.CompletedTask;
        }
#endif

        /// <summary>
        /// Affiche une notification pour un nouveau message.
        /// </summary>
        /// <param name="senderName">Nom de l'expéditeur</param>
        /// <param name="messageText">Texte du message</param>
        /// <param name="conversationId">ID de la conversation</param>
        public async Task ShowMessageNotificationAsync(string senderName, string messageText, string conversationId)
        {
            try
            {
                var title = $"Nouveau message de {senderName}";
                var content = messageText.Length > 100 ? $"{messageText.Substring(0, 100)}..." : messageText;

#if ANDROID
                await ShowAndroidNotification(title, content, conversationId);
#elif IOS
                await ShowiOSNotification(title, content, conversationId);
#else
                // Fallback pour les autres plateformes (log)
                _logger.LogInformation($"📱 Notification: {title} - {content}");
                await Task.CompletedTask;
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage de la notification");
                // Fallback en cas d'erreur
                _logger.LogInformation($"📱 Notification (fallback): Nouveau message de {senderName}");
            }
        }

#if ANDROID
        /// <summary>
        /// Affiche une notification native Android.
        /// </summary>
        private Task ShowAndroidNotification(string title, string content, string conversationId)
        {
            try
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                
                // Intent pour ouvrir la conversation quand on clique sur la notification
                var intent = new Intent(context, typeof(MainActivity));
                intent.PutExtra("conversationId", conversationId);
                intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                
                var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                var notification = new NotificationCompat.Builder(context, CHANNEL_ID)
                    .SetContentTitle(title)
                    .SetContentText(content)
                    .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo) // Icône par défaut
                    .SetContentIntent(pendingIntent)
                    .SetAutoCancel(true)
                    .SetPriority(NotificationCompat.PriorityDefault)
                    .Build();

                var notificationManager = NotificationManagerCompat.From(context);
                
                // Vérifier les permissions de notification
                if (ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.PostNotifications) == Permission.Granted)
                {
                    notificationManager.Notify(DateTime.Now.Millisecond, notification);
                    _logger.LogInformation("Notification Android affichée avec succès");
                }
                else
                {
                    _logger.LogWarning("Permission de notification Android manquante");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage de la notification Android");
            }
            
            return Task.CompletedTask;
        }
#endif

#if IOS
        /// <summary>
        /// Affiche une notification native iOS.
        /// </summary>
        private async Task ShowiOSNotification(string title, string content, string conversationId)
        {
            try
            {
                var center = UNUserNotificationCenter.Current;
                
                var notificationContent = new UNMutableNotificationContent()
                {
                    Title = title,
                    Body = content,
                    Sound = UNNotificationSound.Default,
                    UserInfo = NSDictionary.FromObjectAndKey(NSString.FromData(conversationId, NSStringEncoding.UTF8), new NSString("conversationId"))
                };

                var request = UNNotificationRequest.FromIdentifier(
                    Guid.NewGuid().ToString(),
                    notificationContent,
                    UNTimeIntervalNotificationTrigger.CreateTrigger(1, false)
                );

                await center.AddNotificationRequestAsync(request);
                _logger.LogInformation("Notification iOS programmée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage de la notification iOS");
            }
        }
#endif

        /// <summary>
        /// Affiche une notification pour un signalement d'annonce (pour les administrateurs).
        /// </summary>
        /// <param name="reporterName">Nom de l'utilisateur qui signale</param>
        /// <param name="reason">Raison du signalement</param>
        /// <param name="reportId">ID du signalement</param>
        public async Task ShowReportNotificationAsync(string reporterName, string reason, string reportId)
        {
            try
            {
                var title = "🚨 Nouveau signalement";
                var content = $"{reporterName} a signalé une annonce : {reason}";

#if ANDROID
                await ShowAndroidReportNotification(title, content, reportId);
#elif IOS
                await ShowiOSReportNotification(title, content, reportId);
#else
                // Fallback pour les autres plateformes (log)
                _logger.LogInformation($"📱 Notification: {title} - {content}");
                await Task.CompletedTask;
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage de la notification de signalement");
            }
        }

#if ANDROID
        /// <summary>
        /// Affiche une notification native Android pour un signalement.
        /// </summary>
        private Task ShowAndroidReportNotification(string title, string content, string reportId)
        {
            try
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                
                // Intent pour ouvrir l'application quand on clique sur la notification
                var intent = new Intent(context, context.GetType());
                intent.PutExtra("reportId", reportId);
                intent.PutExtra("openModeration", true); // Flag pour ouvrir la page de modération
                intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                
                var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                var notification = new NotificationCompat.Builder(context, CHANNEL_ID)
                    .SetContentTitle(title)
                    .SetContentText(content)
                    .SetSmallIcon(Android.Resource.Drawable.IcDialogAlert) // Icône d'alerte pour les signalements
                    .SetContentIntent(pendingIntent)
                    .SetAutoCancel(true)
                    .SetPriority(NotificationCompat.PriorityHigh) // Priorité élevée pour les signalements
                    .Build();

                var notificationManager = NotificationManagerCompat.From(context);
                
                // Vérifier les permissions de notification
                if (ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.PostNotifications) == Permission.Granted)
                {
                    notificationManager.Notify(DateTime.Now.Millisecond, notification);
                    _logger.LogInformation("Notification de signalement Android affichée avec succès");
                }
                else
                {
                    _logger.LogWarning("Permission de notification Android manquante");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage de la notification Android de signalement");
            }

            return Task.CompletedTask;
        }
#endif

#if IOS
        /// <summary>
        /// Affiche une notification native iOS pour un signalement.
        /// </summary>
        private async Task ShowiOSReportNotification(string title, string content, string reportId)
        {
            try
            {
                var center = UNUserNotificationCenter.Current;
                
                var notificationContent = new UNMutableNotificationContent()
                {
                    Title = title,
                    Body = content,
                    Sound = UNNotificationSound.Default,
                    UserInfo = NSDictionary.FromObjectAndKey(NSString.FromData(reportId, NSStringEncoding.UTF8), new NSString("reportId"))
                };

                var request = UNNotificationRequest.FromIdentifier(
                    Guid.NewGuid().ToString(),
                    notificationContent,
                    UNTimeIntervalNotificationTrigger.CreateTrigger(1, false)
                );

                await center.AddNotificationRequestAsync(request);
                _logger.LogInformation("Notification de signalement iOS programmée avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage de la notification de signalement iOS");
            }
        }
#endif

        /// <summary>
        /// Vérifie si les notifications sont activées.
        /// </summary>
        /// <returns>True si les notifications sont activées</returns>
        public async Task<bool> AreNotificationsEnabledAsync()
        {
            try
            {
#if ANDROID
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var notificationManager = NotificationManagerCompat.From(context);
                return notificationManager.AreNotificationsEnabled();
#elif IOS
                var center = UNUserNotificationCenter.Current;
                var settings = await center.GetNotificationSettingsAsync();
                return settings.AuthorizationStatus == UNAuthorizationStatus.Authorized;
#else
                return true; // Fallback pour autres plateformes
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du statut des notifications");
                return false;
            }
        }

        /// <summary>
        /// Ouvre les paramètres de notification de l'application.
        /// </summary>
        public async Task OpenNotificationSettingsAsync()
        {
            try
            {
#if ANDROID
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var intent = new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
                var uri = Android.Net.Uri.FromParts("package", context.PackageName, null);
                intent.SetData(uri);
                intent.SetFlags(ActivityFlags.NewTask);
                context.StartActivity(intent);
                await Task.CompletedTask; // Pour éviter le warning CS1998 sur Android
#elif IOS
                await Launcher.OpenAsync(UIKit.UIApplication.OpenSettingsUrlString);
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ouverture des paramètres de notification");
            }
        }
    }
}

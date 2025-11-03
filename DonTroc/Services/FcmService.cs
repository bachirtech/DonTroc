#if ANDROID
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Firebase.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Firebase.CloudMessaging;

namespace DonTroc.Services;

public class FcmService : IFirebaseCloudMessagingDelegate
{
    private readonly IServiceProvider _serviceProvider;

    public FcmService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task OnTokenChanged(string fcmToken)
    {
        Debug.WriteLine($"FCM token changed: {fcmToken}");
        return SendRegistrationToServer(fcmToken);
    }

    public Task OnMessageReceived(RemoteMessage remoteMessage)
    {
        Debug.WriteLine("Message received");

        // Afficher la notification localement si l'application est au premier plan
        var notificationService = _serviceProvider.GetRequiredService<NotificationService>();
        
        var conversationId = remoteMessage.Data.ContainsKey("conversationId") ? remoteMessage.Data["conversationId"] : string.Empty;

        return notificationService.ShowMessageNotificationAsync(
            remoteMessage.GetNotification()?.Title ?? "Nouveau message",
            remoteMessage.GetNotification()?.Body ?? string.Empty,
            conversationId);
    }

    private async Task SendRegistrationToServer(string token)
    {
        try
        {
            // Utiliser IServiceProvider pour obtenir une instance de AuthService et FirebaseService
            // pour éviter les dépendances circulaires au démarrage.
            using var scope = _serviceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
            var firebaseService = scope.ServiceProvider.GetRequiredService<FirebaseService>();

            var userId = authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var userProfile = await firebaseService.GetUserProfileAsync(userId);
                if (userProfile != null)
                {
                    userProfile.FcmToken = token;
                    await firebaseService.SaveUserProfileAsync(userProfile);
                    Debug.WriteLine("FCM token saved to user profile.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving FCM token: {ex.Message}");
        }
    }
}

public interface IFirebaseCloudMessagingDelegate
{
    Task OnTokenChanged(string fcmToken);
    Task OnMessageReceived(RemoteMessage remoteMessage);
}
#endif

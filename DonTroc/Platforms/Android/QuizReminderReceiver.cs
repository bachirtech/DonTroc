using Android.App;
using Android.Content;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Android.Content.PM;

namespace DonTroc.Platforms.Android;

/// <summary>
/// BroadcastReceiver pour recevoir les alarmes de rappel du quiz quotidien.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { "com.dontroc.QUIZ_REMINDER" })]
public class QuizReminderReceiver : BroadcastReceiver
{
    private const string QUIZ_CHANNEL_ID = "dontroc_quiz";
    private const int QUIZ_NOTIFICATION_ID = 9001;

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        try
        {
            // Vérifier si l'utilisateur peut encore jouer au quiz aujourd'hui
            var lastPlayDate = Preferences.Get("last_daily_quiz_date", string.Empty);
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            
            // Si déjà joué aujourd'hui, ne pas envoyer la notification
            if (lastPlayDate == today)
            {
                System.Diagnostics.Debug.WriteLine("Quiz déjà joué aujourd'hui, notification ignorée");
                return;
            }

            ShowQuizNotification(context);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur dans QuizReminderReceiver: {ex.Message}");
        }
    }

    private void ShowQuizNotification(Context context)
    {
        try
        {
            // S'assurer que le canal existe
            EnsureNotificationChannel(context);

            // Intent pour ouvrir l'application quand on clique sur la notification
            var intent = new Intent(context, typeof(MainActivity));
            intent.PutExtra("openQuiz", true);
            intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

            var pendingIntent = PendingIntent.GetActivity(
                context, 
                QUIZ_NOTIFICATION_ID, 
                intent, 
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var notification = new NotificationCompat.Builder(context, QUIZ_CHANNEL_ID)
                .SetContentTitle("🎯 Défi Quiz du Jour !")
                .SetContentText("Votre quiz quotidien vous attend ! Testez vos connaissances et gagnez des points.")
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true)
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetDefaults((int)NotificationDefaults.All)
                .SetStyle(new NotificationCompat.BigTextStyle()
                    .BigText("Votre quiz quotidien vous attend ! Testez vos connaissances sur le troc et l'échange, gagnez des points et débloquez des récompenses. 🏆"))
                .Build();

            var notificationManager = NotificationManagerCompat.From(context);

            // Vérifier les permissions de notification (Android 13+)
            if (ContextCompat.CheckSelfPermission(context, global::Android.Manifest.Permission.PostNotifications) == Permission.Granted)
            {
                notificationManager.Notify(QUIZ_NOTIFICATION_ID, notification);
                System.Diagnostics.Debug.WriteLine("Notification Quiz affichée avec succès");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Permission de notification manquante");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors de l'affichage de la notification: {ex.Message}");
        }
    }

    private void EnsureNotificationChannel(Context context)
    {
        try
        {
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                var notificationManager = NotificationManagerCompat.From(context);
                
                var existingChannel = notificationManager.GetNotificationChannel(QUIZ_CHANNEL_ID);
                if (existingChannel == null)
                {
                    var channel = new NotificationChannel(
                        QUIZ_CHANNEL_ID, 
                        "Défis Quiz DonTroc", 
                        NotificationImportance.High)
                    {
                        Description = "Notifications pour les défis quiz quotidiens"
                    };

                    notificationManager.CreateNotificationChannel(channel);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la création du canal: {ex.Message}");
        }
    }
}

/// <summary>
/// BroadcastReceiver pour redémarrer les alarmes après un redémarrage de l'appareil.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted })]
public class BootCompletedReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        if (intent.Action == Intent.ActionBootCompleted)
        {
            try
            {
                // Vérifier si les rappels quiz sont activés
                var reminderEnabled = Preferences.Get("quiz_reminder_enabled", false);
                if (!reminderEnabled) return;

                var hour = Preferences.Get("quiz_reminder_hour", 10);
                var minute = Preferences.Get("quiz_reminder_minute", 0);

                // Replanifier l'alarme
                RescheduleQuizReminder(context, hour, minute);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans BootCompletedReceiver: {ex.Message}");
            }
        }
    }

    private void RescheduleQuizReminder(Context context, int hour, int minute)
    {
        try
        {
            var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
            if (alarmManager == null) return;

            var intent = new Intent(context, typeof(QuizReminderReceiver));
            intent.SetAction("com.dontroc.QUIZ_REMINDER");

            var pendingIntent = PendingIntent.GetBroadcast(
                context, 
                9001, 
                intent, 
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            // Calculer le temps jusqu'à la prochaine occurrence
            var now = DateTime.Now;
            var scheduledTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);

            if (scheduledTime <= now)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }

            var triggerTime = (long)(scheduledTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

            // Utiliser SetInexactRepeating pour éviter USE_EXACT_ALARM
            alarmManager.SetInexactRepeating(
                AlarmType.RtcWakeup,
                triggerTime,
                AlarmManager.IntervalDay,
                pendingIntent);

            System.Diagnostics.Debug.WriteLine($"Rappel Quiz replanifié pour {hour}:{minute:D2} (inexact)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la replanification: {ex.Message}");
        }
    }
}


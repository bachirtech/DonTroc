using System;
using System.Threading.Tasks;
using DonTroc.Services;
using DonTroc.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using Plugin.Firebase.CloudMessaging;

namespace DonTroc;

public partial class App : Application
{
    private readonly UnreadMessageService _unreadMessageService;
    private readonly AuthService _authService;
    private readonly IServiceProvider _serviceProvider;
    private readonly FileLoggerService _fileLogger;
    private readonly NotificationService _notificationService;
    private readonly SmartNotificationService _smartNotificationService;
    private readonly RetentionNotificationService _retentionNotificationService;
    private readonly AppRatingService _appRatingService;
    private readonly GlobalNotificationService _globalNotificationService;
    private readonly ProximityNotificationService _proximityNotificationService;
    
    // Timer périodique pour les vérifications de rétention (toutes les 2h)
    private CancellationTokenSource? _retentionTimerCts;

    public App(AuthService authService, GlobalNotificationService globalNotificationService, UnreadMessageService unreadMessageService, AdMobService adMobService, IServiceProvider serviceProvider, FileLoggerService fileLogger, NotificationService notificationService, SmartNotificationService smartNotificationService, AppRatingService appRatingService, ProximityNotificationService proximityNotificationService, RetentionNotificationService retentionNotificationService)
    {
        InitializeComponent();

        _authService = authService;
        _unreadMessageService = unreadMessageService;
        _serviceProvider = serviceProvider;
        _fileLogger = fileLogger ?? new FileLoggerService();
        _notificationService = notificationService;
        _smartNotificationService = smartNotificationService;
        _retentionNotificationService = retentionNotificationService;
        _appRatingService = appRatingService;
        _globalNotificationService = globalNotificationService;
        _proximityNotificationService = proximityNotificationService;

        // Gestion d'erreurs
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            InitializeApp();
            _appRatingService.StartTracking();
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
            ShowErrorPage($"Erreur d'initialisation: {ex.Message}");
        }
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        var ex = args.ExceptionObject as Exception;
        _fileLogger.LogException(ex);
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
#if DEBUG
            ShowErrorPage($"Erreur critique: {ex?.Message ?? "Exception inconnue"}");
#else
            ShowErrorPage("Une erreur inattendue s'est produite. Veuillez redémarrer l'application.");
#endif
        });
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        try { _fileLogger.Log(args?.Exception?.ToString() ?? ""); } catch { }
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
#if DEBUG
            ShowErrorPage($"Erreur critique asynchrone: {args?.Exception?.Message}");
#else
            ShowErrorPage("Une erreur inattendue s'est produite. Veuillez redémarrer l'application.");
#endif
        });
        
        args?.SetObserved();
    }

    private void ShowErrorPage(string message)
    {
        try
        {
            var children = new List<IView>
            {
                new Label 
                { 
                    Text = "Une erreur est survenue 😔",
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new Label 
                { 
                    Text = message,
                    FontSize = 14,
                    HorizontalTextAlignment = TextAlignment.Center
                },
            };

#if DEBUG
            // En debug, afficher les outils de diagnostic
            var logPath = _fileLogger?.LogFilePath ?? "(chemin inconnu)";
            children.Add(new Label
            {
                Text = $"Journal: {logPath}",
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center,
                Opacity = 0.6
            });
            children.Add(new Button
            {
                Text = "Voir le journal (Debug)",
                Command = new Command(() =>
                {
                    var logs = _fileLogger?.ReadAllLogs() ?? "(aucun journal)";
                    MainPage = new ContentPage
                    {
                        Content = new StackLayout
                        {
                            Padding = 12,
                            Children =
                            {
                                new ScrollView
                                {
                                    Content = new Label { Text = logs, FontSize = 11 }
                                },
                                new Button
                                {
                                    Text = "Redémarrer l'application",
                                    Command = new Command(() => InitializeApp())
                                }
                            }
                        }
                    };
                })
            });
#endif

            children.Add(new Button
            {
                Text = "Redémarrer l'application",
                Command = new Command(() => InitializeApp())
            });

            MainPage = new ContentPage 
            { 
                Content = new StackLayout
                {
                    Padding = 20,
                    Spacing = 12,
                    VerticalOptions = LayoutOptions.Center,
                    Children = { }
                }
            };

            // Ajouter les enfants
            var layout = (StackLayout)((ContentPage)MainPage).Content;
            foreach (var child in children)
                layout.Children.Add(child);
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
        }
    }

    private void InitializeApp()
    {
        try
        {
            MainPage = new ContentPage
            {
                BackgroundColor = Color.FromArgb("#F6F1EB"),
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 20,
                    Children =
                    {
                        new Image 
                        { 
                            Source = "logotroc.png",
                            HeightRequest = 150,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new ActivityIndicator 
                        { 
                            IsRunning = true,
                            Color = Color.FromArgb("#8B4513"),
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = "Chargement...",
                            TextColor = Color.FromArgb("#8B4513"),
                            FontSize = 16,
                            HorizontalOptions = LayoutOptions.Center
                        }
                    }
                }
            };

            _ = InitializeAuthAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _fileLogger.LogException(t.Exception?.GetBaseException());
                    Debug.WriteLine($"[App] Erreur InitializeAuthAsync: {t.Exception?.GetBaseException()?.Message}");
                }
            }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
            ShowErrorPage($"Erreur lors du chargement: {ex.Message}");
        }
    }

    private async Task InitializeAuthAsync()
    {
        try
        {
            // ÉTAPE 1 : Vérifier l'onboarding AVANT l'authentification
            // L'onboarding est un écran d'introduction → il passe AVANT le login
            var onboardingService = _serviceProvider.GetRequiredService<OnboardingService>();
            if (!onboardingService.IsOnboardingCompleted)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var onboardingView = _serviceProvider.GetRequiredService<OnboardingView>();
                    MainPage = new NavigationPage(onboardingView);
                });
                return; // L'onboarding redirigera vers LoginView une fois terminé
            }

            // ÉTAPE 2 : Onboarding déjà fait → vérifier l'authentification
            bool isAuthenticated = _authService.IsSignedIn || await _authService.TryAutoSignInAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (isAuthenticated)
                {
                    MainPage = new AppShell(_unreadMessageService);
                }
                else
                {
                    MainPage = new NavigationPage(_serviceProvider.GetRequiredService<LoginView>());
                }
            });

            if (isAuthenticated)
            {
                try
                {
                    await _globalNotificationService.InitializeAsync();
                    _ = Task.Run(async () =>
                    {
                        try { await _proximityNotificationService.UpdateUserLocationAsync(); }
                        catch { }
                    });
                }
                catch { }
            }

            // Vérification des mises à jour de l'app (popup soft/force).
            // Fire-and-forget, délai de 3s pour ne pas perturber le premier affichage.
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    var appUpdateService = _serviceProvider.GetRequiredService<AppUpdateService>();
                    await appUpdateService.CheckForUpdateAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[App] Erreur AppUpdate check: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MainPage = new NavigationPage(_serviceProvider.GetRequiredService<LoginView>());
            });
        }
    }

    protected override void OnStart()
    {
        try
        {
            base.OnStart();

#if ANDROID
            _ = InitializePushNotificationsAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.WriteLine($"[App] Erreur push init: {t.Exception?.GetBaseException()?.Message}");
            }, TaskScheduler.Default);
#endif

            RunBackgroundTasks();
            
            // Tracker l'activité pour les rappels push serveur
            _ = UpdateLastActiveAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.WriteLine($"[App] Erreur UpdateLastActive: {t.Exception?.GetBaseException()?.Message}");
            }, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
        }
    }

#if ANDROID
    private async Task InitializePushNotificationsAsync()
    {
        await _notificationService.RequestNotificationPermissionAsync();
        await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
        var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();

        if (token != null)
        {
            var fcmService = _serviceProvider.GetRequiredService<FcmService>();
            await fcmService.OnTokenChanged(token);
        }

        // Abonner l'utilisateur au topic "all_users" pour recevoir les annonces
        // globales (nouvelle version, événements, etc.). Conforme Google Play.
        try
        {
            await CrossFirebaseCloudMessaging.Current.SubscribeToTopicAsync("all_users");
            Debug.WriteLine("[App] Abonné au topic FCM 'all_users'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Erreur subscribe topic: {ex.Message}");
        }
    }
#endif

    private void RunBackgroundTasks()
    {
        // Annuler un éventuel timer précédent
        _retentionTimerCts?.Cancel();
        _retentionTimerCts = new CancellationTokenSource();
        var ct = _retentionTimerCts.Token;

        Task.Run(async () =>
        {
            try
            {
                // Premier lancement après 30s (laisser l'app s'initialiser)
                await Task.Delay(TimeSpan.FromSeconds(30), ct);

                while (!ct.IsCancellationRequested)
                {
                    var userId = _authService.GetUserId();
                    if (!string.IsNullOrEmpty(userId))
                    {
                        // Suggestions personnalisées (existant)
                        try { await _smartNotificationService.SendPersonalizedSuggestionsAsync(userId); }
                        catch { /* ignoré */ }

                        // Vérifications de rétention (nouveau)
                        try { await _retentionNotificationService.RunAllChecksAsync(userId); }
                        catch { /* ignoré */ }
                    }

                    // Attendre 2h avant le prochain cycle
                    await Task.Delay(TimeSpan.FromHours(2), ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Timer annulé normalement (OnSleep ou Dispose)
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Erreur background tasks: {ex.Message}");
            }
        }, ct);
    }

    protected override void OnSleep()
    {
        try
        {
            base.OnSleep();
            // Arrêter le timer de rétention quand l'app passe en arrière-plan
            _retentionTimerCts?.Cancel();
        }
        catch (Exception ex) { _fileLogger.LogException(ex); }
    }

    protected override void OnResume()
    {
        try
        {
            base.OnResume();
            
            if (_authService.IsSignedIn)
            {
                // Relancer les tâches de fond (inclut le timer de rétention)
                RunBackgroundTasks();
                
                _ = Task.Run(async () =>
                {
                    try { await _proximityNotificationService.UpdateUserLocationAsync(); }
                    catch (Exception ex) { Debug.WriteLine($"[App] Erreur UpdateLocation: {ex.Message}"); }
                });
                
                // Tracker l'activité pour les rappels push serveur
                _ = UpdateLastActiveAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        Debug.WriteLine($"[App] Erreur UpdateLastActive (resume): {t.Exception?.GetBaseException()?.Message}");
                }, TaskScheduler.Default);
            }
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
        }
    }

    /// <summary>
    /// Met à jour le timestamp LastActiveAt dans Firebase pour le tracking de rétention serveur.
    /// Fire-and-forget, ne bloque pas l'UI.
    /// </summary>
    private async Task UpdateLastActiveAsync()
    {
        try
        {
            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var firebaseService = _serviceProvider.GetRequiredService<FirebaseService>();
                await firebaseService.UpdateLastActiveAsync(userId);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Erreur UpdateLastActive: {ex.Message}");
        }
    }
}
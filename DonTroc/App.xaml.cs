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
        DonTroc.Services.BootLogger.Log("App.ctor → start (DI resolved OK)");
        // ⚠️ FIX CRASH iOS : dfinir une MainPage de secours AVANT InitializeComponent().
        // Si InitializeComponent() (parsing XAML) lève une exception, MainPage reste null.
        // Or CreateWindow() (appelé depuis le callback UIKit de scène) appelle base.CreateWindow()
        // qui lève InvalidOperationException si MainPage == null → EXC_CRASH (SIGABRT).
        // Ce fallback garantit qu'une page est toujours disponible pour CreateWindow.
        MainPage = new ContentPage
        {
            BackgroundColor = Color.FromArgb("#F6F1EB"),
            Content = new ActivityIndicator { IsRunning = true }
        };

        try
        {
            InitializeComponent();
            DonTroc.Services.BootLogger.Log("App.ctor → InitializeComponent OK");
        }
        catch (Exception ex)
        {
            DonTroc.Services.BootLogger.LogException("App.InitializeComponent", ex);
            System.Diagnostics.Debug.WriteLine($"[App] InitializeComponent failed: {ex}");
            // MainPage de secours dj dfinie ci-dessus, on continue
        }

        UserAppTheme = AppTheme.Light;

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
            DonTroc.Services.BootLogger.Log("App.ctor → InitializeApp OK");
            _appRatingService.StartTracking();
        }
        catch (Exception ex)
        {
            DonTroc.Services.BootLogger.LogException("App.InitializeApp", ex);
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
            // ⚠️ FIX iOS écran noir : on construit une splash 100% safe.
            // Pas d'Image (le bundle iOS Release peut omettre/échouer à charger un asset),
            // pas de StaticResource (si App.Resources a échoué à se charger, écran noir).
            // BackgroundColor inline en hex = toujours visible.
            var splash = new ContentPage
            {
                BackgroundColor = Color.FromArgb("#F6F1EB"),
                Content = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 20,
                    Children =
                    {
                        new Label
                        {
                            Text = "DonTroc",
                            FontSize = 32,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#8B4513"),
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

            // Essayer d'ajouter le logo, sans crasher si l'asset manque
            try
            {
                var logo = new Image
                {
                    Source = "logotroc.png",
                    HeightRequest = 150,
                    HorizontalOptions = LayoutOptions.Center
                };
                ((VerticalStackLayout)splash.Content).Children.Insert(0, logo);
            }
            catch (Exception imgEx)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Logo load failed (safe): {imgEx.Message}");
            }

            MainPage = splash;

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
            DonTroc.Services.BootLogger.Log("InitializeAuthAsync → start");
            // ÉTAPE 1 : Vérifier l'onboarding AVANT l'authentification
            // L'onboarding est un écran d'introduction → il passe AVANT le login
            var onboardingService = _serviceProvider.GetRequiredService<OnboardingService>();
            DonTroc.Services.BootLogger.Log($"InitializeAuthAsync → OnboardingCompleted={onboardingService.IsOnboardingCompleted}");
            if (!onboardingService.IsOnboardingCompleted)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var onboardingView = _serviceProvider.GetRequiredService<OnboardingView>();
                    DonTroc.Services.BootLogger.Log("InitializeAuthAsync → resolved OnboardingView, SetRootPage");
                    SetRootPage(new NavigationPage(onboardingView));
                });
                return; // L'onboarding redirigera vers LoginView une fois terminé
            }

            // ÉTAPE 2 : Onboarding déjà fait → vérifier l'authentification
            bool isAuthenticated = _authService.IsSignedIn || await _authService.TryAutoSignInAsync();
            DonTroc.Services.BootLogger.Log($"InitializeAuthAsync → isAuthenticated={isAuthenticated}");

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    Page rootPage = isAuthenticated
                        ? (Page)new AppShell(_unreadMessageService)
                        : _serviceProvider.GetRequiredService<LoginView>();
                    DonTroc.Services.BootLogger.Log($"InitializeAuthAsync → rootPage built ({rootPage.GetType().Name}), SetRootPage");
                    SetRootPage(rootPage);
                    DonTroc.Services.BootLogger.Log("InitializeAuthAsync → SetRootPage DONE ✅");
                }
                catch (Exception innerEx)
                {
                    DonTroc.Services.BootLogger.LogException("InitializeAuthAsync.BuildRootPage", innerEx);
                    throw;
                }
            });

            if (isAuthenticated)
            {
                try
                {
                    await _globalNotificationService.InitializeAsync();
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _proximityNotificationService.UpdateUserLocationAsync();
                        }
                        catch
                        {
                            Console.WriteLine("[App] Erreur UpdateUserLocation (background): localisation indisponible ou permission refusée.");
                        }
                    });
                }
                catch (Exception ex)
                {
                        Debug.WriteLine($"[App] Erreur initialisation notifications: {ex.Message}");
                }
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

            // ⚠️ FIX iOS écran noir : si la création de LoginView/AppShell échoue
            // (DI cassée, XAML compilé invalide, asset manquant…), on doit MALGRÉ
            // TOUT afficher quelque chose de visible — sinon l'utilisateur reste
            // sur un écran noir indéfiniment.
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    SetRootPage(_serviceProvider.GetRequiredService<LoginView>());
                }
                catch (Exception fallbackEx)
                {
                    _fileLogger?.LogException(fallbackEx);
                    // Ultime fallback : page d'erreur 100% code (aucune dépendance XAML/DI).
                    SetRootPage(new ContentPage
                    {
                        BackgroundColor = Color.FromArgb("#F6F1EB"),
                        Content = new VerticalStackLayout
                        {
                            Padding = 24,
                            Spacing = 16,
                            VerticalOptions = LayoutOptions.Center,
                            Children =
                            {
                                new Label
                                {
                                    Text = "DonTroc",
                                    FontSize = 28,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.FromArgb("#8B4513"),
                                    HorizontalOptions = LayoutOptions.Center
                                },
                                new Label
                                {
                                    Text = "Impossible de démarrer l'application.",
                                    FontSize = 16,
                                    TextColor = Color.FromArgb("#444"),
                                    HorizontalTextAlignment = TextAlignment.Center
                                },
                                new Label
                                {
                                    Text = $"Détail : {ex.Message}",
                                    FontSize = 12,
                                    TextColor = Color.FromArgb("#888"),
                                    HorizontalTextAlignment = TextAlignment.Center
                                },
                                new Button
                                {
                                    Text = "Réessayer",
                                    BackgroundColor = Color.FromArgb("#D98C6A"),
                                    TextColor = Colors.White,
                                    Command = new Command(() => InitializeApp())
                                }
                            }
                        }
                    });
                }
            });
        }
    }

    /// <summary>
    /// Remplace la page racine via l'API Window MAUI 9 (Windows[0].Page).
    /// Sur iOS, cela force la libération de l'ancien UIViewController root
    /// (UINavigationController résiduel) qui sinon laisse une bande grise
    /// courbe (UINavigationBar translucide) visible sur toutes les pages.
    /// Fallback sur MainPage si aucune Window n'est encore active.
    /// </summary>
    private void SetRootPage(Page page)
    {
        try
        {
            if (Windows != null && Windows.Count > 0)
            {
                Windows[0].Page = page;
            }
            else
            {
                MainPage = page;
            }
        }
        catch (Exception ex)
        {
            _fileLogger?.LogException(ex);
            MainPage = page;
        }
    }

    /// <summary>
    /// Override CreateWindow pour garantir qu'une page racine est toujours disponible.
    /// ⚠️ Cette méthode est appelée depuis le callback UIKit de création de scène
    /// (FBSScene → MauiUISceneDelegate). Toute exception non catchée ici provoque
    /// un EXC_CRASH (SIGABRT). On s'assure donc que MainPage est toujours défini
    /// et on entoure toute la logique d'un try-catch.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        DonTroc.Services.BootLogger.Log($"App.CreateWindow → start (MainPage={(MainPage?.GetType().Name ?? "null")})");
        try
        {
            // Garantir qu'une MainPage est toujours définie avant CreateWindow.
            if (MainPage == null)
            {
                DonTroc.Services.BootLogger.Log("App.CreateWindow → MainPage was NULL, applying fallback");
                MainPage = new ContentPage
                {
                    BackgroundColor = Color.FromArgb("#F6F1EB"),
                    Content = new ActivityIndicator { IsRunning = true }
                };
            }

            var window = base.CreateWindow(activationState);
            DonTroc.Services.BootLogger.Log("App.CreateWindow → base.CreateWindow OK ✅");
            return window;
        }
        catch (Exception ex)
        {
            DonTroc.Services.BootLogger.LogException("App.CreateWindow", ex);
            _fileLogger?.LogException(ex);
            System.Diagnostics.Debug.WriteLine($"[App] CreateWindow exception: {ex}");

            // Fallback minimal : créer une fenêtre avec une page d'erreur.
            // Ne JAMAIS laisser l'exception remonter jusqu'au callback UIKit natif.
            MainPage = new ContentPage
            {
                BackgroundColor = Color.FromArgb("#F6F1EB"),
                Content = new Label
                {
                    Text = "Démarrage...",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            return base.CreateWindow(activationState);
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
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

    public App(AuthService authService, GlobalNotificationService globalNotificationService, UnreadMessageService unreadMessageService, AdMobService adMobService, IServiceProvider serviceProvider, FileLoggerService fileLogger, NotificationService notificationService, SmartNotificationService smartNotificationService)
    {
        InitializeComponent();

        _authService = authService;
        _unreadMessageService = unreadMessageService;
        _serviceProvider = serviceProvider;
        _fileLogger = fileLogger ?? new FileLoggerService();
        _notificationService = notificationService;
        _smartNotificationService = smartNotificationService;

        // Gestion d'erreurs
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            InitializeApp();
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
            System.Diagnostics.Debug.WriteLine($"[App] Erreur lors de l'initialisation: {ex}");
            ShowErrorPage($"Erreur d'initialisation: {ex.Message}");
        }
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        var ex = args.ExceptionObject as Exception;
        _fileLogger.LogException(ex);
        System.Diagnostics.Debug.WriteLine($"[App] Exception non gérée: {ex?.Message}\n{ex?.StackTrace}");
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ShowErrorPage($"Erreur critique: {ex?.Message ?? "Exception inconnue"}");
        });
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        try
        {
            if (args?.Exception != null)
            {
                _fileLogger.Log(args.Exception.ToString());
            }
        }
        catch { }

        System.Diagnostics.Debug.WriteLine($"[App] Exception asynchrone non gérée: {args?.Exception?.Message}\n{args?.Exception?.StackTrace}");
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ShowErrorPage($"Erreur critique asynchrone: {args?.Exception?.Message}");
        });
        
        args?.SetObserved();
    }

    private void ShowErrorPage(string message)
    {
        try
        {
            var logPath = _fileLogger?.LogFilePath ?? "(chemin du journal inconnu)";

            MainPage = new ContentPage 
            { 
                Content = new StackLayout
                {
                    Padding = 20,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label 
                        { 
                            Text = message,
                            FontSize = 16,
                            HorizontalTextAlignment = TextAlignment.Center
                        },
                        new Label
                        {
                            Text = $"Journal: {logPath}",
                            FontSize = 12,
                            HorizontalTextAlignment = TextAlignment.Center
                        },
                        new Button
                        {
                            Text = "Voir le journal",
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
                                                Content = new Label { Text = logs }
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
                        },
                        new Button
                        {
                            Text = "Redémarrer l'application",
                            Command = new Command(() => InitializeApp())
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
            System.Diagnostics.Debug.WriteLine($"[App] Erreur lors de l'affichage de la page d'erreur: {ex}");
        }
    }

    private void InitializeApp()
    {
        try
        {
            if (_authService.IsSignedIn)
            {
                MainPage = new AppShell(_unreadMessageService);
            }
            else
            {
                // ✅ Solution simplifiée : utiliser directement le service provider
                var loginView = _serviceProvider.GetRequiredService<LoginView>();
                MainPage = new NavigationPage(loginView);
            }
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
            System.Diagnostics.Debug.WriteLine($"[App] Erreur lors de l'initialisation de la page principale: {ex}");
            ShowErrorPage($"Erreur lors du chargement: {ex.Message}");
        }
    }

    protected override void OnStart()
    {
        try
        {
            base.OnStart();
            Debug.WriteLine("[App] Application démarrée");
            
            // Si des opérations asynchrones sont nécessaires, les démarrer sans await
            _ = Task.Run(async () =>
            {
                try
                {
                    // Initialisation asynchrone en arrière-plan si nécessaire
                    await Task.Delay(100); // Placeholder pour d'éventuelles initialisations
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[App] Erreur lors de l'initialisation asynchrone: {ex.Message}");
                }
            });

#if ANDROID
            // Demander la permission pour les notifications et initialiser FCM
            _ = InitializePushNotificationsAsync();
#endif

            // Lancer les tâches de fond (notifications, etc.)
            RunBackgroundTasks();
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
            Debug.WriteLine($"[App] Erreur OnStart: {ex}");
        }
    }

#if ANDROID
    private async Task InitializePushNotificationsAsync()
    {
        // Demander la permission à l'utilisateur
        await _notificationService.RequestNotificationPermissionAsync();

        // Initialiser FCM et récupérer le jeton
        await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
        var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
        Debug.WriteLine($"[App] Jeton FCM récupéré: {token}");

        // Sauvegarder le jeton (le FcmService est déjà injecté et gère le OnTokenChanged)
        // On peut appeler manuellement pour s'assurer que le token est sauvegardé au démarrage
        var fcmService = _serviceProvider.GetRequiredService<FcmService>();
        if (token != null)
        {
            await fcmService.OnTokenChanged(token);
        }
    }
#endif

    private void RunBackgroundTasks()
    {
        Task.Run(async () =>
        {
            // Attendre un peu pour ne pas impacter le démarrage
            await Task.Delay(TimeSpan.FromSeconds(30));

            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                Debug.WriteLine("[App] Lancement des tâches de fond pour les notifications intelligentes.");
                await _smartNotificationService.SendPersonalizedSuggestionsAsync(userId);
            }
        });
    }

    protected override void OnSleep()
    {
        try
        {
            base.OnSleep();
            System.Diagnostics.Debug.WriteLine("[App] Application en veille");
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
            System.Diagnostics.Debug.WriteLine($"[App] Erreur OnSleep: {ex}");
        }
    }

    protected override void OnResume()
    {
        try
        {
            base.OnResume();
            System.Diagnostics.Debug.WriteLine("[App] Application reprise");
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
            System.Diagnostics.Debug.WriteLine($"[App] Erreur OnResume: {ex}");
        }
    }
}
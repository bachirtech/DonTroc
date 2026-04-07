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
    private readonly AppRatingService _appRatingService;
    private readonly GlobalNotificationService _globalNotificationService;
    private readonly ProximityNotificationService _proximityNotificationService;

    public App(AuthService authService, GlobalNotificationService globalNotificationService, UnreadMessageService unreadMessageService, AdMobService adMobService, IServiceProvider serviceProvider, FileLoggerService fileLogger, NotificationService notificationService, SmartNotificationService smartNotificationService, AppRatingService appRatingService, ProximityNotificationService proximityNotificationService)
    {
        InitializeComponent();

        _authService = authService;
        _unreadMessageService = unreadMessageService;
        _serviceProvider = serviceProvider;
        _fileLogger = fileLogger ?? new FileLoggerService();
        _notificationService = notificationService;
        _smartNotificationService = smartNotificationService;
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
            ShowErrorPage($"Erreur critique: {ex?.Message ?? "Exception inconnue"}");
        });
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        try { _fileLogger.Log(args?.Exception?.ToString() ?? ""); } catch { }
        
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
        }
    }

    private void InitializeApp()
    {
        try
        {
            MainPage = new ContentPage
            {
                BackgroundColor = Color.FromArgb("#F5F5DC"),
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

            _ = InitializeAuthAsync();
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
            bool isAuthenticated = _authService.IsSignedIn || await _authService.TryAutoSignInAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (isAuthenticated)
                    MainPage = new AppShell(_unreadMessageService);
                else
                    MainPage = new NavigationPage(_serviceProvider.GetRequiredService<LoginView>());
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
            _ = InitializePushNotificationsAsync();
#endif

            RunBackgroundTasks();
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
    }
#endif

    private void RunBackgroundTasks()
    {
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30));

            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _smartNotificationService.SendPersonalizedSuggestionsAsync(userId);
            }
        });
    }

    protected override void OnSleep()
    {
        try { base.OnSleep(); }
        catch (Exception ex) { _fileLogger.LogException(ex); }
    }

    protected override void OnResume()
    {
        try
        {
            base.OnResume();
            
            if (_authService.IsSignedIn)
            {
                _ = Task.Run(async () =>
                {
                    try { await _proximityNotificationService.UpdateUserLocationAsync(); }
                    catch { }
                });
            }
        }
        catch (Exception ex)
        {
            _fileLogger.LogException(ex);
        }
    }
}
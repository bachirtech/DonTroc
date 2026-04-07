﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System;
using CommunityToolkit.Maui;
using DonTroc.Services;
using DonTroc.ViewModels;
using DonTroc.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Bundled.Shared;
using Plugin.Maui.Audio;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Core.Hosting;
#if ANDROID
using DonTroc.Platforms.Android;
using Plugin.Firebase.Bundled.Platforms.Android;
#endif

#if IOS
using Plugin.Firebase.Bundled.Platforms.iOS;
#endif

namespace DonTroc;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureLifecycleEvents(events =>
            {
#if ANDROID
                events.AddAndroid(android => android.OnCreate((activity, _) =>
                {
                    CrossFirebase.Initialize(activity, () => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity!, CreateCrossFirebaseSettings());
                }));
#endif
#if IOS
                events.AddiOS(ios => ios.FinishedLaunching((app, launchOptions) => {
                    CrossFirebase.Initialize(CreateCrossFirebaseSettings());
                    return false;
                }));
#endif
            })
            .UseSkiaSharp()
            .ConfigureSyncfusionCore()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Poppins-Regular.ttf", "PoppinsRegular");
                fonts.AddFont("Poppins-Bold.ttf", "PoppinsBold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                // Enregistrer le handler personnalisé pour AdBannerView sur Android (AdMob)
                handlers.AddHandler<DonTroc.Views.AdBannerView, AdMobBannerHandler>();
                
                // Handler unifié pour UnifiedAdBannerView (AdMob)
                handlers.AddHandler<DonTroc.Views.UnifiedAdBannerView, UnifiedAdBannerHandler>();
#endif
            });

        // Configuration du plugin audio
        builder.Services.AddSingleton(AudioManager.Current);

        // Configuration des services de base optimisés pour production
        builder.Services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100 * 1024 * 1024; // 100MB par défaut
            options.CompactionPercentage = 0.25;
        });
        builder.Services.AddHttpClient("DonTrocClient", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5); // 5 minutes par défaut
            client.DefaultRequestHeaders.Add("User-Agent", "DonTroc/1.0");
        });

        // Services core (ordre important pour l'injection de dépendances)
        builder.Services.AddSingleton<CacheService>();
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<PerformanceService>();

        // Services externes (doivent être enregistrés avant FirebaseService)
        builder.Services.AddSingleton<CloudinaryConfigService>();
        builder.Services.AddSingleton<SecureCloudinaryService>();
        builder.Services.AddSingleton<FileLoggerService>();

        // Services d'authentification (doit être avant FirebaseService)
        builder.Services.AddSingleton<PasswordValidationService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<FirebaseService>();
        builder.Services.AddSingleton<GeolocationService>();

        // Services AdMob - Injection de dépendances par plateforme
#if ANDROID
        builder.Services.AddSingleton<IAdMobService, DonTroc.Platforms.Android.AdMobNativeService>();
        // GOOGLE SIGN-IN DÉSACTIVÉ TEMPORAIREMENT
        // builder.Services.AddSingleton<GoogleAuthService, DonTroc.Platforms.Android.GoogleAuthService>();
#elif IOS
        builder.Services.AddSingleton<IAdMobService, DonTroc.Platforms.iOS.AdMobNativeService>();
#else
        builder.Services.AddSingleton<IAdMobService, AdMobSimulationService>();
#endif
        builder.Services.AddTransient<AdMobService>();

        // Service d'achats in-app (suppression des pubs)
        builder.Services.AddSingleton<InAppBillingService>();


        // Autres services
        builder.Services.AddSingleton<GamificationService>();
        builder.Services.AddSingleton<IGamificationService>(sp => sp.GetRequiredService<GamificationService>());
        builder.Services.AddSingleton<QuizService>();
        builder.Services.AddSingleton<IQuizService>(sp => sp.GetRequiredService<QuizService>());
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<PushNotificationService>();
        builder.Services.AddSingleton<SmartNotificationService>();
        builder.Services.AddSingleton<AsyncImageUploadService>();
        builder.Services.AddSingleton<OptimizedImageService>();
        builder.Services.AddSingleton<GlobalNotificationService>();
        builder.Services.AddSingleton<UnreadMessageService>();
        builder.Services.AddSingleton<RatingService>();
        builder.Services.AddSingleton<TransactionService>();
        builder.Services.AddSingleton<SocialService>();
        builder.Services.AddSingleton<LazyLoadingService>();
        builder.Services.AddSingleton<FavoritesService>();
        builder.Services.AddSingleton<AudioService>();
        builder.Services.AddSingleton<AnimationService>();
        builder.Services.AddSingleton<ReportService>();
        builder.Services.AddSingleton<TipsService>();
        builder.Services.AddSingleton<ITipsService>(sp => sp.GetRequiredService<TipsService>());
        builder.Services.AddSingleton<AppRatingService>();
        builder.Services.AddSingleton<ProximityNotificationService>();
        
        // Service d'administration
        builder.Services.AddSingleton<AdminService>();

#if ANDROID
        // Service pour les notifications Push FCM
        builder.Services.AddSingleton<FcmService>();
        builder.Services.AddSingleton<IFirebaseCloudMessagingDelegate>(provider =>
            provider.GetRequiredService<FcmService>());
#endif

        // ViewModels (enregistrés en tant que transitoires pour un cycle de vie propre)
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ChatViewModel>();
        builder.Services.AddTransient<ConversationsViewModel>();
        builder.Services.AddTransient<AnnoncesViewModel>();
        builder.Services.AddTransient<CreationAnnonceViewModel>();
        builder.Services.AddTransient<EditAnnonceViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<FavoritesViewModel>();
        builder.Services.AddTransient<EditProfileViewModel>();
        builder.Services.AddTransient<ImageViewerViewModel>();
        builder.Services.AddTransient<ProfilViewModel>();
        builder.Services.AddTransient<RatingViewModel>();
        builder.Services.AddTransient<TransactionsViewModel>();
        builder.Services.AddTransient<SocialViewModel>();
        builder.Services.AddTransient<TransactionDetailsViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<BaseViewModel>();
        builder.Services.AddTransient<PremiumFeaturesViewModel>();
        builder.Services.AddTransient<ModerationViewModel>();
        builder.Services.AddTransient<RewardsViewModel>();
        builder.Services.AddTransient<QuizViewModel>();
        builder.Services.AddTransient<WheelOfFortuneViewModel>();
        builder.Services.AddTransient<AllBadgesViewModel>();
        
        // ViewModels d'administration
        builder.Services.AddTransient<AdminDashboardViewModel>();
        builder.Services.AddTransient<UserManagementViewModel>();
        builder.Services.AddTransient<AdminLogsViewModel>();

        // Vues
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<LoginView>();
        builder.Services.AddTransient<CreationAnnonceView>();
        builder.Services.AddTransient<AnnoncesView>();
        builder.Services.AddTransient<ChatView>();
        builder.Services.AddTransient<ConversationsView>();
        builder.Services.AddTransient<DashboardView>();
        builder.Services.AddTransient<EditAnnonceView>();
        builder.Services.AddTransient<EditProfileView>();
        builder.Services.AddTransient<FavoritesView>();
        builder.Services.AddTransient<ProfilView>();
        builder.Services.AddTransient<RatingView>();
        builder.Services.AddTransient<SocialView>();
        builder.Services.AddTransient<TransactionsView>();
        builder.Services.AddTransient<TransactionDetailsView>();
        builder.Services.AddTransient<MapView>();
        builder.Services.AddTransient<ImageViewerView>();
        builder.Services.AddTransient<ModerationPage>();
        builder.Services.AddTransient<RewardsPage>();
        builder.Services.AddTransient<QuizPage>();
        builder.Services.AddTransient<WheelOfFortunePage>();
        builder.Services.AddTransient<AllBadgesPage>();
        
        // Pages d'administration
        builder.Services.AddTransient<AdminDashboardPage>();
        builder.Services.AddTransient<UserManagementPage>();
        builder.Services.AddTransient<AdminLogsPage>();
        builder.Services.AddTransient<AdminSetupPage>();

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        return builder.Build();
    }

    private static CrossFirebaseSettings CreateCrossFirebaseSettings()
    {
        return new CrossFirebaseSettings(
            isAuthEnabled: true,
            isCloudMessagingEnabled: true,
            isAnalyticsEnabled: true,
            isCrashlyticsEnabled: true,
            isRemoteConfigEnabled: true,
            isStorageEnabled: true
            // GOOGLE SIGN-IN DÉSACTIVÉ TEMPORAIREMENT
            // googleRequestIdToken: "12542152309-asqvk30n6eukuio6tbg8nm93vq4h2lv6.apps.googleusercontent.com"
        );
    }
}
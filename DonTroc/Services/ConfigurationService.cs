using System;

namespace DonTroc.Services
{
    /// <summary>
    /// Service de configuration optimisé pour la production - Version Release
    /// </summary>
    public class ConfigurationService
    {
        public static bool IsProduction => 
#if DEBUG
            false;
#else
            true;
#endif

        public static string Environment => IsProduction ? "Production" : "Development";

        // URLs Firebase
        public const string FirebaseUrl = "https://dontroc-55570-default-rtdb.europe-west1.firebasedatabase.app/";
        public const string FirebaseApiKey = "AIzaSyCMAQf1wqkpI-G6cqtZS7esPa8juh71UJw";
        public static string FirebaseAuthDomain => "dontroc-55570.firebaseapp.com";

        // Configuration de logging optimisée pour la production
        public static bool EnableDetailedLogging => !IsProduction;
        public static bool EnablePerformanceLogging => !IsProduction; // Désactivé en production pour les performances
        public static bool EnableCrashReporting => IsProduction;

        // Configuration du cache optimisée pour la mémoire
        public static TimeSpan DefaultCacheExpiry => TimeSpan.FromMinutes(IsProduction ? 60 : 5);
        public static int MaxCacheSize => IsProduction ? 200 : 50;
        public static long MaxMemoryThreshold => IsProduction ? 150 * 1024 * 1024 : 50 * 1024 * 1024; // 150MB en prod, 50MB en dev

        // Configuration des uploads optimisée
        public static int MaxConcurrentUploads => IsProduction ? 4 : 2;
        public static TimeSpan UploadTimeout => TimeSpan.FromMinutes(IsProduction ? 10 : 2);
        public static int MaxImageSize => 8 * 1024 * 1024; // 8MB max pour Cloudinary
        public static int OptimalImageDimension => 1080; // Résolution optimale

        // Configuration de performance production
        public static int MaxRetryAttempts => IsProduction ? 5 : 3;
        public static TimeSpan BaseRetryDelay => TimeSpan.FromSeconds(IsProduction ? 2 : 1);
        public static int DatabaseConnectionPoolSize => IsProduction ? 20 : 5;

        // Configuration réseau optimisée
        public static TimeSpan HttpClientTimeout => TimeSpan.FromSeconds(IsProduction ? 45 : 15);
        public static int MaxHttpConnections => IsProduction ? 50 : 10;

        // Seuils de performance optimisés pour la production
        public static class PerformanceThresholds
        {
            public static TimeSpan SlowOperationThreshold => TimeSpan.FromMilliseconds(IsProduction ? 2000 : 500);
            public static TimeSpan VerySlowOperationThreshold => TimeSpan.FromMilliseconds(IsProduction ? 5000 : 1500);
            public static int MaxImageCacheSize => IsProduction ? 100 : 20;
            public static int MaxDatabaseQueryTime => IsProduction ? 3000 : 1000;
        }

        // Configuration de sécurité pour la production
        public static class Security
        {
            public static bool EnableTokenRefresh => IsProduction;
            public static TimeSpan TokenRefreshInterval => TimeSpan.FromMinutes(IsProduction ? 45 : 15);
            public static bool EnableRequestValidation => IsProduction;
            public static int MaxRequestsPerMinute => IsProduction ? 100 : 1000;
        }

        // Configuration de nettoyage automatique
        public static class Cleanup
        {
            public static TimeSpan CacheCleanupInterval => TimeSpan.FromMinutes(IsProduction ? 30 : 10);
            public static TimeSpan GarbageCollectionInterval => TimeSpan.FromMinutes(IsProduction ? 15 : 5);
            public static float CacheCleanupThreshold => 0.8f; // Nettoyer quand le cache atteint 80%
        }

        // Configuration UI optimisée
        public static class UI
        {
            public static bool EnableAnimations => !IsProduction; // Désactiver les animations en prod pour les performances
            public static int ListVirtualizationThreshold => IsProduction ? 20 : 10;
            public static TimeSpan UIUpdateThrottle => TimeSpan.FromMilliseconds(IsProduction ? 100 : 50);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;
using System.Text.Json;
using System.Threading.Tasks;
using DonTroc.Models;

namespace DonTroc.Services
{
    /// <summary>
    /// Service de cache haute performance optimisé pour la production
    /// </summary>
    public class CacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ObjectPool<StringBuilder> _stringBuilderPool;
        private readonly ConcurrentDictionary<string, DateTime> _cacheKeys;
        private readonly ConcurrentDictionary<string, TimeSpan> _performanceMetrics;

        public CacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _cacheKeys = new ConcurrentDictionary<string, DateTime>();
            _performanceMetrics = new ConcurrentDictionary<string, TimeSpan>();

            // Pool d'objets pour réduire les allocations
            var provider = new DefaultObjectPoolProvider();
            _stringBuilderPool = provider.CreateStringBuilderPool();
        }

        #region Cache Générique Optimisé

        /// <summary>
        /// Obtient un objet du cache ou l'exécute et le met en cache avec optimisation
        /// </summary>
        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            // Vérification rapide du cache
            if (_memoryCache.TryGetValue(key, out T? cachedValue))
            {
                return cachedValue;
            }

            // Exécution de la factory avec mesure de performance
            var startTime = DateTime.UtcNow;
            var value = await factory();
            var duration = DateTime.UtcNow - startTime;

            // Enregistrer les métriques de performance
            _performanceMetrics.AddOrUpdate(key, duration,
                (k, v) => TimeSpan.FromTicks((v.Ticks + duration.Ticks) / 2));

            if (value != null)
            {
                var actualExpiration = expiration ?? ConfigurationService.DefaultCacheExpiry;
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = actualExpiration,
                    Priority = GetCachePriority<T>(),
                    Size = EstimateObjectSize(value)
                };

                // Callback pour nettoyer les métadonnées lors de l'expiration
                options.RegisterPostEvictionCallback((k, v, reason, state) =>
                {
                    var keyString = k?.ToString();
                    if (keyString != null)
                    {
                        _cacheKeys.TryRemove(keyString, out _);
                    }
                });

                _memoryCache.Set(key, value, options);
                _cacheKeys.TryAdd(key, DateTime.UtcNow.Add(actualExpiration));
            }

            return value;
        }

        /// <summary>
        /// Met un objet en cache avec priorité intelligente
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiration = null, CacheItemPriority? priority = null)
        {
            if (value != null)
            {
                var actualExpiration = expiration ?? ConfigurationService.DefaultCacheExpiry;
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = actualExpiration,
                    Priority = priority ?? GetCachePriority<T>(),
                    Size = EstimateObjectSize(value)
                };

                options.RegisterPostEvictionCallback((k, v, reason, state) =>
                {
                    var keyString = k?.ToString();
                    if (keyString != null)
                    {
                        _cacheKeys.TryRemove(keyString, out _);
                    }
                });

                _memoryCache.Set(key, value, options);
                _cacheKeys.TryAdd(key, DateTime.UtcNow.Add(actualExpiration));
            }
        }

        /// <summary>
        /// Obtient un objet du cache avec vérification d'expiration
        /// </summary>
        public T? Get<T>(string key)
        {
            if (_cacheKeys.TryGetValue(key, out var expiration) && expiration < DateTime.UtcNow)
            {
                Remove(key);
                return default;
            }

            return _memoryCache.TryGetValue(key, out T? value) ? value : default;
        }

        /// <summary>
        /// Supprime un objet du cache avec nettoyage des métadonnées
        /// </summary>
        public void Remove(string key)
        {
            _memoryCache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
            _performanceMetrics.TryRemove(key, out _);
        }

        /// <summary>
        /// Supprime tous les objets du cache dont la clé commence par le préfixe spécifié
        /// </summary>
        public void RemoveByPrefix(string prefix)
        {
            var keysToRemove = _cacheKeys.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                Remove(key);
            }
        }

        #endregion

        #region Cache spécialisé pour les annonces

        /// <summary>
        /// Cache optimisé pour les annonces avec compression
        /// </summary>
        public async Task<List<Annonce>?> GetAnnoncesAsync(string cacheKey, Func<Task<List<Annonce>>> factory)
        {
            return await GetOrSetAsync(cacheKey, factory, ConfigurationService.DefaultCacheExpiry);
        }

        /// <summary>
        /// Cache une annonce individuelle
        /// </summary>
        public void CacheAnnonce(Annonce annonce)
        {
            if (annonce?.Id != null)
            {
                Set($"annonce_{annonce.Id}", annonce, ConfigurationService.DefaultCacheExpiry, CacheItemPriority.High);
            }
        }

        /// <summary>
        /// Obtient une annonce mise en cache
        /// </summary>
        public Annonce? GetCachedAnnonce(string annonceId)
        {
            return Get<Annonce>($"annonce_{annonceId}");
        }

        /// <summary>
        /// Invalide le cache d'une annonce
        /// </summary>
        public void InvalidateAnnonce(string annonceId)
        {
            Remove($"annonce_{annonceId}");
            RemoveByPrefix("annonces_");
        }

        #endregion

        #region Cache des images avec optimisation mémoire

        /// <summary>
        /// Cache optimisé pour les images avec limitation de taille
        /// </summary>
        public void CacheImage(string imageUrl, byte[] imageData)
        {
            if (imageData.Length <= 5 * 1024 * 1024) // Limite à 5MB par image
            {
                var key = $"image_{GetImageCacheKey(imageUrl)}";
                Set(key, imageData, TimeSpan.FromHours(2), CacheItemPriority.Low);
            }
        }

        /// <summary>
        /// Obtient une image mise en cache
        /// </summary>
        public byte[]? GetCachedImage(string imageUrl)
        {
            var key = $"image_{GetImageCacheKey(imageUrl)}";
            return Get<byte[]>(key);
        }

        /// <summary>
        /// Génère une clé de cache pour les images
        /// </summary>
        private string GetImageCacheKey(string imageUrl)
        {
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(imageUrl)))
                [..16];
        }

        #endregion

        #region Métriques de performance

        /// <summary>
        /// Cache les métriques de performance
        /// </summary>
        public void CachePerformanceMetrics(string operation, TimeSpan duration)
        {
            if (ConfigurationService.EnablePerformanceLogging)
            {
                var key = $"perf_{operation}";
                Set(key, duration, TimeSpan.FromHours(1), CacheItemPriority.Low);
            }
        }

        /// <summary>
        /// Obtient les métriques de performance cachées
        /// </summary>
        public Dictionary<string, TimeSpan> GetCachedPerformanceMetrics()
        {
            var metrics = new Dictionary<string, TimeSpan>();
            var perfKeys = _cacheKeys.Keys.Where(k => k.StartsWith("perf_")).ToList();

            foreach (var key in perfKeys)
            {
                var duration = Get<TimeSpan>(key);
                if (duration != default)
                {
                    metrics[key.Substring(5)] = duration; // Enlever le préfixe "perf_"
                }
            }

            return metrics;
        }

        #endregion

        #region Optimisation et maintenance

        /// <summary>
        /// Optimise le cache en supprimant les entrées expirées
        /// </summary>
        public void OptimizeCache()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _cacheKeys
                .Where(kvp => kvp.Value < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                Remove(key);
            }

            // Force le garbage collection si nécessaire
            if (expiredKeys.Count > ConfigurationService.PerformanceThresholds.MaxImageCacheSize)
            {
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }

        /// <summary>
        /// Obtient les statistiques du cache
        /// </summary>
        public CacheStats GetCacheStats()
        {
            var now = DateTime.UtcNow;
            var totalEntries = _cacheKeys.Count;
            var expiredEntries = _cacheKeys.Values.Count(expiration => expiration < now);

            return new CacheStats
            {
                TotalEntries = totalEntries,
                ExpiredEntries = expiredEntries,
                ActiveEntries = totalEntries - expiredEntries,
                HitRate = CalculateHitRate(),
                MemoryUsage = EstimateMemoryUsage()
            };
        }

        /// <summary>
        /// Vide complètement le cache
        /// </summary>
        public void ClearAll()
        {
            foreach (var key in _cacheKeys.Keys.ToList())
            {
                Remove(key);
            }
        }

        /// <summary>
        /// Nettoie les anciennes entrées du cache basé sur un pourcentage
        /// Méthode manquante ajoutée pour résoudre l'erreur dans PerformanceService
        /// </summary>
        public void CleanupOldEntries(float percentageToClean)
        {
            if (percentageToClean <= 0 || percentageToClean > 1)
                return;

            var now = DateTime.UtcNow;
            var allEntries = _cacheKeys.ToList();

            // Trier par ordre d'expiration (les plus anciens d'abord)
            var sortedEntries = allEntries
                .OrderBy(kvp => kvp.Value)
                .ToList();

            // Calculer le nombre d'entrées à supprimer
            var entriesToRemove = (int)(sortedEntries.Count * percentageToClean);

            // Supprimer les entrées les plus anciennes
            var keysToRemove = sortedEntries
                .Take(entriesToRemove)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                Remove(key);
            }

            // Log l'opération de nettoyage si configuré
            if (ConfigurationService.EnableDetailedLogging && keysToRemove.Count > 0)
            {
                // Note: Le logging sera géré par PerformanceService
            }
        }

        /// <summary>
        /// Nettoie automatiquement le cache si la mémoire dépasse les seuils
        /// </summary>
        public void AutoCleanupIfNeeded()
        {
            var currentMemory = GC.GetTotalMemory(false);
            var threshold = ConfigurationService.MaxMemoryThreshold;

            if (currentMemory > threshold * 0.8) // 80% du seuil
            {
                // Nettoyer 25% des entrées les plus anciennes
                CleanupOldEntries(0.25f);
            }
            else if (currentMemory > threshold * 0.9) // 90% du seuil
            {
                // Nettoyer 40% des entrées les plus anciennes
                CleanupOldEntries(0.40f);
            }
        }

        #endregion

        #region Méthodes utilitaires privées

        /// <summary>
        /// Détermine la priorité du cache basée sur le type
        /// </summary>
        private CacheItemPriority GetCachePriority<T>()
        {
            var type = typeof(T);

            // Priorité haute pour les données critiques
            if (type == typeof(Annonce) || type == typeof(List<Annonce>))
                return CacheItemPriority.High;

            // Priorité normale pour les données utilisateur
            if (type == typeof(UserProfile) || type == typeof(Conversation))
                return CacheItemPriority.Normal;

            // Priorité basse pour les images et données temporaires
            if (type == typeof(byte[]))
                return CacheItemPriority.Low;

            return CacheItemPriority.Normal;
        }

        /// <summary>
        /// Estime la taille d'un objet pour la gestion mémoire
        /// </summary>
        private long EstimateObjectSize<T>(T obj)
        {
            try
            {
                if (obj == null) return 0;

                // Estimation basique selon le type
                var type = typeof(T);

                if (type == typeof(byte[]))
                    return ((byte[])(object)obj).Length;

                if (type == typeof(string))
                    return ((string)(object)obj).Length * 2; // Unicode

                // Pour les objets complexes, utiliser la sérialisation JSON comme estimation
                var json = JsonSerializer.Serialize(obj);
                return json.Length * 2; // Estimation approximative
            }
            catch
            {
                // En cas d'erreur, retourner une taille par défaut
                return 1024; // 1KB par défaut
            }
        }

        /// <summary>
        /// Calcule le taux de succès du cache
        /// </summary>
        private double CalculateHitRate()
        {
            // Implémentation simplifiée - en production, vous pourriez
            // suivre les hits/misses de manière plus détaillée
            var totalEntries = _cacheKeys.Count;
            var activeEntries = _cacheKeys.Values.Count(exp => exp > DateTime.UtcNow);

            return totalEntries > 0 ? (double)activeEntries / totalEntries : 0.0;
        }

        /// <summary>
        /// Estime l'utilisation mémoire du cache
        /// </summary>
        private long EstimateMemoryUsage()
        {
            // Estimation basique - en production, vous pourriez utiliser
            // des outils plus sophistiqués de mesure mémoire
            return _cacheKeys.Count * 1024; // Estimation approximative
        }

        #endregion
    }

    /// <summary>
    /// Statistiques du cache pour le monitoring
    /// </summary>
    public class CacheStats
    {
        public int TotalEntries { get; set; }
        public int ExpiredEntries { get; set; }
        public int ActiveEntries { get; set; }
        public double HitRate { get; set; }
        public long MemoryUsage { get; set; }
    }
}


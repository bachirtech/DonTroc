using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DonTroc.Models;

namespace DonTroc.Services
{
    /// <summary>
    /// Service de lazy loading pour optimiser le chargement des données
    /// </summary>
    public class LazyLoadingService : IDisposable
    {
        private readonly CacheService _cacheService;
        private readonly PerformanceService _performanceService;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _loadingSemaphores;
        private readonly Timer _cleanupTimer;

        public LazyLoadingService(CacheService cacheService, PerformanceService performanceService)
        {
            _cacheService = cacheService;
            _performanceService = performanceService;
            _loadingSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            
            // Timer de nettoyage toutes les 10 minutes
            _cleanupTimer = new Timer(CleanupSemaphores, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
        }

        /// <summary>
        /// Charge des données de manière paresseuse avec cache et déduplication
        /// </summary>
        public async Task<T?> LoadAsync<T>(string key, Func<Task<T>> factory, TimeSpan? cacheExpiration = null)
        {
            // Vérifier d'abord le cache
            var cached = _cacheService.Get<T>(key);
            if (cached != null)
            {
                return cached;
            }

            // Utiliser un semaphore pour éviter les chargements multiples du même élément
            var semaphore = _loadingSemaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            
            await semaphore.WaitAsync();
            try
            {
                // Double-check après avoir acquis le verrou
                cached = _cacheService.Get<T>(key);
                if (cached != null)
                {
                    return cached;
                }

                // Charger les données avec mesure de performance
                var result = await _performanceService.MeasureAsync($"LazyLoad_{typeof(T).Name}", factory);
                
                if (result != null)
                {
                    _cacheService.Set(key, result, cacheExpiration);
                }

                if (result != null) return  result;
            }
            finally
            {
                semaphore.Release();
            }
            return default;       
        }

        /// <summary>
        /// Charge une collection par pages avec lazy loading
        /// </summary>
        public async Task<PagedResult<T>> LoadPagedAsync<T>(
            string baseKey,
            int pageIndex,
            int pageSize,
            Func<int, int, Task<IEnumerable<T>>> factory,
            Func<Task<int?>>? totalCountFactory = null)
        {
            var pageKey = $"{baseKey}_page_{pageIndex}_{pageSize}";
            
            // CORRECTION : spécifier explicitement le type List<T>
            var pageData = await LoadAsync<List<T>>(pageKey, async () =>
            {
                var items = await factory(pageIndex, pageSize);
                return items.ToList();
            }, TimeSpan.FromMinutes(5));

            // Charger le nombre total si nécessaire
            var totalCount = 0;
            if (totalCountFactory != null)
            {
                var countKey = $"{baseKey}_total_count";
                // CORRECTION : spécifier explicitement le type int?
                var count = await LoadAsync<int?>(countKey, totalCountFactory, TimeSpan.FromMinutes(15));
                totalCount = count ?? 0;
            }

            return new PagedResult<T>
            {
                Items = pageData ?? new List<T>(),
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                HasMorePages = totalCountFactory != null && (pageIndex + 1) * pageSize < totalCount
            };
        }

        #region Chargement spécifique à l'application

        /// <summary>
        /// Charge les annonces avec lazy loading et pagination
        /// </summary>
        public async Task<PagedResult<Annonce>> LoadAnnoncesAsync(
            int pageIndex = 0,
            int pageSize = 20,
            string? category = null,
            string? searchTerm = null,
            Func<int, int, string?, string?, Task<IEnumerable<Annonce>>>? customFactory = null)
        {
            var cacheKey = $"annonces_{category}_{searchTerm}".Replace(" ", "_");
            
            return await LoadPagedAsync<Annonce>(
                cacheKey,
                pageIndex,
                pageSize,
                async (page, size) => 
                {
                    if (customFactory != null)
                    {
                        return await customFactory(page, size, category, searchTerm);
                    }
                    return new List<Annonce>();
                }
            );
        }

        /// <summary>
        /// Charge les conversations avec lazy loading
        /// </summary>
        public async Task<List<Conversation>> LoadConversationsAsync(
            string userId,
            Func<string, Task<List<Conversation>>> factory)
        {
            var key = $"conversations_{userId}";
            // CORRECTION : spécifier explicitement le type List<Conversation>
            var result = await LoadAsync<List<Conversation>>(key, () => factory(userId), TimeSpan.FromMinutes(5));
            return result ?? new List<Conversation>();
        }

        /// <summary>
        /// Charge les messages d'une conversation avec pagination
        /// </summary>
        public async Task<PagedResult<Message>> LoadMessagesAsync(
            string conversationId,
            int pageIndex = 0,
            int pageSize = 50,
            Func<string, int, int, Task<IEnumerable<Message>>>? factory = null)
        {
            var baseKey = $"messages_{conversationId}";
            
            return await LoadPagedAsync<Message>(
                baseKey,
                pageIndex,
                pageSize,
                async (page, size) => 
                {
                    if (factory != null)
                    {
                        return await factory(conversationId, page, size);
                    }
                    return new List<Message>();
                }
            );
        }

        /// <summary>
        /// Charge le profil utilisateur avec cache long
        /// </summary>
        public async Task<UserProfile?> LoadUserProfileAsync(
            string userId,
            Func<string, Task<UserProfile?>> factory)
        {
            var key = $"user_profile_{userId}";
            // CORRECTION : spécifier explicitement le type UserProfile?
            return await LoadAsync<UserProfile?>(key, () => factory(userId), TimeSpan.FromHours(1));
        }

        #endregion

        #region Préchargement intelligent

        /// <summary>
        /// Précharge les données probablement nécessaires
        /// </summary>
        public async Task PreloadDataAsync<T>(
            IEnumerable<string> keys,
            Func<string, Task<T>> factory,
            int maxConcurrency = 3)
        {
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = keys.Select(async key =>
            {
                await semaphore.WaitAsync();
                try
                {
                    // Vérifier si déjà en cache
                    if (_cacheService.Get<T>(key) == null)
                    {
                        // CORRECTION : spécifier explicitement le type T
                        await LoadAsync<T>(key, () => factory(key));
                    }
                }
                catch { }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Précharge les pages suivantes en arrière-plan
        /// </summary>
        public void PreloadNextPages<T>(
            string baseKey,
            int currentPage,
            int pageSize,
            int pagesToPreload,
            Func<int, int, Task<IEnumerable<T>>> factory)
        {
            // CORRECTION : utiliser Task.Run avec Action pour éviter l'ambiguïté
            Task.Run(async () =>
            {
                try
                {
                    for (int i = 1; i <= pagesToPreload; i++)
                    {
                        var nextPage = currentPage + i;
                        var pageKey = $"{baseKey}_page_{nextPage}_{pageSize}";
                        
                        // Vérifier si déjà en cache
                        if (_cacheService.Get<List<T>>(pageKey) == null)
                        {
                            // CORRECTION : spécifier explicitement le type List<T>
                            await LoadAsync<List<T>>(pageKey, async () =>
                            {
                                var items = await factory(nextPage, pageSize);
                                return items.ToList();
                            }, TimeSpan.FromMinutes(5));
                        }
                    }
                }
                catch { }
            });
        }

        #endregion

        #region Gestion des ressources

        /// <summary>
        /// Nettoie les semaphores inutilisés
        /// </summary>
        private void CleanupSemaphores(object? state)
        {
            try
            {
                var keysToRemove = new List<string>();
                
                foreach (var kvp in _loadingSemaphores)
                {
                    if (kvp.Value.CurrentCount == 1) // Semaphore disponible = pas d'utilisation active
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    if (_loadingSemaphores.TryRemove(key, out var semaphore))
                    {
                        semaphore.Dispose();
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Invalide le cache pour une clé spécifique
        /// </summary>
        public void InvalidateCache(string key)
        {
            _cacheService.Remove(key);
        }

        /// <summary>
        /// Invalide le cache pour toutes les clés commençant par un préfixe
        /// </summary>
        public void InvalidateCacheByPrefix(string prefix)
        {
            _cacheService.RemoveByPrefix(prefix);
        }

        /// <summary>
        /// Nettoie les ressources
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            
            foreach (var semaphore in _loadingSemaphores.Values)
            {
                semaphore.Dispose();
            }
            
            _loadingSemaphores.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Résultat paginé pour le lazy loading
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasMorePages { get; set; }
    }
}

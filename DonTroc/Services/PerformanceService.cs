using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using DonTroc.Models;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace DonTroc.Services
{
    /// <summary>
    /// Service de gestion des performances et optimisation mémoire - Version Production
    /// </summary>
    public class PerformanceService
    {
        private readonly ILogger<PerformanceService>? _logger;
        private readonly CacheService _cacheService;
        private readonly System.Threading.Timer _gcTimer;
        private readonly System.Threading.Timer _metricsTimer;
        private readonly Dictionary<string, Stopwatch> _activeOperations;
        private readonly Dictionary<string, PerformanceMetrics> _operationMetrics;
        private readonly object _lockObject = new();

        public PerformanceService(CacheService cacheService, ILogger<PerformanceService>? logger = null)
        {
            _cacheService = cacheService;
            _logger = logger;
            _activeOperations = new Dictionary<string, Stopwatch>();
            _operationMetrics = new Dictionary<string, PerformanceMetrics>();
            
            // Timer pour le garbage collection périodique - Optimisé pour production
            var gcInterval = ConfigurationService.IsProduction ? TimeSpan.FromMinutes(10) : TimeSpan.FromMinutes(5);
            _gcTimer = new System.Threading.Timer(PerformGarbageCollection, null, gcInterval, gcInterval);
            
            // Timer pour le nettoyage des métriques
            _metricsTimer = new System.Threading.Timer(CleanupMetrics, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        #region Mesure des performances

        /// <summary>
        /// Démarre la mesure d'une opération avec gestion optimisée pour la production
        /// </summary>
        public void StartOperation(string operationName)
        {
#if DEBUG
            if (!ConfigurationService.EnablePerformanceLogging) return;
            
            lock (_lockObject)
            {
                if (!_activeOperations.ContainsKey(operationName))
                {
                    _activeOperations[operationName] = Stopwatch.StartNew();
                }
            }
#endif
        }

        /// <summary>
        /// Termine la mesure d'une opération - Version optimisée production
        /// </summary>
        public void EndOperation(string operationName)
        {
#if DEBUG
            lock (_lockObject)
            {
                if (_activeOperations.TryGetValue(operationName, out var stopwatch))
                {
                    stopwatch.Stop();
                    RecordMetrics(operationName, stopwatch.ElapsedMilliseconds);
                    _activeOperations.Remove(operationName);
                }
            }
#endif
        }

        /// <summary>
        /// Enregistre les métriques uniquement si nécessaire
        /// </summary>
        private void RecordMetrics(string operationName, long elapsedMs)
        {
            if (!_operationMetrics.ContainsKey(operationName))
            {
                _operationMetrics[operationName] = new PerformanceMetrics();
            }

            var metrics = _operationMetrics[operationName];
            metrics.TotalCalls++;
            metrics.TotalTimeMs += elapsedMs;
            metrics.LastExecutionMs = elapsedMs;
            
            // Log seulement les opérations lentes en production
            if (elapsedMs > 1000 && ConfigurationService.IsProduction)
            {
                _logger?.LogWarning("Opération lente détectée: {Operation} - {Duration}ms", operationName, elapsedMs);
            }
        }

        #endregion

        #region Mesure des performances avec types génériques

        /// <summary>
        /// Mesure le temps d'exécution d'une opération asynchrone avec retour de valeur
        /// </summary>
        public async Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation)
        {
#if DEBUG
            if (!ConfigurationService.EnablePerformanceLogging) 
            {
                return await operation();
            }
            
            StartOperation(operationName);
            try
            {
                var result = await operation();
                return result;
            }
            finally
            {
                EndOperation(operationName);
            }
#else
            // En production, exécuter directement sans mesure pour les performances
            return await operation();
#endif
        }

        /// <summary>
        /// Mesure le temps d'exécution d'une opération synchrone avec retour de valeur
        /// </summary>
        public T Measure<T>(string operationName, Func<T> operation)
        {
#if DEBUG
            if (!ConfigurationService.EnablePerformanceLogging) 
            {
                return operation();
            }
            
            StartOperation(operationName);
            try
            {
                var result = operation();
                return result;
            }
            finally
            {
                EndOperation(operationName);
            }
#else
            // En production, exécuter directement sans mesure pour les performances
            return operation();
#endif
        }

        /// <summary>
        /// Mesure le temps d'exécution d'une opération asynchrone sans retour
        /// </summary>
        public async Task MeasureAsync(string operationName, Func<Task> operation)
        {
#if DEBUG
            if (!ConfigurationService.EnablePerformanceLogging) 
            {
                await operation();
                return;
            }
            
            StartOperation(operationName);
            try
            {
                await operation();
            }
            finally
            {
                EndOperation(operationName);
            }
#else
            // En production, exécuter directement sans mesure pour les performances
            await operation();
#endif
        }

        #endregion

        #region Optimisation mémoire

        /// <summary>
        /// Garbage collection optimisé pour la production
        /// </summary>
        private void PerformGarbageCollection(object? state)
        {
            try
            {
                var beforeMemory = GC.GetTotalMemory(false);
                
                // Forcer la collection uniquement si nécessaire
                if (beforeMemory > ConfigurationService.MaxMemoryThreshold)
                {
                    GC.Collect(2, GCCollectionMode.Optimized);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Optimized);
                    
                    var afterMemory = GC.GetTotalMemory(true);
                    var freedMemory = beforeMemory - afterMemory;
                    
                    if (freedMemory > 1024 * 1024) // Plus de 1MB libéré
                    {
                        _logger?.LogInformation("Mémoire libérée: {FreedMB} MB", freedMemory / (1024 * 1024));
                    }
                }
                
                // Nettoyage du cache si nécessaire
                CleanupCacheIfNeeded();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur lors du garbage collection");
            }
        }

        /// <summary>
        /// Nettoyage intelligent du cache basé sur l'utilisation mémoire
        /// </summary>
        private void CleanupCacheIfNeeded()
        {
            var currentMemory = GC.GetTotalMemory(false);
            
            if (currentMemory > ConfigurationService.MaxMemoryThreshold)
            {
                // Nettoyer 30% du cache en commençant par les éléments les moins utilisés
                _cacheService.CleanupOldEntries(0.3f);
                _logger?.LogInformation("Cache nettoyé en raison de la pression mémoire");
            }
        }

        /// <summary>
        /// Nettoyage périodique des métriques anciennes
        /// </summary>
        private void CleanupMetrics(object? state)
        {
            if (_operationMetrics.Count > 100) // Limiter à 100 métriques max
            {
                lock (_lockObject)
                {
                    var oldMetrics = _operationMetrics.Take(50).ToList();
                    foreach (var metric in oldMetrics)
                    {
                        _operationMetrics.Remove(metric.Key);
                    }
                }
            }
        }

        #endregion

        #region Optimisations réseau production

        /// <summary>
        /// Configuration HTTP optimisée pour la production
        /// </summary>
        public static void ConfigureHttpClientForProduction(HttpClient client)
        {
            // Timeouts optimisés pour la production
            client.Timeout = TimeSpan.FromSeconds(30);
            
            // Headers de performance
            client.DefaultRequestHeaders.Add("Keep-Alive", "timeout=30, max=100");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            
            // Cache control pour les ressources statiques
            client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
            {
                MaxAge = TimeSpan.FromMinutes(10),
                Public = true
            };
        }

        #endregion

        public void Dispose()
        {
            _gcTimer.Dispose();
            _metricsTimer.Dispose();
        }

        public async Task<List<Annonce>> MeasureAsync<T>(string getannonces, Func<Task<List<Annonce>>> func) // Méthode asynchrone pour mesurer les performances
        {
            StartOperation(getannonces);
            try
            {
                return await Task.Run(func);
            }
            finally
            {
                EndOperation(getannonces);
            }
        }
    }

    /// <summary>
    /// Métriques de performance simplifiées pour la production
    /// </summary>
    public class PerformanceMetrics
    {
        public int TotalCalls { get; set; }
        public long TotalTimeMs { get; set; }
        public long LastExecutionMs { get; set; }
        public double AverageTimeMs => TotalCalls > 0 ? (double)TotalTimeMs / TotalCalls : 0;
    }
}

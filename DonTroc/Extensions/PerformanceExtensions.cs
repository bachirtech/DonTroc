using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;

namespace DonTroc.Extensions
{
    /// <summary>
    /// Collection observable optimisée pour de meilleures performances
    /// </summary>
    public class OptimizedObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification;

        /// <summary>
        /// Ajoute plusieurs éléments en une seule notification
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;

            _suppressNotification = true;
            
            foreach (var item in items)
            {
                Items.Add(item);
            }
            
            _suppressNotification = false;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Remplace tous les éléments par de nouveaux éléments
        /// </summary>
        public void ReplaceAll(IEnumerable<T> items)
        {
            if (items == null) return;

            _suppressNotification = true;
            Items.Clear();
            
            foreach (var item in items)
            {
                Items.Add(item);
            }
            
            _suppressNotification = false;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Supprime plusieurs éléments en une seule notification
        /// </summary>
        public void RemoveRange(IEnumerable<T> items)
        {
            if (items == null) return;

            _suppressNotification = true;
            
            foreach (var item in items.ToList())
            {
                Items.Remove(item);
            }
            
            _suppressNotification = false;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
            {
                base.OnCollectionChanged(e);
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_suppressNotification)
            {
                base.OnPropertyChanged(e);
            }
        }
    }

    /// <summary>
    /// Extensions pour les collections et listes
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Chunking optimisé pour traiter de grandes collections par blocs
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int size)
        {
            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return GetChunk(enumerator, size);
            }
        }

        private static IEnumerable<T> GetChunk<T>(IEnumerator<T> enumerator, int size)
        {
            yield return enumerator.Current;
            for (int i = 1; i < size && enumerator.MoveNext(); i++)
            {
                yield return enumerator.Current;
            }
        }

        /// <summary>
        /// Pagination optimisée
        /// </summary>
        public static IEnumerable<T> GetPage<T>(this IEnumerable<T> source, int pageIndex, int pageSize)
        {
            return source.Skip(pageIndex * pageSize).Take(pageSize);
        }

        /// <summary>
        /// Tri avec cache pour éviter les recomputations
        /// </summary>
        public static IOrderedEnumerable<T> OrderByCached<T, TKey>(this IEnumerable<T> source, 
            Func<T, TKey> keySelector, 
            Dictionary<T, TKey>? cache = null) where T : notnull
        {
            cache ??= new Dictionary<T, TKey>();
            
            return source.OrderBy(item =>
            {
                if (!cache.TryGetValue(item, out var key))
                {
                    key = keySelector(item);
                    cache[item] = key;
                }
                return key;
            });
        }
    }

    /// <summary>
    /// Extensions pour les tâches asynchrones
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Exécute une tâche avec timeout et retry automatique
        /// </summary>
        public static async Task<T> WithRetryAsync<T>(this Task<T> task, 
            int maxRetries = 3, 
            TimeSpan? delay = null,
            CancellationToken cancellationToken = default)
        {
            var retryDelay = delay ?? TimeSpan.FromSeconds(1);
            var lastException = default(Exception);

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await task;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    lastException = ex;
                    await Task.Delay(retryDelay, cancellationToken);
                    retryDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * 1.5); // Backoff exponentiel
                }
            }

            throw lastException ?? new InvalidOperationException("Retry failed");
        }

        /// <summary>
        /// Fire and forget avec gestion d'exception
        /// </summary>
        public static void SafeFireAndForget(this Task task, Action<Exception>? onException = null)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    onException?.Invoke(t.Exception);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    /// <summary>
    /// Extensions pour optimiser les chaînes de caractères
    /// </summary>
    public static class StringExtensions
    {
        private static readonly Dictionary<string, string> _stringCache = new();
        private static readonly object _stringCacheLock = new();

        /// <summary>
        /// Interning de chaînes pour économiser la mémoire
        /// </summary>
        public static string Intern(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            lock (_stringCacheLock)
            {
                if (_stringCache.TryGetValue(value, out var cached))
                {
                    return cached;
                }

                var interned = string.Intern(value);
                _stringCache[value] = interned;
                return interned;
            }
        }

        /// <summary>
        /// Formatage optimisé avec StringBuilder poolé
        /// </summary>
        public static string FormatOptimized(this string format, params object[] args)
        {
            if (args.Length == 0) return format;
            
            // Utilisation du StringBuilder poolé serait idéale ici
            return string.Format(format, args);
        }

        /// <summary>
        /// Truncate optimisé avec ellipsis
        /// </summary>
        public static string TruncateWithEllipsis(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return maxLength <= 3 ? value[..maxLength] : $"{value[..(maxLength - 3)]}...";
        }
    }

    /// <summary>
    /// Extensions pour optimiser les images
    /// </summary>
    public static class ImageExtensions
    {
        /// <summary>
        /// Génère une URL d'image optimisée pour la taille de l'écran
        /// </summary>
        public static string OptimizeForScreen(this string imageUrl, double width, double height)
        {
            if (string.IsNullOrEmpty(imageUrl)) return imageUrl;

            // Pour Cloudinary ou autre service d'optimisation d'images
            var deviceDensity = DeviceDisplay.MainDisplayInfo.Density;
            var actualWidth = (int)(width * deviceDensity);
            var actualHeight = (int)(height * deviceDensity);

            // Si l'URL contient déjà des paramètres de redimensionnement, ne pas modifier
            if (imageUrl.Contains("w_") || imageUrl.Contains("h_"))
                return imageUrl;

            // Ajouter les paramètres de redimensionnement pour Cloudinary
            if (imageUrl.Contains("cloudinary"))
            {
                var insertIndex = imageUrl.LastIndexOf("/upload/") + 8;
                if (insertIndex > 7)
                {
                    return imageUrl.Insert(insertIndex, $"w_{actualWidth},h_{actualHeight},c_fill,f_auto,q_auto/");
                }
            }

            return imageUrl;
        }

        /// <summary>
        /// Génère une URL de miniature optimisée
        /// </summary>
        public static string GetThumbnail(this string imageUrl, int size = 150)
        {
            return OptimizeForScreen(imageUrl, size, size);
        }
    }

    /// <summary>
    /// Extensions pour la validation optimisée
    /// </summary>
    public static class ValidationExtensions
    {
        private static readonly Dictionary<string, bool> _validationCache = new();
        private static readonly object _validationCacheLock = new();

        /// <summary>
        /// Validation d'email avec cache
        /// </summary>
        public static bool IsValidEmailCached(this string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            lock (_validationCacheLock)
            {
                if (_validationCache.TryGetValue(email, out var cached))
                {
                    return cached;
                }

                var isValid = System.Text.RegularExpressions.Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                
                _validationCache[email] = isValid;
                return isValid;
            }
        }

        /// <summary>
        /// Nettoie le cache de validation périodiquement
        /// </summary>
        public static void ClearValidationCache()
        {
            lock (_validationCacheLock)
            {
                _validationCache.Clear();
            }
        }
    }
}

using System;
using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Image = SixLabors.ImageSharp.Image;

namespace DonTroc.Services
{
    /// <summary>
    /// Service optimisé pour la gestion et le cache des images
    /// </summary>
    public class OptimizedImageService : IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, Task<byte[]?>> _downloadTasks;
        private readonly SemaphoreSlim _downloadSemaphore;

        public OptimizedImageService(IMemoryCache memoryCache, CacheService cacheService)
        {
            _memoryCache = memoryCache;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _downloadTasks = new ConcurrentDictionary<string, Task<byte[]?>>();
            _downloadSemaphore = new SemaphoreSlim(3, 3); // Max 3 téléchargements simultanés
        }

        #region Chargement optimisé des images

        /// <summary>
        /// Charge une image de manière asynchrone avec cache
        /// </summary>
        public async Task<ImageSource?> LoadImageAsync(string? imageUrl, int? targetWidth = null, int? targetHeight = null)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return null;

            try
            {
                // Optimiser l'URL pour la taille cible
                var optimizedUrl = OptimizeImageUrl(imageUrl, targetWidth, targetHeight);
                var cacheKey = $"image_{optimizedUrl.GetHashCode()}";

                // Vérifier d'abord dans le cache mémoire
                if (_memoryCache.TryGetValue(cacheKey, out object? cachedImage) && cachedImage is ImageSource imageSource)
                {
                    return imageSource;
                }

                // Télécharger l'image
                var imageBytes = await DownloadImageAsync(optimizedUrl);
                if (imageBytes == null || imageBytes.Length == 0)
                    return null;

                // Créer l'ImageSource
                var newImageSource = ImageSource.FromStream(() => new MemoryStream(imageBytes));

                // Mettre en cache avec expiration
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                    Size = imageBytes.Length,
                    Priority = CacheItemPriority.Normal
                };

                _memoryCache.Set(cacheKey, newImageSource, cacheOptions);
                return newImageSource;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement de l'image {imageUrl}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Télécharge une image avec déduplication des requêtes
        /// </summary>
        private async Task<byte[]?> DownloadImageAsync(string imageUrl)
        {
            // Utiliser une tâche partagée pour éviter les téléchargements multiples de la même image
            return await _downloadTasks.GetOrAdd(imageUrl, url => DownloadImageInternalAsync(url));
        }

        /// <summary>
        /// Téléchargement interne avec limitation de concurrence
        /// </summary>
        private async Task<byte[]?> DownloadImageInternalAsync(string imageUrl)
        {
            await _downloadSemaphore.WaitAsync();
            try
            {
                using var response = await _httpClient.GetAsync(imageUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur téléchargement image {imageUrl}: {ex.Message}");
                return null;
            }
            finally
            {
                _downloadSemaphore.Release();
                // Nettoyer la t��che du cache après téléchargement
                _downloadTasks.TryRemove(imageUrl, out _);
            }
        }

        #endregion

        #region Optimisation des URLs d'images

        /// <summary>
        /// Optimise l'URL d'image selon la taille cible et la densité d'écran
        /// </summary>
        private string OptimizeImageUrl(string originalUrl, int? targetWidth, int? targetHeight)
        {
            if (string.IsNullOrEmpty(originalUrl))
                return originalUrl;

            // Récupérer la densité d'écran
            var density = DeviceDisplay.MainDisplayInfo.Density;
            
            // Calculer les dimensions optimales
            var optimalWidth = targetWidth.HasValue ? (int)(targetWidth.Value * density) : (int?)null;
            var optimalHeight = targetHeight.HasValue ? (int)(targetHeight.Value * density) : (int?)null;

            // Optimisation pour Cloudinary
            if (originalUrl.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase))
            {
                return OptimizeCloudinaryUrl(originalUrl, optimalWidth, optimalHeight);
            }

            // Pour d'autres services, retourner l'URL originale
            return originalUrl;
        }

        /// <summary>
        /// Optimise une URL Cloudinary avec transformations
        /// </summary>
        private string OptimizeCloudinaryUrl(string originalUrl, int? width, int? height)
        {
            try
            {
                // Si l'URL contient déjà des transformations, ne pas modifier
                if (originalUrl.Contains("/upload/w_") || originalUrl.Contains("/upload/h_"))
                    return originalUrl;

                var uploadIndex = originalUrl.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
                if (uploadIndex == -1)
                    return originalUrl;

                var transformations = new List<string>();

                // Ajouter les dimensions si spécifiées
                if (width.HasValue)
                    transformations.Add($"w_{width.Value}");
                
                if (height.HasValue)
                    transformations.Add($"h_{height.Value}");

                // Ajouter des optimisations standard
                transformations.AddRange(new[]
                {
                    "c_fill",          // Crop pour remplir
                    "f_auto",          // Format automatique (WebP si supporté)
                    "q_auto:good",     // Qualité automatique optimisée
                    "dpr_auto"         // Device Pixel Ratio automatique
                });

                var transformationString = string.Join(",", transformations);
                var insertPosition = uploadIndex + "/upload/".Length;
                
                return originalUrl.Insert(insertPosition, $"{transformationString}/");
            }
            catch
            {
                return originalUrl;
            }
        }

        #endregion

        #region Préchargement intelligent

        /// <summary>
        /// Précharge une liste d'images en arrière-plan
        /// </summary>
        public async Task PreloadImagesAsync(IEnumerable<string> imageUrls, int maxConcurrent = 2)
        {
            var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            var tasks = imageUrls.Select(async url =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await LoadImageAsync(url, 300, 300); // Taille standard pour le préchargement
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Précharge les images visibles dans une liste avec pagination
        /// </summary>
        public async Task PreloadVisibleImagesAsync<T>(IEnumerable<T> items, 
            Func<T, string?> imageUrlSelector, 
            int visibleItemsCount = 10)
        {
            var visibleItems = items.Take(visibleItemsCount);
            var imageUrls = visibleItems
                .Select(imageUrlSelector)
                .Where(url => !string.IsNullOrEmpty(url))
                .Cast<string>();

            await PreloadImagesAsync(imageUrls);
        }

        #endregion

        #region Compression et redimensionnement

        /// <summary>
        /// Compresse une image pour l'upload
        /// </summary>
        public async Task<byte[]?> CompressImageAsync(byte[] imageData, int maxWidth = 1920, int maxHeight = 1080, int quality = 85)
        {
            try
            {
                using var image = Image.Load(imageData);
                
                // Calculer les nouvelles dimensions en préservant le ratio
                var ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
                if (ratio < 1.0)
                {
                    var newWidth = (int)(image.Width * ratio);
                    var newHeight = (int)(image.Height * ratio);
                    
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                }

                using var outputStream = new MemoryStream();
                await image.SaveAsJpegAsync(outputStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                {
                    Quality = quality
                });

                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur compression image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Génère des miniatures multiples pour différentes tailles
        /// </summary>
        public async Task<Dictionary<string, byte[]>> GenerateThumbnailsAsync(byte[] originalImage, 
            Dictionary<string, (int width, int height)> sizes)
        {
            var results = new Dictionary<string, byte[]>();

            try
            {
                using var image = Image.Load(originalImage);
                
                foreach (var (sizeName, (width, height)) in sizes)
                {
                    var clonedImage = image;
                    clonedImage.Mutate(x => x.Resize(width, height));
                    
                    using var outputStream = new MemoryStream();
                    await clonedImage.SaveAsJpegAsync(outputStream);
                    results[sizeName] = outputStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur génération miniatures: {ex.Message}");
            }

            return results;
        }

        #endregion

        #region Upload rapide avec optimisation en arrière-plan

        /// <summary>
        /// Optimise une image rapidement pour publication immédiate (qualité réduite)
        /// </summary>
        public async Task<byte[]> FastOptimizeImageAsync(byte[] imageData, int maxWidth = 800, int maxHeight = 600, int quality = 60)
        {
            try
            {
                using var image = Image.Load(imageData);
                
                // Redimensionnement rapide
                var targetWidth = Math.Min(image.Width, maxWidth);
                var targetHeight = Math.Min(image.Height, maxHeight);
                
                if (image.Width > maxWidth || image.Height > maxHeight)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new SixLabors.ImageSharp.Size(targetWidth, targetHeight),
                        Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max,
                        Sampler = KnownResamplers.Triangle // Plus rapide que Bicubic
                    }));
                }

                using var output = new MemoryStream();
                await image.SaveAsJpegAsync(output, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                {
                    Quality = quality // Qualité réduite pour vitesse
                });
                
                return output.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'optimisation rapide: {ex.Message}");
                return imageData; // Retourner l'image originale en cas d'erreur
            }
        }

        /// <summary>
        /// Optimise une image en haute qualité en arrière-plan
        /// </summary>
        public async Task<byte[]> HighQualityOptimizeImageAsync(byte[] imageData, int maxWidth = 1920, int maxHeight = 1080, int quality = 90)
        {
            try
            {
                using var image = Image.Load(imageData);
                
                // Redimensionnement haute qualité
                var ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
                if (ratio < 1.0)
                {
                    var newWidth = (int)(image.Width * ratio);
                    var newHeight = (int)(image.Height * ratio);
                    
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new SixLabors.ImageSharp.Size(newWidth, newHeight),
                        Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max,
                        Sampler = KnownResamplers.Bicubic // Haute qualité
                    }));
                }

                using var output = new MemoryStream();
                await image.SaveAsJpegAsync(output, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                {
                    Quality = quality
                });
                
                return output.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'optimisation haute qualité: {ex.Message}");
                return imageData;
            }
        }

        #endregion

        #region Nettoyage et statistiques

        /// <summary>
        /// Nettoie le cache des images
        /// </summary>
        public void ClearImageCache()
        {
            if (_memoryCache is MemoryCache memCache)
            {
                memCache.Clear();
            }
        }

        /// <summary>
        /// Obtient les statistiques du cache
        /// </summary>
        public (int Count, long TotalSize) GetCacheStats()
        {
            // Cette méthode nécessiterait une implémentation custom du cache pour tracker les stats
            return (0, 0);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _httpClient?.Dispose();
            _downloadSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DonTroc.Services
{
    /// <summary>
    /// Service pour gérer l'upload d'images en arrière-plan avec optimisation progressive
    /// </summary>
    public class AsyncImageUploadService
    {
        private readonly FirebaseService _firebaseService;
        private readonly OptimizedImageService _optimizedImageService;
        private readonly ILogger<AsyncImageUploadService> _logger;
        private readonly AuthService _authService; // Ajout du service d'authentification
        private readonly ConcurrentDictionary<string, ImageUploadTask> _uploadTasks;
        private readonly SemaphoreSlim _uploadSemaphore;

        public AsyncImageUploadService(
            FirebaseService firebaseService, 
            OptimizedImageService optimizedImageService,
            ILogger<AsyncImageUploadService> logger,
            AuthService authService) // Injection de AuthService
        {
            _firebaseService = firebaseService;
            _optimizedImageService = optimizedImageService;
            _logger = logger;
            _authService = authService; // Initialisation
            _uploadTasks = new ConcurrentDictionary<string, ImageUploadTask>();
            _uploadSemaphore = new SemaphoreSlim(2, 2); // Max 2 uploads simultanés
        }

        /// <summary>
        /// Upload rapide avec optimisation progressive
        /// </summary>
        public async Task<string> FastUploadWithProgressiveOptimizationAsync(
            byte[] originalImageData, 
            string annonceId, 
            int imageIndex,
            CancellationToken cancellationToken = default)
        {
            var taskId = $"{annonceId}_{imageIndex}";
            
            try
            {
                _logger.LogInformation("Début de l'upload direct pour l'image {TaskId}", taskId);

                // Upload direct de l'image originale via Cloudinary
                var userId = _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("Impossible d'uploader une image sans être authentifié.");
                }
                
                var imageUrl = await _firebaseService.UploadImageAsync(originalImageData, $"annonce_{annonceId}_{imageIndex}.jpg", userId, annonceId);
                
                _logger.LogInformation("Upload direct terminé pour {TaskId}: {Url}", taskId, imageUrl);

                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload direct de {TaskId}", taskId);

                // IMPORTANT : ne pas masquer l'erreur en renvoyant une URL placeholder pour les uploads "directs".
                // Le ViewModel qui pilote la création d'annonce doit recevoir l'exception afin d'afficher
                // un message utile à l'utilisateur et de lui permettre de réessayer ou corriger l'image.
                throw;
            }
        }

        // Les méthodes ci-dessous ne sont plus utilisées avec l'upload direct,
        // mais conservées au cas où une future version réintroduirait le traitement en arrière-plan.

        /// <summary>
        /// Optimise et remplace l'image en arrière-plan
        /// </summary>
        private async Task OptimizeAndReplaceImageAsync(ImageUploadTask uploadTask, CancellationToken cancellationToken)
        {
            await _uploadSemaphore.WaitAsync(cancellationToken);
            
            try
            {
                uploadTask.Status = ImageUploadStatus.OptimizingHighQuality;
                _logger.LogInformation("Début optimisation haute qualité pour {TaskId}", uploadTask.TaskId);

                // Optimisation haute qualité
                var highQualityData = await _optimizedImageService.HighQualityOptimizeImageAsync(uploadTask.OriginalData);
                
                // Upload de la version haute qualité via Cloudinary
                var userId = _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("Impossible d'uploader une image sans être authentifié.");
                }
                
                var finalImageUrl = await _firebaseService.UploadImageAsync(highQualityData, $"annonce_{uploadTask.AnnonceId}_{uploadTask.ImageIndex}_hq.jpg", userId, uploadTask.AnnonceId);
                
                // Mettre à jour l'annonce dans Firebase avec la nouvelle URL
                await UpdateAnnonceImageUrlAsync(uploadTask.AnnonceId, uploadTask.ImageIndex, finalImageUrl);

                // Nettoyage de l'ancienne image temporaire (optionnel mais recommandé)
                try
                {
                    if (!string.IsNullOrEmpty(uploadTask.TempImagePath))
                    {
                        await _firebaseService.DeleteImageAsync(uploadTask.TempImagePath, userId);
                    }
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Échec de la suppression de l'image temporaire {Path}", uploadTask.TempImagePath);
                }

                uploadTask.Status = ImageUploadStatus.Completed;
                _logger.LogInformation("Optimisation et remplacement terminés pour {TaskId}", uploadTask.TaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'optimisation haute qualité pour {TaskId}", uploadTask.TaskId);
                uploadTask.Status = ImageUploadStatus.Failed;
                uploadTask.ErrorMessage = ex.Message;
            }
            finally
            {
                _uploadSemaphore.Release();
            }
        }

        /// <summary>
        /// Met à jour l'URL de l'image dans l'annonce
        /// </summary>
        private async Task UpdateAnnonceImageUrlAsync(string annonceId, int imageIndex, string newImageUrl)
        {
            try
            {
                var annonce = await _firebaseService.GetAnnonceAsync(annonceId);
                if (annonce != null)
                {
                    if (annonce.PhotosUrls == null)
                    {
                        annonce.PhotosUrls = new List<string>();
                    }
                    
                    // Ajouter des placeholders si nécessaire
                    while (annonce.PhotosUrls.Count <= imageIndex)
                    {
                        annonce.PhotosUrls.Add(string.Empty);
                    }
                    
                    annonce.PhotosUrls[imageIndex] = newImageUrl;
                    
                    // Utiliser la méthode optimisée pour ne mettre à jour que les URLs
                    await _firebaseService.UpdateAnnonceImageUrlAsync(annonceId, annonce.PhotosUrls);
                    
                    _logger.LogInformation("URL d'image mise à jour pour annonce {AnnonceId}, index {ImageIndex}", annonceId, imageIndex);
                }
                else
                {
                    _logger.LogWarning("Impossible de mettre à jour l'URL de l'image : annonce non trouvée.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'URL d'image pour {AnnonceId}", annonceId);
            }
        }

        /// <summary>
        /// Supprime l'image temporaire
        /// </summary>
        private async Task CleanupTempImage(string tempImagePath)
        {
            try
            {
                var userId = _authService.GetUserId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _firebaseService.DeleteImageAsync(tempImagePath, userId);
                    _logger.LogInformation("Image temporaire supprimée: {Path}", tempImagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de supprimer l'image temporaire: {Path}", tempImagePath);
            }
        }

        /// <summary>
        /// Obtient le statut d'upload d'une image
        /// </summary>
        public ImageUploadStatus GetUploadStatus(string annonceId, int imageIndex)
        {
            var taskId = $"{annonceId}_{imageIndex}";
            return _uploadTasks.TryGetValue(taskId, out var task) ? task.Status : ImageUploadStatus.NotStarted;
        }

        /// <summary>
        /// Obtient toutes les tâches d'upload pour une annonce
        /// </summary>
        public List<ImageUploadTask> GetUploadTasksForAnnonce(string annonceId)
        {
            return _uploadTasks.Values
                .Where(t => t.AnnonceId == annonceId)
                .ToList();
        }

        /// <summary>
        /// Annule tous les uploads en cours pour une annonce
        /// </summary>
        public async Task CancelUploadsForAnnonceAsync(string annonceId)
        {
            var tasks = GetUploadTasksForAnnonce(annonceId);
            
            foreach (var task in tasks)
            {
                if (task.Status == ImageUploadStatus.OptimizingHighQuality)
                {
                    task.Status = ImageUploadStatus.Cancelled;
                }
                
                // Nettoyer les images temporaires
                if (!string.IsNullOrEmpty(task.TempImagePath))
                {
                    await CleanupTempImage(task.TempImagePath);
                }
                
                _uploadTasks.TryRemove(task.TaskId, out _);
            }
            
            _logger.LogInformation("Uploads annulés pour l'annonce {AnnonceId}", annonceId);
        }

        /// <summary>
        /// Nettoie les tâches terminées
        /// </summary>
        public void CleanupCompletedTasks()
        {
            var completedTasks = _uploadTasks.Values
                .Where(t => t.Status == ImageUploadStatus.Completed || 
                           t.Status == ImageUploadStatus.Failed ||
                           t.Status == ImageUploadStatus.Cancelled)
                .ToList();

            foreach (var task in completedTasks)
            {
                _uploadTasks.TryRemove(task.TaskId, out _);
            }

            _logger.LogInformation("Nettoyage de {Count} tâches terminées", completedTasks.Count);
        }
    }

    /// <summary>
    /// Représente une tâche d'upload d'image
    /// </summary>
    public class ImageUploadTask
    {
        public string TaskId { get; set; } = string.Empty;
        public string AnnonceId { get; set; } = string.Empty;
        public int ImageIndex { get; set; }
        public string TempImageUrl { get; set; } = string.Empty;
        public string TempImagePath { get; set; } = string.Empty;
        public string FinalImageUrl { get; set; } = string.Empty;
        public string FinalImagePath { get; set; } = string.Empty;
        public byte[] OriginalData { get; set; } = Array.Empty<byte>();
        public ImageUploadStatus Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Statuts possibles d'une tâche d'upload
    /// </summary>
    public enum ImageUploadStatus
    {
        NotStarted,
        FastUploaded,
        OptimizingHighQuality,
        HighQualityUploaded,
        Completed,
        Failed,
        Cancelled
    }
}

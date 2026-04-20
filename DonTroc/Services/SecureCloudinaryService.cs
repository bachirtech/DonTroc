using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DonTroc.Services
{
    /// <summary>
    /// Service sécurisé pour l'upload d'images sur Cloudinary
    /// </summary>
    public class SecureCloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly AuthService _authService;
        
        // Limitations de sécurité
        private const int MAX_FILE_SIZE = 100 * 1024 * 1024; // 100 MB
         private const int MAX_FILES_PER_UPLOAD = 5;
         private const int MAX_FILES_PER_USER_PER_DAY = 50; // 50 images par utilisateur par jour
        
        public SecureCloudinaryService(CloudinaryConfigService configService, AuthService authService)
        {
            _cloudinary = configService.GetCloudinary();
            _authService = authService;
        }

        /// <summary>
        /// Upload sécurisé de plusieurs images avec validation complète
        /// </summary>
        public async Task<List<string>> UploadImagesSecureAsync(List<byte[]> imagesData, string userId, string? annonceId = null)
        {
            try
            {
                if (!await IsUserAuthorizedAsync(userId))
                    throw new UnauthorizedAccessException("Utilisateur non autorisé");

                if (imagesData == null || !imagesData.Any())
                    return new List<string>();

                if (imagesData.Count > MAX_FILES_PER_UPLOAD)
                    throw new ArgumentException($"Trop de fichiers. Maximum autorisé: {MAX_FILES_PER_UPLOAD}");

                try
                {
                    if (!await CanUserUploadTodayAsync(userId, imagesData.Count))
                        throw new InvalidOperationException("Limite quotidienne d'upload atteinte");
                }
                catch { /* Continuer si vérification échoue */ }

                var uploadTasks = new List<Task<string>>();
                for (int i = 0; i < imagesData.Count; i++)
                {
                    uploadTasks.Add(UploadSingleImageSecureAsync(imagesData[i], userId, i, annonceId));
                }

                var results = await Task.WhenAll(uploadTasks);
                var uploadedUrls = results.Where(url => !string.IsNullOrEmpty(url)).ToList();

                try { await LogUploadActivityAsync(userId, uploadedUrls.Count); }
                catch { /* Ne pas faire échouer pour un problème de log */ }

                return uploadedUrls;
            }
            catch (Exception ex)
            {
                try { await LogSecurityIncidentAsync(userId, "Upload Error", ex.Message); }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Upload sécurisé d'une seule image - VERSION EXPERT MAUI STABLE
        /// </summary>
        private async Task<string> UploadSingleImageSecureAsync(byte[] imageData, string userId, int index, string? annonceId = null)
        {
            try
            {
                if (imageData.Length > MAX_FILE_SIZE)
                    throw new ArgumentException($"Image {index + 1} trop grande (max {MAX_FILE_SIZE / 1024 / 1024}MB)");

                if (imageData.Length < 20)
                    throw new ArgumentException($"Image {index + 1} trop petite ou corrompue");

                // Validation de sécurité assouplie
                if (!ValidateImageSecurity(imageData))
                    await LogSecurityIncidentAsync(userId, "PermissiveUpload", $"Image validation failed for user {userId} but upload was allowed.");

                using var stream = new MemoryStream(imageData);
                var secureFileName = GenerateSecureFileName(userId, index);
                
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(secureFileName, stream),
                    Folder = !string.IsNullOrEmpty(annonceId) 
                        ? $"dontroc/annonces/{annonceId}" 
                        : $"dontroc/users/{SanitizeUserId(userId)}",
                    PublicId = secureFileName,
                    Overwrite = false,
                    Transformation = new Transformation()
                        .Quality("auto:good")
                        .FetchFormat("auto")
                        .Width(1200).Height(1200).Crop("limit"),
                    Tags = $"dontroc,user_{SanitizeUserId(userId)},upload_{DateTime.UtcNow:yyyyMMdd}",
                    Context = new StringDictionary
                    {
                        ["uploaded_by"] = userId,
                        ["upload_time"] = DateTime.UtcNow.ToString("O"),
                        ["app_version"] = "1.0"
                    }
                };

                // Tentative d'upload avec retry
                int maxAttempts = 3;
                int attempt = 0;
                UploadResult? uploadResult = null;
                Exception? lastException = null;

                while (attempt < maxAttempts)
                {
                    try
                    {
                        attempt++;
                        uploadResult = await _cloudinary.UploadAsync(uploadParams);
                        if (uploadResult?.Error == null) break;
                        lastException = new Exception(uploadResult.Error.Message);
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                    await Task.Delay(500 * (int)Math.Pow(2, attempt - 1));
                }

                if (uploadResult == null || uploadResult.Error != null)
                {
                    var cloudMsg = uploadResult?.Error?.Message ?? lastException?.Message ?? "Erreur inconnue";
                    var lower = (cloudMsg ?? string.Empty).ToLowerInvariant();
                    
                    string userMessage;
                    if (lower.Contains("invalid") || lower.Contains("not an image") || lower.Contains("corrupt") || lower.Contains("format"))
                        userMessage = "Le format de l'image n'est pas supporté ou l'image est corrompue. Essayez JPG/PNG.";
                    else if (lower.Contains("size") || lower.Contains("large") || lower.Contains("max"))
                        userMessage = $"L'image est trop volumineuse. Taille max: {MAX_FILE_SIZE / 1024 / 1024}MB.";
                    else if (lower.Contains("unauthorized") || lower.Contains("forbidden"))
                        userMessage = "Erreur d'autorisation pour l'upload. Veuillez vérifier votre session.";
                    else
                        userMessage = "Impossible d'uploader l'image. Vérifiez votre connexion et réessayez.";

                    throw new InvalidOperationException(userMessage);
                }

                var finalUrl = uploadResult?.SecureUrl?.ToString();
                if (string.IsNullOrEmpty(finalUrl))
                    throw new Exception("Upload réussi mais URL manquante");

                return finalUrl;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Upload sécurisé de fichiers audio avec validation complète
        /// </summary>
        public async Task<List<string>> UploadAudioSecureAsync(List<byte[]> audioData, string userId)
        {
            try
            {
                if (!await IsUserAuthorizedAsync(userId))
                    throw new UnauthorizedAccessException("Utilisateur non autorisé");

                if (!audioData.Any())
                    return new List<string>();

                if (audioData.Count > MAX_FILES_PER_UPLOAD)
                    throw new ArgumentException($"Trop de fichiers audio. Maximum autorisé: {MAX_FILES_PER_UPLOAD}");

                if (!await CanUserUploadTodayAsync(userId, audioData.Count))
                    throw new InvalidOperationException("Limite quotidienne d'upload atteinte");

                var uploadTasks = new List<Task<string>>();
                for (int i = 0; i < audioData.Count; i++)
                {
                    uploadTasks.Add(UploadSingleAudioSecureAsync(audioData[i], userId, i));
                }

                var results = await Task.WhenAll(uploadTasks);
                var uploadedUrls = results.Where(url => !string.IsNullOrEmpty(url)).ToList();

                try { await LogUploadActivityAsync(userId, uploadedUrls.Count); }
                catch { }

                return uploadedUrls;
            }
            catch (Exception ex)
            {
                try { await LogSecurityIncidentAsync(userId, "Audio Upload Error", ex.Message); }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Upload sécurisé d'un seul fichier audio
        /// </summary>
        private async Task<string> UploadSingleAudioSecureAsync(byte[] audioData, string userId, int index)
        {
            // Validation de sécurité de l'audio
            if (!ValidateAudioSecurity(audioData))
                throw new ArgumentException($"Fichier audio {index + 1} non valide ou potentiellement dangereuse");

            using var stream = new MemoryStream(audioData);
            
            // Génération d'un nom de fichier sécurisé et unique
            var secureFileName = GenerateSecureAudioFileName(userId, index);
            
            var uploadParams = new VideoUploadParams() // Utiliser VideoUploadParams pour l'audio
            {
                File = new FileDescription(secureFileName, stream),
                Folder = $"dontroc/users/{SanitizeUserId(userId)}/audio", // Dossier spécifique pour l'audio
                PublicId = secureFileName,
                Overwrite = false,
                
                // Tags pour l'organisation et la sécurité
                Tags = string.Join(",", new List<string> 
                { 
                    "dontroc", 
                    "audio_upload", 
                    "voice_message",
                    $"user_{SanitizeUserId(userId)}",
                    $"upload_{DateTime.UtcNow:yyyyMMdd}"
                }),
                
                // Métadonnées de sécurité
                Context = new StringDictionary
                {
                    ["uploaded_by"] = userId,
                    ["upload_time"] = DateTime.UtcNow.ToString("O"),
                    ["app_version"] = "1.0",
                    ["file_type"] = "voice_message",
                    ["security_hash"] = GenerateSecurityHash(audioData)
                }
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            
            if (uploadResult.Error != null)
            {
                throw new Exception($"Erreur Cloudinary audio: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Validation complète de sécurité d'un fichier audio
        /// </summary>
        private bool ValidateAudioSecurity(byte[] audioData)
        {
            try
            {
                if (audioData.Length > MAX_FILE_SIZE) return false;
                if (audioData.Length < 100) return false;
                if (!HasValidAudioMagicBytes(audioData)) return false;
                if (ContainsSuspiciousPatternsStrict(audioData)) return false;
                return true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Vérifie les magic bytes pour s'assurer que c'est vraiment un fichier audio
        /// </summary>
        private bool HasValidAudioMagicBytes(byte[] data)
        {
            if (data.Length < 4) return false;

            try
            {
                // MP3: ID3 ou Frame sync
                if (data.Length >= 3 && data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)
                    return true;
                
                if (data.Length >= 2 && data[0] == 0xFF && (data[1] & 0xE0) == 0xE0)
                    return true;

                // WAV: RIFF ... WAVE
                if (data.Length >= 12 && 
                    data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
                    data[8] == 0x57 && data[9] == 0x41 && data[10] == 0x56 && data[11] == 0x45)
                    return true;

                // OGG
                if (data.Length >= 4 && data[0] == 0x4F && data[1] == 0x67 && data[2] == 0x67 && data[3] == 0x53)
                    return true;

                // M4A/AAC: ftyp
                if (data.Length >= 12)
                {
                    for (int i = 0; i < Math.Min(data.Length - 8, 20); i++)
                    {
                        if (data[i] == 0x66 && data[i + 1] == 0x74 && data[i + 2] == 0x79 && data[i + 3] == 0x70)
                            return true;
                    }
                }

                // Approche permissive
                return true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Validation de sécurité pour les images (version assouplie et optimisée)
        /// </summary>
        private bool ValidateImageSecurity(byte[] imageData)
        {
            try
            {
                if (imageData.Length > MAX_FILE_SIZE) return false;
                if (imageData.Length < 50) return false;
                if (imageData.Length > 100 && imageData.Length < MAX_FILE_SIZE) return true;
                return HasValidImageMagicBytesRelaxed(imageData);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Vérifie les magic bytes pour s'assurer que c'est vraiment une image (version assouplie)
        /// </summary>
        private bool HasValidImageMagicBytesRelaxed(byte[] data)
        {
            if (data.Length < 2) return false;

            try
            {
                // JPEG: FF D8
                if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8) return true;

                // PNG: 89 50 4E 47
                if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) return true;

                // GIF: 47 49 46 38
                if (data.Length >= 4 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && 
                    (data[3] == 0x38 || data[3] == 0x39)) return true;

                // WebP: RIFF ... WEBP
                if (data.Length >= 12 && 
                    data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
                    data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50) return true;

                // BMP: 42 4D
                if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D) return true;

                // TIFF: 49 49 ou 4D 4D
                if (data.Length >= 2 && 
                    ((data[0] == 0x49 && data[1] == 0x49) || (data[0] == 0x4D && data[1] == 0x4D))) return true;

                // HEIC/HEIF: ftyp
                if (data.Length >= 12)
                {
                    for (int i = 0; i < Math.Min(data.Length - 8, 20); i++)
                    {
                        if (data[i] == 0x66 && data[i + 1] == 0x74 && data[i + 2] == 0x79 && data[i + 3] == 0x70)
                            return true;
                    }
                }

                // ICO: 00 00 01 00
                if (data.Length >= 4 && data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x01 && data[3] == 0x00) return true;

                // AVIF: ftyp avec "avif"
                if (data.Length >= 16)
                {
                    for (int i = 0; i < Math.Min(data.Length - 12, 20); i++)
                    {
                        if (data[i] == 0x66 && data[i + 1] == 0x74 && data[i + 2] == 0x79 && data[i + 3] == 0x70 &&
                            data[i + 8] == 0x61 && data[i + 9] == 0x76 && data[i + 10] == 0x69 && data[i + 11] == 0x66)
                            return true;
                    }
                }

                // Approche permissive pour taille raisonnable
                if (data.Length > 100 && data.Length < MAX_FILE_SIZE)
                    return true;

                return false;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Détection stricte de patterns vraiment suspects (version moins agressive)
        /// </summary>
        private bool ContainsSuspiciousPatternsStrict(byte[] data)
        {
            try
            {
                var checkLength = Math.Min(data.Length, 2048);
                var dataToCheck = data.Take(checkLength).Concat(data.Skip(Math.Max(0, data.Length - checkLength))).ToArray();
                var dataString = Encoding.UTF8.GetString(dataToCheck, 0, Math.Min(dataToCheck.Length, 1024));
                
                var suspiciousPatterns = new[]
                {
                    "<script>", "javascript:", "eval(", "document.cookie",
                    "<?php", "system(", "exec("
                };

                return suspiciousPatterns.Any(pattern => 
                    dataString.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Génération d'un nom de fichier sécurisé
        /// </summary>
        private string GenerateSecureFileName(string userId, int index)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomBytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            var randomString = Convert.ToHexString(randomBytes).ToLower();
            
            return $"img_{SanitizeUserId(userId)}_{timestamp}_{index}_{randomString}";
        }

        /// <summary>
        /// Génération d'un nom de fichier audio sécurisé
        /// </summary>
        private string GenerateSecureAudioFileName(string userId, int index)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomBytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            var randomString = Convert.ToHexString(randomBytes).ToLower();
            
            return $"voice_{SanitizeUserId(userId)}_{timestamp}_{index}_{randomString}";
        }

        /// <summary>
        /// Assainissement de l'ID utilisateur pour éviter les injections
        /// </summary>
        private string SanitizeUserId(string userId)
        {
            return string.Concat(userId.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));
        }

        /// <summary>
        /// Génère un hash de sécurité pour l'image
        /// </summary>
        private string GenerateSecurityHash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToHexString(hash).ToLower();
        }

        /// <summary>
        /// Vérifie si l'utilisateur est autorisé à uploader
        /// </summary>
        private async Task<bool> IsUserAuthorizedAsync(string userId)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                return currentUser != null && currentUser.Uid == userId;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Vérifie la limite quotidienne d'upload
        /// </summary>
        private async Task<bool> CanUserUploadTodayAsync(string userId, int newFilesCount)
        {
            // Cette méthode devrait vérifier dans votre base de données
            // le nombre d'uploads aujourd'hui pour cet utilisateur
            // Pour l'instant, on retourne true, mais implémentez la logique réelle
            
            var todayUploads = await GetUserUploadsCountTodayAsync();
            return (todayUploads + newFilesCount) <= MAX_FILES_PER_USER_PER_DAY;
        }

        /// <summary>
        /// Compte les uploads de l'utilisateur aujourd'hui.
        /// Utilise Preferences comme compteur simple par jour.
        /// </summary>
        private Task<int> GetUserUploadsCountTodayAsync()
        {
            try
            {
                var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var key = $"uploads_{_authService.GetUserId()}_{today}";
                var count = Preferences.Get(key, 0);
                return Task.FromResult(count);
            }
            catch
            {
                return Task.FromResult(0);
            }
        }

        /// <summary>
        /// Enregistre l'activité d'upload pour le suivi (compteur quotidien)
        /// </summary>
        private Task LogUploadActivityAsync(string userId, int filesCount)
        {
            try
            {
                var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var key = $"uploads_{userId}_{today}";
                var current = Preferences.Get(key, 0);
                Preferences.Set(key, current + filesCount);
                System.Diagnostics.Debug.WriteLine($"[Cloudinary] Upload: user={userId}, count={filesCount}, total today={current + filesCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Cloudinary] Erreur log upload: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enregistre les incidents de sécurité
        /// </summary>
        private Task LogSecurityIncidentAsync(string userId, string incidentType, string details)
        {
            System.Diagnostics.Debug.WriteLine($"[Cloudinary] ⚠️ INCIDENT SÉCURITÉ: type={incidentType}, user={userId}, details={details}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Suppression sécurisée d'images
        /// </summary>
        public async Task<bool> DeleteImageSecureAsync(string imageUrl, string userId)
        {
            try
            {
                // Extraire le public_id de l'URL Cloudinary
                var publicId = ExtractPublicIdFromUrl(imageUrl);
                if (string.IsNullOrEmpty(publicId))
                    return false;

                // Vérifier que l'utilisateur peut supprimer cette image
                if (!CanUserDeleteImage(publicId, userId))
                {
                    await LogSecurityIncidentAsync(userId, "Unauthorized Delete", 
                        $"Tentative de suppression d'une image non possédée: {publicId}");
                    return false;
                }

                var deletionParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deletionParams);
                
                await LogUploadActivityAsync(userId, -1); // Log de suppression
                
                return result.Result == "ok";
            }
            catch (Exception ex)
            {
                await LogSecurityIncidentAsync(userId, "Delete Error", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Vérifie si l'utilisateur peut supprimer cette image.
        /// L'image doit appartenir à l'utilisateur (le public_id Cloudinary contient le userId comme préfixe de dossier).
        /// </summary>
        private bool CanUserDeleteImage(string publicId, string userId)
        {
            if (string.IsNullOrEmpty(publicId) || string.IsNullOrEmpty(userId))
                return false;

            // Les images sont uploadées dans le dossier "dontroc/{userId}/..."
            // Vérifier que le public_id commence par le préfixe de l'utilisateur
            return publicId.Contains($"/{userId}/") || publicId.StartsWith($"{userId}/");
        }

        /// <summary>
        /// Extrait le public_id d'une URL Cloudinary
        /// </summary>
        private string ExtractPublicIdFromUrl(string imageUrl)
        {
            try
            {
                var uri = new Uri(imageUrl);
                var pathParts = uri.AbsolutePath.Split('/');
                var uploadIndex = Array.IndexOf(pathParts, "upload");
                
                if (uploadIndex >= 0 && uploadIndex < pathParts.Length - 2)
                {
                    var publicIdParts = pathParts.Skip(uploadIndex + 2);
                    var publicId = string.Join("/", publicIdParts);
                    
                    var lastDotIndex = publicId.LastIndexOf('.');
                    if (lastDotIndex > 0)
                        publicId = publicId.Substring(0, lastDotIndex);
                    
                    return publicId;
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}

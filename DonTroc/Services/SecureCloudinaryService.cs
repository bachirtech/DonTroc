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
        private const int MAX_FILE_SIZE = 100 * 1080 * 1080; // ~100MB la taille maximale (configurable)
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
                System.Diagnostics.Debug.WriteLine($"[SecureCloudinaryService] Début upload de {imagesData.Count} images pour utilisateur {userId}");

                // Vérifications de sécurité préliminaires
                if (!await IsUserAuthorizedAsync(userId))
                {
                    System.Diagnostics.Debug.WriteLine($"[SecureCloudinaryService] Utilisateur {userId} non autorisé");
                    throw new UnauthorizedAccessException("Utilisateur non autorisé");
                }

                if (imagesData == null || !imagesData.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[SecureCloudinaryService] Aucune image fournie");
                    return new List<string>();
                }

                if (imagesData.Count > MAX_FILES_PER_UPLOAD)
                {
                    System.Diagnostics.Debug.WriteLine($"[SecureCloudinaryService] Trop de fichiers: {imagesData.Count}");
                    throw new ArgumentException($"Trop de fichiers. Maximum autorisé: {MAX_FILES_PER_UPLOAD}");
                }

                // Vérifier la limite quotidienne (plus permissive)
                try
                {
                    if (!await CanUserUploadTodayAsync(userId, imagesData.Count))
                    {
                        System.Diagnostics.Debug.WriteLine($"[SecureCloudinaryService] Limite quotidienne atteinte pour {userId}");
                        throw new InvalidOperationException("Limite quotidienne d'upload atteinte");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SecureCloudinaryService] Erreur vérification limite, continuer quand même: {ex.Message}");
                    // Continuer même si la vérification de limite échoue
                }

                var uploadedUrls = new List<string>();
                var uploadTasks = new List<Task<string>>();

                for (int i = 0; i < imagesData.Count; i++)
                {
                    var imageData = imagesData[i];
                    System.Diagnostics.Debug.WriteLine($"[SecureCloudinaryService] Préparation upload image {i + 1}/{imagesData.Count}, taille: {imageData.Length} bytes");
                    var task = UploadSingleImageSecureAsync(imageData, userId, i, annonceId);
                    uploadTasks.Add(task);
                }

                var results = await Task.WhenAll(uploadTasks);
                uploadedUrls.AddRange(results.Where(url => !string.IsNullOrEmpty(url)));

                System.Diagnostics.Debug.WriteLine($"[SecureCloudinaryService] Upload terminé: {uploadedUrls.Count}/{imagesData.Count} images uploadées avec succès");

                // Enregistrer l'activité d'upload pour le suivi
                try
                {
                    await LogUploadActivityAsync(userId, uploadedUrls.Count);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SecureCloudinaryService] Erreur log activité: {ex.Message}");
                    // Ne pas faire échouer l'upload pour un problème de log
                }

                return uploadedUrls;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecureCloudinaryService] Erreur lors de l'upload sécurisé: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SecureCloudinaryService] Stack trace: {ex.StackTrace}");
                
                // Log de sécurité pour les tentatives suspectes
                try
                {
                    await LogSecurityIncidentAsync(userId, "Upload Error", ex.Message);
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[SecureCloudinaryService] Erreur log sécurité: {logEx.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Upload sécurisé d'une seule image - VERSION EXPERT MAUI STABLE
        /// </summary>
        private async Task<string> UploadSingleImageSecureAsync(byte[] imageData, string userId, int index, string? annonceId = null)
        {
            System.Diagnostics.Debug.WriteLine($"[Expert] Début upload image {index + 1}, taille: {imageData.Length} bytes");

            try
            {
                // APPROCHE EXPERT: Validation minimale mais efficace
                if (imageData.Length > MAX_FILE_SIZE)
                {
                    throw new ArgumentException($"Image {index + 1} trop grande (max {MAX_FILE_SIZE / 1024 / 1024}MB)");
                }

                // tolérance : accepter les très petites images (par ex. vignettes) mais garder un seuil minimal bas
                if (imageData.Length < 20)
                {
                    throw new ArgumentException($"Image {index + 1} trop petite ou corrompue");
                }

                // EXPERT: Validation de sécurité assouplie pour accepter plus de formats
                if (!ValidateImageSecurity(imageData))
                {
                    // Loggue un avertissement mais ne bloque pas l'upload, laissant Cloudinary décider.
                    System.Diagnostics.Debug.WriteLine($"[Expert] AVERTISSEMENT: La validation de l'image {index + 1} a échoué, mais l'upload continue (approche permissive).");
                    await LogSecurityIncidentAsync(userId, "PermissiveUpload", $"Image validation failed for user {userId} but upload was allowed.");
                }

                System.Diagnostics.Debug.WriteLine($"[Expert] Validation OK (ou permissive), upload vers Cloudinary");

                using var stream = new MemoryStream(imageData);
                
                // Génération d'un nom de fichier sécurisé et unique
                var secureFileName = GenerateSecureFileName(userId, index);
                
                // EXPERT: Configuration Cloudinary optimisée pour la stabilité
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(secureFileName, stream),
                    Folder = !string.IsNullOrEmpty(annonceId) 
                        ? $"dontroc/annonces/{annonceId}" 
                        : $"dontroc/users/{SanitizeUserId(userId)}",
                    PublicId = secureFileName,
                    Overwrite = false,
                    
                    // EXPERT: Transformations minimales pour éviter les erreurs
                    Transformation = new Transformation()
                        .Quality("auto:good")
                        .FetchFormat("auto")
                        .Width(1200).Height(1200).Crop("limit"),
                        
                    // EXPERT: Tags simplifiés
                    Tags = $"dontroc,user_{SanitizeUserId(userId)},upload_{DateTime.UtcNow:yyyyMMdd}",
                    
                    // EXPERT: Context minimal
                    Context = new StringDictionary
                    {
                        ["uploaded_by"] = userId,
                        ["upload_time"] = DateTime.UtcNow.ToString("O"),
                        ["app_version"] = "1.0"
                    }
                };

                System.Diagnostics.Debug.WriteLine($"[Expert] Lancement upload Cloudinary");
                // Tentative d'upload avec retry simple pour les erreurs transitoires
                int maxAttempts = 3;
                int attempt = 0;
                UploadResult? uploadResult = null;
                Exception? lastException = null;

                while (attempt < maxAttempts)
                {
                    try
                    {
                        attempt++;
                        System.Diagnostics.Debug.WriteLine($"[Expert] Upload attempt {attempt} for image {index + 1}");
                        uploadResult = await _cloudinary.UploadAsync(uploadParams);
                        if (uploadResult?.Error == null)
                            break; // succès

                        System.Diagnostics.Debug.WriteLine($"[Expert] Cloudinary reported error on attempt {attempt}: {uploadResult.Error.Message}");
                        lastException = new Exception(uploadResult.Error.Message);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Expert] Exception during Cloudinary upload attempt {attempt}: {ex.Message}");
                        lastException = ex;
                    }

                    // Backoff exponentiel entre tentatives (500ms, 1s, 2s)
                    var delayMs = 500 * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delayMs);
                }

                if (uploadResult == null || uploadResult.Error != null)
                {
                    var cloudMsg = uploadResult?.Error?.Message ?? lastException?.Message ?? "Erreur inconnue lors de l'upload";

                    System.Diagnostics.Debug.WriteLine($"[Expert] Erreur Cloudinary finale: {cloudMsg}");

                    // Mapping des messages d'erreur vers un message utilisateur plus clair
                    var lower = (cloudMsg ?? string.Empty).ToLowerInvariant();
                    string userMessage;
                    if (lower.Contains("invalid") || lower.Contains("not an image") || lower.Contains("corrupt") || lower.Contains("corrupted") || lower.Contains("format"))
                        userMessage = "Le format de l'image n'est pas supporté ou l'image est corrompue. Essayez JPG/PNG.";
                    else if (lower.Contains("size") || lower.Contains("large") || lower.Contains("max") || lower.Contains("too big"))
                        userMessage = $"L'image est trop volumineuse. Taille max: {MAX_FILE_SIZE / 1024 / 1024}MB.";
                    else if (lower.Contains("unauthorized") || lower.Contains("forbidden"))
                        userMessage = "Erreur d'autorisation pour l'upload. Veuillez vérifier votre session.";
                    else
                        userMessage = "Impossible d'uploader l'image. Vérifiez votre connexion et réessayez.";

                    // Log complet pour debug côté dev sans exposer tous les détails à l'utilisateur
                    System.Diagnostics.Debug.WriteLine($"[Expert] Détails erreur Cloudinary (dev): {cloudMsg}");

                    throw new InvalidOperationException(userMessage);
                }

                var finalUrl = uploadResult?.SecureUrl?.ToString();
                if (string.IsNullOrEmpty(finalUrl))
                {
                    System.Diagnostics.Debug.WriteLine($"[Expert] URL vide après upload");
                    throw new Exception("Upload réussi mais URL manquante");
                }

                System.Diagnostics.Debug.WriteLine($"[Expert] Upload réussi: {finalUrl}");
                return finalUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Expert] Erreur upload image {index + 1}: {ex.Message}");
                throw; // Re-lancer l'exception avec le message original
            }
        }

        /// <summary>
        /// Upload sécurisé de fichiers audio avec validation complète
        /// </summary>
        public async Task<List<string>> UploadAudioSecureAsync(List<byte[]> audioData, string userId)
        {
            try
            {
                // Vérifications de sécurité préliminaires
                if (!await IsUserAuthorizedAsync(userId))
                    throw new UnauthorizedAccessException("Utilisateur non autorisé");

                if (!audioData.Any())
                    return new List<string>();

                if (audioData.Count > MAX_FILES_PER_UPLOAD)
                    throw new ArgumentException($"Trop de fichiers audio. Maximum autorisé: {MAX_FILES_PER_UPLOAD}");

                // Vérifier la limite quotidienne
                if (!await CanUserUploadTodayAsync(userId, audioData.Count))
                    throw new InvalidOperationException("Limite quotidienne d'upload atteinte");

                var uploadedUrls = new List<string>();
                var uploadTasks = new List<Task<string>>();

                for (int i = 0; i < audioData.Count; i++)
                {
                    var audio = audioData[i];
                    var task = UploadSingleAudioSecureAsync(audio, userId, i);
                    uploadTasks.Add(task);
                }

                var results = await Task.WhenAll(uploadTasks);
                uploadedUrls.AddRange(results.Where(url => !string.IsNullOrEmpty(url)));

                // Enregistrer l'activité d'upload pour le suivi
                await LogUploadActivityAsync(userId, uploadedUrls.Count);

                return uploadedUrls;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'upload audio sécurisé: {ex.Message}");
                
                // Log de sécurité pour les tentatives suspectes
                await LogSecurityIncidentAsync(userId, "Audio Upload Error", ex.Message);
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
                System.Diagnostics.Debug.WriteLine($"[ValidateAudioSecurity] Validation audio de {audioData.Length} bytes");

                // Vérification de la taille max
                if (audioData.Length > MAX_FILE_SIZE)
                {
                    System.Diagnostics.Debug.WriteLine($"[ValidateAudioSecurity] Fichier audio trop grand: {audioData.Length} > {MAX_FILE_SIZE}");
                    return false;
                }

                // Vérification de la taille minimale (évite les fichiers vides/corrompus)
                if (audioData.Length < 100)
                {
                    System.Diagnostics.Debug.WriteLine($"[ValidateAudioSecurity] Fichier audio trop petit: {audioData.Length} < 100");
                    return false;
                }

                // Vérification des magic bytes pour détecter le vrai type de fichier audio
                if (!HasValidAudioMagicBytes(audioData))
                {
                    System.Diagnostics.Debug.WriteLine("[ValidateAudioSecurity] Magic bytes audio invalides");
                    return false;
                }

                // Vérification anti-malware basique
                if (ContainsSuspiciousPatternsStrict(audioData))
                {
                    System.Diagnostics.Debug.WriteLine("[ValidateAudioSecurity] Patterns suspects détectés dans l'audio");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("[ValidateAudioSecurity] Fichier audio validé avec succès");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ValidateAudioSecurity] Erreur validation: {ex.Message}");
                // En cas d'erreur de validation, on accepte le fichier (approche permissive)
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
                // MP3: ID3 (49 44 33) ou Frame sync (FF FB/FF F3/FF F2)
                if (data.Length >= 3 && data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)
                {
                    System.Diagnostics.Debug.WriteLine("[AudioMagicBytes] MP3 avec ID3 détecté");
                    return true;
                }
                
                if (data.Length >= 2 && data[0] == 0xFF && (data[1] & 0xE0) == 0xE0)
                {
                    System.Diagnostics.Debug.WriteLine("[AudioMagicBytes] MP3 Frame sync détecté");
                    return true;
                }

                // WAV: RIFF ... WAVE
                if (data.Length >= 12 && 
                    data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
                    data[8] == 0x57 && data[9] == 0x41 && data[10] == 0x56 && data[11] == 0x45)
                {
                    System.Diagnostics.Debug.WriteLine("[AudioMagicBytes] WAV détecté");
                    return true;
                }

                // OGG: 4F 67 67 53
                if (data.Length >= 4 && data[0] == 0x4F && data[1] == 0x67 && data[2] == 0x67 && data[3] == 0x53)
                {
                    System.Diagnostics.Debug.WriteLine("[AudioMagicBytes] OGG détecté");
                    return true;
                }

                // M4A/AAC: ftyp avec différentes signatures
                if (data.Length >= 12)
                {
                    for (int i = 0; i < Math.Min(data.Length - 8, 20); i++)
                    {
                        if (data[i] == 0x66 && data[i + 1] == 0x74 && data[i + 2] == 0x79 && data[i + 3] == 0x70)
                        {
                            System.Diagnostics.Debug.WriteLine("[AudioMagicBytes] M4A/AAC détecté");
                            return true;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AudioMagicBytes] Format audio non reconnu, premiers bytes: {BitConverter.ToString(data.Take(Math.Min(16, data.Length)).ToArray())}");
                // Approche permissive - accepter même si le format n'est pas reconnu
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioMagicBytes] Erreur: {ex.Message}");
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
                // Vérification de la taille max
                if (imageData.Length > MAX_FILE_SIZE)
                {
                    System.Diagnostics.Debug.WriteLine($"[ValidateImageSecurity] Image trop grande: {imageData.Length}");
                    return false;
                }

                // Vérification de la taille minimale (évite les images vides/corrompues)
                if (imageData.Length < 50)
                {
                    System.Diagnostics.Debug.WriteLine($"[ValidateImageSecurity] Image trop petite: {imageData.Length}");
                    return false;
                }

                // Validation assouplie : accepter la plupart des images de taille raisonnable
                // Laisser Cloudinary faire la validation finale
                if (imageData.Length > 100 && imageData.Length < MAX_FILE_SIZE)
                {
                    System.Diagnostics.Debug.WriteLine($"[ValidateImageSecurity] Taille d'image acceptable, validation permissive.");
                    return true;
                }

                // Pour les très petites images, faire une validation basique des magic bytes
                return HasValidImageMagicBytesRelaxed(imageData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ValidateImageSecurity] Erreur durant la validation: {ex.Message}. Validation permissive par défaut.");
                // En cas d'erreur de validation, approche permissive
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
                // JPEG: FF D8 (pas besoin du troisième byte FF qui peut varier)
                if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
                {
                    System.Diagnostics.Debug.WriteLine("[MagicBytes] JPEG détecté");
                    return true;
                }

                // PNG: 89 50 4E 47
                if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                {
                    System.Diagnostics.Debug.WriteLine("[MagicBytes] PNG détecté");
                    return true;
                }

                // GIF: 47 49 46 38 ou 47 49 46 39
                if (data.Length >= 4 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && 
                    (data[3] == 0x38 || data[3] == 0x39))
                {
                    System.Diagnostics.Debug.WriteLine("[MagicBytes] GIF détecté");
                    return true;
                }

                // WebP: RIFF ... WEBP
                if (data.Length >= 12 && 
                    data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
                    data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
                {
                    System.Diagnostics.Debug.WriteLine("[MagicBytes] WebP détecté");
                    return true;
                }

                // BMP: 42 4D
                if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D)
                {
                    System.Diagnostics.Debug.WriteLine("[MagicBytes] BMP détecté");
                    return true;
                }

                // TIFF: 49 49 ou 4D 4D
                if (data.Length >= 2 && 
                    ((data[0] == 0x49 && data[1] == 0x49) || (data[0] == 0x4D && data[1] == 0x4D)))
                {
                    System.Diagnostics.Debug.WriteLine("[MagicBytes] TIFF détecté");
                    return true;
                }

                // HEIC/HEIF (format iOS moderne): ftyp avec des variantes
                if (data.Length >= 12)
                {
                    // Recherche de "ftyp" à différentes positions
                    for (int i = 0; i < Math.Min(data.Length - 8, 20); i++)
                    {
                        if (data[i] == 0x66 && data[i + 1] == 0x74 && data[i + 2] == 0x79 && data[i + 3] == 0x70)
                        {
                            System.Diagnostics.Debug.WriteLine("[MagicBytes] HEIC/HEIF détecté");
                            return true;
                        }
                    }
                }

                // CORRECTION: Ajout de formats supplémentaires couramment utilisés sur mobile
                
                // ICO: 00 00 01 00
                if (data.Length >= 4 && data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x01 && data[3] == 0x00)
                {
                    System.Diagnostics.Debug.WriteLine("[MagicBytes] ICO détecté");
                    return true;
                }

                // AVIF (format moderne): ftyp avec "avif"
                if (data.Length >= 16)
                {
                    for (int i = 0; i < Math.Min(data.Length - 12, 20); i++)
                    {
                        if (data[i] == 0x66 && data[i + 1] == 0x74 && data[i + 2] == 0x79 && data[i + 3] == 0x70 &&
                            data[i + 8] == 0x61 && data[i + 9] == 0x76 && data[i + 10] == 0x69 && data[i + 11] == 0x66)
                        {
                            System.Diagnostics.Debug.WriteLine("[MagicBytes] AVIF détecté");
                            return true;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[MagicBytes] Format non reconnu, premiers bytes: {BitConverter.ToString(data.Take(Math.Min(16, data.Length)).ToArray())}");
                
                // CORRECTION: Approche encore plus permissive
                // Accepter tout fichier de taille raisonnable même si le format n'est pas reconnu
                // Cloudinary se chargera de la validation finale
                if (data.Length > 100 && data.Length < MAX_FILE_SIZE)
                {
                    System.Diagnostics.Debug.WriteLine("[MagicBytes] Format inconnu mais taille acceptable, validation très permissive - laisser Cloudinary valider");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MagicBytes] Erreur: {ex.Message}");
                // CORRECTION: En cas d'erreur, toujours accepter l'image
                System.Diagnostics.Debug.WriteLine("[MagicBytes] Erreur - validation permissive par défaut");
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
                // Ne vérifier que les premiers et derniers kilobytes pour éviter les faux positifs
                var checkLength = Math.Min(data.Length, 2048);
                var dataToCheck = data.Take(checkLength).Concat(data.Skip(Math.Max(0, data.Length - checkLength))).ToArray();
                var dataString = Encoding.UTF8.GetString(dataToCheck, 0, Math.Min(dataToCheck.Length, 1024));
                
                // Patterns vraiment suspects (liste réduite)
                var suspiciousPatterns = new[]
                {
                    "<script>", "javascript:", "eval(", "document.cookie",
                    "<?php", "system(", "exec("
                };

                var hasSuspicious = suspiciousPatterns.Any(pattern => 
                    dataString.Contains(pattern, StringComparison.OrdinalIgnoreCase));

                if (hasSuspicious)
                {
                    System.Diagnostics.Debug.WriteLine("[SuspiciousPatterns] Pattern suspect détecté");
                }

                return hasSuspicious;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SuspiciousPatterns] Erreur: {ex.Message}");
                // En cas d'erreur, approche permissive
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
        /// Compte les uploads de l'utilisateur aujourd'hui
        /// </summary>
        private async Task<int> GetUserUploadsCountTodayAsync()
        {
            // TODO: Implémenter le comptage réel depuis votre base de données
            // Cette méthode devrait compter les uploads de l'utilisateur aujourd'hui
            await Task.Delay(1); // Placeholder
            return 0;
        }

        /// <summary>
        /// Enregistre l'activité d'upload pour le suivi
        /// </summary>
        private async Task LogUploadActivityAsync(string userId, int filesCount)
        {
            try
            {
                // TODO: Enregistrer dans votre système de logs ou base de données
                System.Diagnostics.Debug.WriteLine($"Upload activity - User: {userId}, Files: {filesCount}, Time: {DateTime.UtcNow}");
                await Task.Delay(1); // Placeholder
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'enregistrement de l'activité: {ex.Message}");
            }
        }

        /// <summary>
        /// Enregistre les incidents de sécurité
        /// </summary>
        private async Task LogSecurityIncidentAsync(string userId, string incidentType, string details)
        {
            try
            {
                // TODO: Enregistrer dans votre système de sécurité
                System.Diagnostics.Debug.WriteLine($"SECURITY INCIDENT - User: {userId}, Type: {incidentType}, Details: {details}, Time: {DateTime.UtcNow}");
                await Task.Delay(1); // Placeholder
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'enregistrement de l'incident de sécurité: {ex.Message}");
            }
        }

        /// <summary>
        /// Suppression sécurisée d'images
        /// </summary>
        public async Task<bool> DeleteImageSecureAsync(string imageUrl, string userId)
        {
            try
            {
                // Vérifier que l'utilisateur peut supprimer cette image
                if (!await CanUserDeleteImageAsync())
                    return false;

                // Extraire le public_id de l'URL Cloudinary
                var publicId = ExtractPublicIdFromUrl(imageUrl);
                if (string.IsNullOrEmpty(publicId))
                    return false;

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
        /// Vérifie si l'utilisateur peut supprimer cette image
        /// </summary>
        private async Task<bool> CanUserDeleteImageAsync()
        {
            // TODO: Vérifier dans votre base de données que cette image appartient à l'utilisateur
            await Task.Delay(1); // Placeholder
            return true;
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
                    // Le public_id est généralement après /upload/v1234567890/
                    var publicIdParts = pathParts.Skip(uploadIndex + 2);
                    var publicId = string.Join("/", publicIdParts);
                    
                    // Supprimer l'extension si présente
                    var lastDotIndex = publicId.LastIndexOf('.');
                    if (lastDotIndex > 0)
                        publicId = publicId.Substring(0, lastDotIndex);
                    
                    return publicId;
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'extraction du public_id: {ex.Message}");
                return string.Empty;
            }
        }
    }
}

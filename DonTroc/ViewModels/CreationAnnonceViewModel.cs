// Fichier: DonTroc/ViewModels/CreationAnnonceViewModel.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace DonTroc.ViewModels;

// ViewModel pour la page de création d'annonce
public class CreationAnnonceViewModel : BaseViewModel
{
    private readonly AsyncImageUploadService _asyncImageUploadService; // Nouveau service d'upload asynchrone
    private readonly AuthService _authService; // Service pour l'authentification
    private readonly FirebaseService _firebaseService; // Service pour interagir avec Firebase
    private readonly GamificationService _gamificationService; // Service de gamification
    private readonly GeolocationService _geolocationService; // Service pour la géolocalisation

    private readonly ILogger<CreationAnnonceViewModel> _logger;

    // Liste pour stocker les données d'images originales
    private readonly List<byte[]> _originalImagesData;
    private readonly SmartNotificationService _smartNotificationService; // Service de notifications intelligentes
    private readonly ProximityNotificationService _proximityNotificationService; // Service de notifications de proximité
    private string _categorie = string.Empty;
    private string _description = string.Empty;
    private bool _isFormValid; // Champ de support pour la propriété IsFormValid
    private int _progressPercentage;
    private double _progressValue;
    private bool _showProgress;

    // Nouvelles propriétés pour l'indicateur de progression
    private string _statusMessage = "Préparation...";
    private string _titre = string.Empty;
    private string _type = string.Empty;

    // Le constructeur reçoit maintenant les nouveaux services pour l'optimisation progressive
    public CreationAnnonceViewModel(FirebaseService firebaseService, AuthService authService,
        GeolocationService geolocationService, ILogger<CreationAnnonceViewModel> logger,
        GamificationService gamificationService, SmartNotificationService smartNotificationService,
        AsyncImageUploadService asyncImageUploadService, ProximityNotificationService proximityNotificationService)
    {
        _firebaseService = firebaseService;
        _authService = authService;
        _geolocationService = geolocationService;
        _logger = logger;
        _gamificationService = gamificationService;
        _smartNotificationService = smartNotificationService;
        _asyncImageUploadService = asyncImageUploadService;
        _proximityNotificationService = proximityNotificationService;

        _logger.LogInformation("CreationAnnonceViewModel initialisé.");

        Photos = new ObservableCollection<ImageSource>();
        _originalImagesData = new List<byte[]>();

        PublierAnnonceCommand = new Command(ExecutePublierAnnonceCommand);
        SelectImageCommand = new Command(ExecuteSelectImageCommand);
        RemoveImageCommand = new Command<ImageSource>(OnRemoveImage);

        ValidateForm(); // Valide le formulaire à l'initialisation
    }

    // Collection pour l'aperçu des images dans l'UI
    public ObservableCollection<ImageSource> Photos { get; }

    // Propriété pour le titre de l'annonce
    public string Titre
    {
        get => _titre;
        set
        {
            SetProperty(ref _titre, value);
            ValidateForm();
        }
    }

    // Propriété pour la description
    public string Description
    {
        get => _description;
        set
        {
            SetProperty(ref _description, value);
            ValidateForm();
        }
    }

    // Propriété pour le type (Don/Troc)
    public string Type
    {
        get => _type;
        set
        {
            SetProperty(ref _type, value);
            ValidateForm();
        }
    }

    // Propriété pour la catégorie
    public string Categorie
    {
        get => _categorie;
        set
        {
            SetProperty(ref _categorie, value);
            ValidateForm();
        }
    }

    // Propriété pour la validation du formulaire, liée à l'UI
    public bool IsFormValid
    {
        get => _isFormValid;
        set => SetProperty(ref _isFormValid, value);
    }

    // Nouvelles propriétés pour l'indicateur de progression
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public int ProgressPercentage
    {
        get => _progressPercentage;
        set => SetProperty(ref _progressPercentage, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public bool ShowProgress
    {
        get => _showProgress;
        set => SetProperty(ref _showProgress, value);
    }

    // Commandes
    public Command PublierAnnonceCommand { get; }
    public ICommand SelectImageCommand { get; }
    public ICommand RemoveImageCommand { get; } // Nouvelle commande pour supprimer une image

    // Méthodes d'exécution pour éviter les async void
    private async void ExecutePublierAnnonceCommand()
    {
        try
        {
            await OnPublierWithDirectUpload();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la publication");
        }
    }

    private async void ExecuteSelectImageCommand()
    {
        try
        {
            await OnSelectImage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sélection d'image");
        }
    }

    /// <summary>
    /// Nouvelle méthode de publication avec upload direct
    /// </summary>
    private async Task OnPublierWithDirectUpload()
    {
        _logger.LogInformation("Début de la publication avec upload direct...");
        if (!IsFormValid)
        {
            _logger.LogWarning("Validation de la publication échouée.");
            return;
        }

        IsBusy = true;
        ShowProgress = true;
        StatusMessage = "Publication en cours...";
        UpdateProgress(0);

        try
        {
            // 1. Récupérer la localisation
            StatusMessage = "Récupération de votre position...";
            var location = await _geolocationService.GetCurrentLocationAsync();
            if (location == null)
            {
                await ShowErrorAlert("Erreur de localisation", "Impossible de récupérer votre position.");
                return;
            }

            UpdateProgress(20);

            // 2. Vérifier l'utilisateur connecté
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await ShowErrorAlert("Erreur d'authentification", "Veuillez vous reconnecter.");
                return;
            }

            // 3. Générer un ID unique pour l'annonce
            var annonceId = Guid.NewGuid().ToString();

            // 4. Upload des images avec validation renforcée
            StatusMessage = "Téléversement des images...";
            var imageUrls = new List<string>();

            if (_originalImagesData.Count > 0)
            {
                _logger.LogInformation("Upload de {Count} images pour l'annonce {AnnonceId}", _originalImagesData.Count,
                    annonceId);

                for (int i = 0; i < _originalImagesData.Count; i++)
                {
                    try
                    {
                        StatusMessage = $"Téléversement image {i + 1}/{_originalImagesData.Count}...";

                        // Tentative d'upload avec possibilité de réessayer
                        int attempt = 0;
                        const int maxAttempts = 2;
                        string? imageUrl = null;
                        Exception? lastEx = null;

                        while (attempt < maxAttempts)
                        {
                            attempt++;
                            try
                            {
                                imageUrl = await _asyncImageUploadService.FastUploadWithProgressiveOptimizationAsync(
                                    _originalImagesData[i], annonceId, i);

                                // Validation stricte de l'URL
                                if (!string.IsNullOrEmpty(imageUrl) &&
                                    Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute) &&
                                    !imageUrl.Contains("placeholder"))
                                {
                                    imageUrls.Add(imageUrl);
                                    _logger.LogInformation("Image {Index} uploadée avec succès: {Url}", i, imageUrl);
                                    lastEx = null;
                                    break; // sortie boucle d'essai
                                }

                                // Si l'URL est invalide, considérer comme une erreur
                                lastEx = new Exception($"URL d'image invalide retournée: {imageUrl}");
                            }
                            catch (Exception ex)
                            {
                                lastEx = ex;
                                _logger.LogWarning(ex, "Tentative {Attempt} échouée pour l'image {Index}", attempt, i);
                            }

                            // Si on doit réessayer, attendre un court instant
                            if (attempt < maxAttempts)
                                await Task.Delay(500);
                        }

                        // Si après les tentatives l'image n'est pas uploadée, proposer des options à l'utilisateur
                        if (lastEx != null)
                        {
                            _logger.LogError(lastEx, "Échec de l'upload pour l'image {Index}", i);

                            // Demander à l'utilisateur quoi faire : Réessayer / Supprimer / Annuler
                            var action = await ShowActionSheet(
                                $"Impossible d'uploader l'image {i + 1}",
                                "Annuler",
                                "Réessayer",
                                "Supprimer");

                            if (action == "Réessayer")
                            {
                                // Réessayer une fois de plus
                                try
                                {
                                    imageUrl =
                                        await _asyncImageUploadService.FastUploadWithProgressiveOptimizationAsync(
                                            _originalImagesData[i], annonceId, i);

                                    if (!string.IsNullOrEmpty(imageUrl) &&
                                        Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute) &&
                                        !imageUrl.Contains("placeholder"))
                                    {
                                        imageUrls.Add(imageUrl);
                                        _logger.LogInformation(
                                            "Image {Index} uploadée avec succès après réessai: {Url}", i, imageUrl);
                                    }
                                    else
                                    {
                                        throw new Exception("URL d'image invalide après réessai");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Réessai échoué pour l'image {Index}", i);
                                    await ShowErrorAlert("Erreur",
                                        $"Impossible d'uploader l'image {i + 1}: {ex.Message}");
                                    throw new Exception($"Impossible d'uploader l'image {i + 1}: {ex.Message}");
                                }
                            }
                            else if (action == "Supprimer")
                            {
                                // L'utilisateur choisit de supprimer l'image de la liste
                                _logger.LogInformation("L'utilisateur a supprimé l'image {Index} de la publication", i);
                                // Ne pas ajouter d'URL pour cette image
                                continue; // passer à l'image suivante
                            }
                            else
                            {
                                // Annuler la publication
                                _logger.LogInformation(
                                    "Publication annulée par l'utilisateur suite à l'échec d'upload de l'image {Index}",
                                    i);
                                throw new Exception($"Publication annulée : échec de l'upload de l'image {i + 1}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de l'upload de l'image {Index}", i);
                        throw new Exception($"Impossible d'uploader l'image {i + 1}: {ex.Message}");
                    }

                    UpdateProgress(20 + (i + 1) * 40 / _originalImagesData.Count);
                }

                // Vérification finale des images uploadées
                if (imageUrls.Count != _originalImagesData.Count)
                {
                    // On accepte les images supprimées par l'utilisateur, mais si aucune image n'est présente, lever une erreur
                    if (imageUrls.Count == 0 && _originalImagesData.Count > 0)
                    {
                        throw new Exception($"Aucune image n'a pu être uploadée.");
                    }
                }
            }

            UpdateProgress(60);

            // 5. Créer l'annonce avec validation des données
            StatusMessage = "Création de l'annonce...";
            var annonce = new Annonce
            {
                Id = annonceId,
                Titre = Titre.Trim(),
                Description = Description.Trim(),
                Type = Type.Trim(),
                Categorie = Categorie.Trim(),
                PhotosUrls = imageUrls, // Liste validée des URLs
                UtilisateurId = userId, // Déjà validé comme non-null
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                DateCreation = DateTime.UtcNow,
                DateCreationTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()
            };

            _logger.LogInformation("Sauvegarde de l'annonce {AnnonceId} avec {ImageCount} images", annonceId,
                imageUrls.Count);

            // 6. Sauvegarder l'annonce
            await _firebaseService.AddAnnonceAsync(annonce);
            UpdateProgress(80);

            // 7. Vérifier que l'annonce a été sauvegardée correctement
            StatusMessage = "Vérification de la publication...";
            await Task.Delay(1000); // Délai pour permettre la synchronisation

            try
            {
                var savedAnnonce = await _firebaseService.GetAnnonceAsync(annonceId);
                if (savedAnnonce == null)
                {
                    throw new Exception("L'annonce n'a pas été sauvegardée correctement");
                }

                if (imageUrls.Count > 0 && (savedAnnonce.PhotosUrls?.Count != imageUrls.Count))
                {
                    _logger.LogWarning("Problème de synchronisation des images. Tentative de correction...");

                    // Tentative de correction
                    savedAnnonce.PhotosUrls = imageUrls;
                    await _firebaseService.UpdateAnnonceAsync(annonceId, savedAnnonce);
                }

                _logger.LogInformation("Annonce {AnnonceId} vérifiée et correctement sauvegardée", annonceId);
            }
            catch (Exception verifyEx)
            {
                _logger.LogWarning("Impossible de vérifier l'annonce: {Error}", verifyEx.Message);
                // Ne pas faire échouer la publication pour un problème de vérification
            }

            // 8. Gamification et notifications
            StatusMessage = "Mise à jour de votre profil...";
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    await _gamificationService.OnUserActionAfterConfirmationAsync(userId, "annonce_created");
                    
                    // Notifier les utilisateurs à proximité (dans un rayon de 50 km)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _proximityNotificationService.NotifyNearbyUsersAsync(annonce);
                        }
                        catch (Exception notifEx)
                        {
                            _logger.LogWarning("Erreur lors des notifications de proximité: {Error}", notifEx.Message);
                        }
                    });
                }
            }
            catch (Exception gamEx)
            {
                _logger.LogWarning("Erreur lors de la gamification: {Error}", gamEx.Message);
            }

            UpdateProgress(100);
            await Task.Delay(500);

            // 9. Afficher le succès avec détails
            var successMessage = imageUrls.Count > 0
                ? $"Votre annonce a été publiée avec {imageUrls.Count} image(s)."
                : "Votre annonce a été publiée.";

            await ShowSuccessAlert("Succès", successMessage);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la publication de l'annonce");
            await ShowErrorAlert("Erreur", $"Une erreur est survenue lors de la publication: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            ShowProgress = false;
            ResetForm();
        }
    }

    /// <summary>
    /// Sélectionne une image avec optimisation rapide et synchronisation garantie
    /// </summary>
    private async Task OnSelectImage()
    {
        if (Photos.Count >= 5)
        {
            await ShowErrorAlert("Limite atteinte", "Vous ne pouvez ajouter que 5 photos.");
            return;
        }

        try
        {
            // Vérifier les permissions d'accès aux médias
            PermissionStatus status;
            try
            {
                status = await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Permissions.RequestAsync<Permissions.Photos>());
            }
            catch (Exception)
            {
                // Fallback pour les plateformes qui ne supportent pas cette permission
                status = PermissionStatus.Granted;
            }

            if (status != PermissionStatus.Granted)
            {
                await ShowErrorAlert("Permission requise",
                    "L'accès aux photos est nécessaire pour sélectionner une image.");
                return;
            }

            var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Veuillez sélectionner une photo"
            });

            if (result != null)
            {
                try
                {
                    // 1. Lire l'image une seule fois et stocker les données
                    byte[] originalImageData;
                    using (var stream = await result.OpenReadAsync())
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        originalImageData = memoryStream.ToArray();
                    }

                    // 2. Ajouter immédiatement les données originales pour garantir la disponibilité
                    _originalImagesData.Add(originalImageData);

                    // 3. Créer l'aperçu immédiat à partir des données copiées
                    var previewImageSource = ImageSource.FromStream(() => new MemoryStream(originalImageData));
                    Photos.Add(previewImageSource);

                    // 4. Obtenir l'index de l'image ajoutée pour l'optimisation
                    var imageIndex = _originalImagesData.Count - 1;

                    // 5. Optimisation en arrière-plan avec remplacement des données
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // Optimisation aggressive pour respecter les limites de Cloudinary
                            byte[] optimizedData = originalImageData; // Initialisation avec les données originales

                            // Cloudinary limite à 10MB, on vise 8MB pour la sécurité
                            const int maxFileSize = 8 * 1024 * 1024; // 8MB

                            if (originalImageData.Length <= maxFileSize)
                            {
                                optimizedData = originalImageData;
                                _logger.LogInformation("Image déjà dans la limite, taille: {Size} bytes",
                                    originalImageData.Length);
                            }
                            else
                            {
                                // Optimisation progressive jusqu'à respecter la limite
                                using var image = SixLabors.ImageSharp.Image.Load(originalImageData);
                                _logger.LogInformation("Image originale: {Width}x{Height}, taille: {Size} bytes",
                                    image.Width, image.Height, originalImageData.Length);

                                // Commencer avec une qualité plus basse pour les très grosses images
                                int quality =
                                    originalImageData.Length > 20 * 1024 * 1024 ? 50 : 75; // 50% si > 20MB, sinon 75%
                                int currentMaxDimension = 1080; // Variable locale pour éviter la capture

                                // Réduction progressive jusqu'à obtenir une taille acceptable
                                for (int attempt = 0; attempt < 5; attempt++)
                                {
                                    // Cloner l'image pour cette tentative avec le type de pixel explicite
                                    using var workingImage = image.CloneAs<SixLabors.ImageSharp.PixelFormats.Rgba32>();

                                    // Redimensionner si nécessaire
                                    if (workingImage.Width > currentMaxDimension ||
                                        workingImage.Height > currentMaxDimension)
                                    {
                                        workingImage.Mutate(x => x.Resize(new ResizeOptions
                                        {
                                            Size = new SixLabors.ImageSharp.Size(currentMaxDimension,
                                                currentMaxDimension),
                                            Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max,
                                            Sampler = KnownResamplers.Box
                                        }));
                                    }

                                    using var testStream = new MemoryStream();
                                    await workingImage.SaveAsync(testStream, new JpegEncoder { Quality = quality });
                                    var testData = testStream.ToArray();

                                    _logger.LogInformation(
                                        "Tentative {Attempt}: dimensions {Width}x{Height}, qualité {Quality}%, taille: {Size} bytes",
                                        attempt + 1, workingImage.Width, workingImage.Height, quality, testData.Length);

                                    if (testData.Length <= maxFileSize)
                                    {
                                        optimizedData = testData;
                                        _logger.LogInformation(
                                            "Optimisation réussie! Taille finale: {Size} bytes (réduction de {Reduction}%)",
                                            testData.Length,
                                            Math.Round((1 - (double)testData.Length / originalImageData.Length) * 100,
                                                1));
                                        break;
                                    }

                                    // Si encore trop gros, réduire plus agressivement
                                    if (attempt < 4)
                                    {
                                        if (quality > 30)
                                        {
                                            quality = Math.Max(30, quality - 15); // Réduire la qualité
                                        }
                                        else
                                        {
                                            currentMaxDimension =
                                                Math.Max(800,
                                                    (int)(currentMaxDimension * 0.8)); // Réduire les dimensions
                                        }
                                    }
                                    else
                                    {
                                        // Dernier recours : très aggressif
                                        workingImage.Mutate(x => x.Resize(new ResizeOptions
                                        {
                                            Size = new SixLabors.ImageSharp.Size(800, 800),
                                            Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max,
                                            Sampler = KnownResamplers.Box
                                        }));

                                        using var finalStream = new MemoryStream();
                                        await workingImage.SaveAsync(finalStream, new JpegEncoder { Quality = 30 });
                                        optimizedData = finalStream.ToArray();

                                        _logger.LogWarning("Optimisation finale très agressive: {Size} bytes",
                                            optimizedData.Length);
                                        break;
                                    }
                                }

                                // Vérification finale
                                if (optimizedData.Length > maxFileSize)
                                {
                                    _logger.LogError(
                                        "Impossible d'optimiser l'image sous {MaxSize} bytes. Taille finale: {FinalSize} bytes",
                                        maxFileSize, optimizedData.Length);
                                    // En dernier recours, utiliser l'image originale et laisser Cloudinary gérer l'erreur
                                    optimizedData = originalImageData;
                                }
                            }

                            // Remplacer les données en arrière-plan si l'optimisation a donné de meilleurs résultats
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                try
                                {
                                    // Vérifier que l'index est toujours valide
                                    if (imageIndex < _originalImagesData.Count)
                                    {
                                        _originalImagesData[imageIndex] = optimizedData;
                                        _logger.LogInformation("Données d'image mises à jour à l'index {Index}",
                                            imageIndex);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Erreur lors du remplacement des données optimisées");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Erreur lors de l'optimisation en arrière-plan - les données originales seront utilisées");
                            // En cas d'erreur, les données originales restent dans _originalImagesData
                        }
                    });

                    ValidateForm();
                    _logger.LogInformation("Image ajoutée. Total: {PhotoCount} photos, {DataCount} données",
                        Photos.Count, _originalImagesData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de l'affichage de l'image");
                    await ShowErrorAlert("Erreur", "Impossible d'afficher l'image sélectionnée.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sélection de l'image");
            await ShowErrorAlert("Erreur", "Impossible de sélectionner l'image. Veuillez réessayer.");
        }
    }

    /// <summary>
    /// Méthodes utilitaires pour l'affichage des dialogues
    /// </summary>
    private async Task ShowErrorAlert(string title, string message)
    {
        var currentPage = Application.Current?.MainPage;
        if (currentPage != null)
        {
            await currentPage.DisplayAlert(title, message, "OK");
        }
    }

    private async Task ShowSuccessAlert(string title, string message)
    {
        var currentPage = Application.Current?.MainPage;
        if (currentPage != null)
        {
            await currentPage.DisplayAlert(title, message, "OK");
        }
    }

    private async Task<string?> ShowActionSheet(string title, string cancel, params string[] buttons)
    {
        var currentPage = Application.Current?.MainPage;
        if (currentPage != null)
        {
            return await currentPage.DisplayActionSheet(title, cancel, null, buttons);
        }

        return cancel;
    }

    // Supprime une image de la liste
    private void OnRemoveImage(ImageSource imageSource)
    {
        var index = Photos.IndexOf(imageSource);
        if (index != -1)
        {
            Photos.RemoveAt(index);
            _originalImagesData.RemoveAt(index);
            ValidateForm();
        }
    }

    // Valide le formulaire
    private void ValidateForm()
    {
        IsFormValid = !string.IsNullOrWhiteSpace(Titre) &&
                      !string.IsNullOrWhiteSpace(Description) &&
                      !string.IsNullOrWhiteSpace(Type) &&
                      !string.IsNullOrWhiteSpace(Categorie) &&
                      Photos.Count > 0;
    }

    // Réinitialise le formulaire après publication
    private void ResetForm()
    {
        Titre = string.Empty;
        Description = string.Empty;
        Type = string.Empty;
        Categorie = string.Empty;
        Photos.Clear();
        _originalImagesData.Clear();
        UpdateProgress(0);
        StatusMessage = "Préparation...";
    }

    private void UpdateProgress(int percentage)
    {
        ProgressPercentage = percentage;
        ProgressValue = percentage / 100.0;
    }
}
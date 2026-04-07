using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel pour l'affichage des images en plein écran avec navigation
/// </summary>
public class ImageViewerViewModel : BaseViewModel, IQueryAttributable
{
    private ObservableCollection<string> _images;
    private int _currentImageIndex;
    private string _imageSource = string.Empty;
    private readonly ILogger<ImageViewerViewModel>? _logger;

    /// <summary>
    /// Collection des URLs d'images à afficher
    /// </summary>
    public ObservableCollection<string> Images
    {
        get => _images;
        set
        {
            _images = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMultipleImages));
            OnPropertyChanged(nameof(TotalImages));
        }
    }

    /// <summary>
    /// Index de l'image actuellement affichée
    /// </summary>
    public int CurrentImageIndex
    {
        get => _currentImageIndex;
        set
        {
            _currentImageIndex = value;
            OnPropertyChanged();
            UpdateCurrentImage();
        }
    }

    /// <summary>
    /// Source de l'image actuellement affichée
    /// </summary>
    public string ImageSource
    {
        get => _imageSource;
        set
        {
            _imageSource = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Indique s'il y a plusieurs images à afficher
    /// </summary>
    public bool HasMultipleImages => Images?.Count > 1;

    /// <summary>
    /// Nombre total d'images
    /// </summary>
    public int TotalImages => Images?.Count ?? 0;

    /// <summary>
    /// Index affiché (base 1 pour l'utilisateur)
    /// </summary>
    public int DisplayIndex => CurrentImageIndex + 1;

    /// <summary>
    /// Commande pour passer à l'image précédente
    /// </summary>
    public ICommand PreviousImageCommand { get; }

    /// <summary>
    /// Commande pour passer à l'image suivante
    /// </summary>
    public ICommand NextImageCommand { get; }

    public ImageViewerViewModel(ILogger<ImageViewerViewModel>? logger = null)
    {
        _logger = logger;
        _images = new ObservableCollection<string>();
        
        // Initialisation des commandes
        PreviousImageCommand = new Command(ExecutePreviousImage, CanExecutePreviousImage);
        NextImageCommand = new Command(ExecuteNextImage, CanExecuteNextImage);

        _logger?.LogInformation("ImageViewerViewModel initialisé");
    }

    /// <summary>
    /// Initialise le viewer avec une liste d'images et un index de départ
    /// </summary>
    /// <param name="images">Liste des URLs d'images</param>
    /// <param name="startIndex">Index de l'image à afficher en premier</param>
    public void Initialize(List<string> images, int startIndex = 0)
    {
        try
        {
            // Filtrer les URLs valides
            var validImages = images?.Where(url => !string.IsNullOrEmpty(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute)).ToList() ?? new List<string>();
            
            Images = new ObservableCollection<string>(validImages);
            
            if (Images.Count == 0)
            {
                _logger?.LogWarning("Aucune image valide fournie au viewer");
                // Ajouter une image par défaut si aucune image valide
                Images.Add("https://via.placeholder.com/400x300?text=Aucune+image");
            }
            
            CurrentImageIndex = Math.Max(0, Math.Min(startIndex, Images.Count - 1));
            _logger?.LogInformation("ImageViewer initialisé avec {Count} images, index de départ: {StartIndex}", Images.Count, CurrentImageIndex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'initialisation du viewer d'images");
            // Fallback sécurisé
            Images = new ObservableCollection<string> { "https://via.placeholder.com/400x300?text=Erreur+chargement" };
            CurrentImageIndex = 0;
        }
    }

    /// <summary>
    /// Méthode appelée automatiquement par MAUI pour recevoir les paramètres de navigation
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            _logger?.LogInformation("Application des paramètres de navigation: {Params}", string.Join(", ", query.Keys));
            
            // CORRECTION: Gestion améliorée des paramètres d'image
            if (query.ContainsKey("imageUrls"))
            {
                var imageUrlsParam = query["imageUrls"];
                _logger?.LogInformation("Paramètre imageUrls reçu: {Param} (Type: {Type})", imageUrlsParam, imageUrlsParam?.GetType().Name);
                
                List<string> imageUrls = new List<string>();
                
                if (imageUrlsParam is string singleUrl)
                {
                    // Cas d'une seule URL ou plusieurs URLs séparées
                    if (!string.IsNullOrEmpty(singleUrl))
                    {
                        // Séparer les URLs multiples par | ou , puis décoder chacune
                        var rawUrls = singleUrl.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (var rawUrl in rawUrls)
                        {
                            var trimmedUrl = rawUrl.Trim();
                            string decodedUrl;
                            
                            // Décoder l'URL si elle contient des caractères encodés
                            try
                            {
                                decodedUrl = Uri.UnescapeDataString(trimmedUrl);
                            }
                            catch
                            {
                                decodedUrl = trimmedUrl;
                            }
                            
                            if (!string.IsNullOrEmpty(decodedUrl))
                            {
                                imageUrls.Add(decodedUrl);
                                _logger?.LogInformation("URL décodée ajoutée: {Url}", decodedUrl);
                            }
                        }
                    }
                }
                else if (imageUrlsParam is IEnumerable<string> urlList)
                {
                    // Cas d'une liste d'URLs
                    imageUrls = urlList.Where(url => !string.IsNullOrEmpty(url)).ToList();
                }
                
                _logger?.LogInformation("URLs d'images finales: {Count} images", imageUrls.Count);
                
                Initialize(imageUrls, 0);
            }
            // CORRECTION: Support alternatif pour le paramètre "ImageUrl" (fallback)
            else if (query.ContainsKey("ImageUrl"))
            {
                var imageUrl = query["ImageUrl"]?.ToString();
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Décoder l'URL si nécessaire
                    try
                    {
                        imageUrl = Uri.UnescapeDataString(imageUrl);
                    }
                    catch { }
                    
                    _logger?.LogInformation("URL d'image alternative reçue: {Url}", imageUrl);
                    Initialize(new List<string> { imageUrl }, 0);
                }
            }
            else
            {
                _logger?.LogWarning("Aucun paramètre d'image trouvé dans les paramètres de navigation");
                Initialize(new List<string>(), 0);
            }
            
            // Support pour passer un index de départ
            if (query.ContainsKey("startIndex") && int.TryParse(query["startIndex"]?.ToString(), out int startIndex))
            {
                CurrentImageIndex = Math.Max(0, Math.Min(startIndex, Images?.Count - 1 ?? 0));
                _logger?.LogInformation("Index de départ défini à: {StartIndex}", CurrentImageIndex);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'application des paramètres de navigation");
            // Fallback sécurisé
            Initialize(new List<string>(), 0);
        }
    }

    /// <summary>
    /// Met à jour l'image courante basée sur l'index
    /// </summary>
    private void UpdateCurrentImage()
    {
        try
        {
            if (Images != null && CurrentImageIndex >= 0 && CurrentImageIndex < Images.Count)
            {
                ImageSource = Images[CurrentImageIndex];
                _logger?.LogDebug("Image mise à jour: index {Index}, URL: {Url}", CurrentImageIndex, ImageSource);
            }
            else
            {
                _logger?.LogWarning("Index d'image invalide: {Index}, nombre d'images: {Count}", CurrentImageIndex, Images?.Count ?? 0);
            }

            // Notifier les changements pour les commandes
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ((Command)PreviousImageCommand).ChangeCanExecute();
                ((Command)NextImageCommand).ChangeCanExecute();
                OnPropertyChanged(nameof(DisplayIndex));
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la mise à jour de l'image courante");
        }
    }

    /// <summary>
    /// Passe à l'image précédente
    /// </summary>
    private void ExecutePreviousImage()
    {
        try
        {
            if (CanExecutePreviousImage())
            {
                CurrentImageIndex--;
                _logger?.LogDebug("Navigation vers l'image précédente: {Index}", CurrentImageIndex);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la navigation vers l'image précédente");
        }
    }

    /// <summary>
    /// Vérifie si on peut passer à l'image précédente
    /// </summary>
    private bool CanExecutePreviousImage()
    {
        return CurrentImageIndex > 0;
    }

    /// <summary>
    /// Passe à l'image suivante
    /// </summary>
    private void ExecuteNextImage()
    {
        try
        {
            if (CanExecuteNextImage())
            {
                CurrentImageIndex++;
                _logger?.LogDebug("Navigation vers l'image suivante: {Index}", CurrentImageIndex);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de la navigation vers l'image suivante");
        }
    }

    /// <summary>
    /// Vérifie si on peut passer à l'image suivante
    /// </summary>
    private bool CanExecuteNextImage()
    {
        return Images != null && CurrentImageIndex < Images.Count - 1;
    }
}

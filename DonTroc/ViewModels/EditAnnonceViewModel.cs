// Fichier: DonTroc/ViewModels/EditAnnonceViewModel.cs

using System;
using System.Collections.Generic;
using DonTroc.Models;
using DonTroc.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace DonTroc.ViewModels;

// ViewModel pour la page de modification d'annonce. 
// IQueryAttributable permet de recevoir des données lors de la navigation.
public class EditAnnonceViewModel : BaseViewModel, IQueryAttributable
{
    private readonly FirebaseService _firebaseService;
    private readonly List<FileResult> _newlySelectedPhotos = new();
    private Annonce _annonceOriginale;
    private string _categorie;
    private string _description;

    private string _titre;
    private string _type;


    // Constructeur du ViewModel corrigé pour l'injection de dépendances
    public EditAnnonceViewModel(FirebaseService firebaseService)
    {
        _firebaseService = firebaseService;

        // Initialisation avec des valeurs par défaut
        _annonceOriginale = new Annonce();
        _titre = string.Empty;
        _description = string.Empty;
        _type = string.Empty;
        _categorie = string.Empty;

        Photos = new ObservableCollection<ImageSource>();
        UpdateAnnonceCommand = new Command(async () => await OnUpdateAnnonce());
        SelectImageCommand = new Command(async () => await OnSelectImage());
        RemoveImageCommand = new Command<ImageSource>(OnRemoveImage); // Initialisation de la commande de suppression
        CancelCommand = new Command(OnCancel);
    }

    public ObservableCollection<ImageSource> Photos { get; }

    public string Titre
    {
        get => _titre;
        set
        {
            SetProperty(ref _titre, value);
            OnPropertyChanged(nameof(IsFormValid));
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            SetProperty(ref _description, value);
            OnPropertyChanged(nameof(IsFormValid));
        }
    }

    public string Type
    {
        get => _type;
        set
        {
            SetProperty(ref _type, value);
            OnPropertyChanged(nameof(IsFormValid));
        }
    }

    public string Categorie
    {
        get => _categorie;
        set
        {
            SetProperty(ref _categorie, value);
            OnPropertyChanged(nameof(IsFormValid));
        }
    }

    /// <summary>
    /// Propriété qui indique si le formulaire est valide pour activer le bouton de sauvegarde
    /// </summary>
    public bool IsFormValid =>
        !string.IsNullOrWhiteSpace(Titre) &&
        !string.IsNullOrWhiteSpace(Description) &&
        !string.IsNullOrWhiteSpace(Type) &&
        !string.IsNullOrWhiteSpace(Categorie);

    public ICommand UpdateAnnonceCommand { get; }
    public ICommand SelectImageCommand { get; }
    public ICommand RemoveImageCommand { get; } // Nouvelle commande pour supprimer une image
    public ICommand CancelCommand { get; }

    // Cette méthode est appelée automatiquement lors de la navigation vers cette page
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Récupère l'annonce passée en paramètre, la désérialise et remplit les champs
        if (query.TryGetValue("annonce", out var annonceJson))
        {
            _annonceOriginale = JsonSerializer.Deserialize<Annonce>(annonceJson.ToString());
            Titre = _annonceOriginale!.Titre;
            Description = _annonceOriginale.Description;
            Type = _annonceOriginale.Type;
            Categorie = _annonceOriginale.Categorie;

            // Charge les photos existantes pour l'aperçu
            foreach (var photoUrl in _annonceOriginale.PhotosUrls)
            {
                Photos.Add(ImageSource.FromUri(new Uri(photoUrl)));
            }
        }
    }

    private async Task OnSelectImage() // Méthode pour sélectionner une nouvelle image
    {
        if (Photos.Count >= 5)
        {
            if (Application.Current == null) return;
            if (Application.Current.MainPage != null)
                await Application.Current.MainPage.DisplayAlert("Limite atteinte",
                    "Vous ne pouvez avoir que 5 photos maximum.", "OK");
            return;
        }

        var result = await MediaPicker.PickPhotoAsync();
        if (result != null)
        {
            _newlySelectedPhotos.Add(result);
            Photos.Add(ImageSource.FromStream(() => result.OpenReadAsync().Result));
        }
    }

    private void OnRemoveImage(ImageSource image) // Méthode pour supprimer une image
    {
        // Recherche l'URL de l'image à partir de l'ImageSource
        var urlToRemove = string.Empty;
        foreach (var url in _annonceOriginale.PhotosUrls)
        {
            if (ImageSource.FromUri(new Uri(url)).ToString() == image.ToString())
            {
                urlToRemove = url;
                break;
            }
        }

        if (!string.IsNullOrEmpty(urlToRemove))
        {
            // Supprime l'URL de l'image de la liste
            _annonceOriginale.PhotosUrls.Remove(urlToRemove);

            // Supprime également l'image de l'ObservableCollection
            Photos.Remove(image);
        }
    }

    private async Task OnUpdateAnnonce() // Méthode pour mettre à jour l'annonce
    {
        if (_annonceOriginale == null) return;

        IsBusy = true;

        try
        {
            var updatedPhotoUrls = new List<string>(_annonceOriginale.PhotosUrls);

            // Créer une copie de la collection pour éviter les modifications concurrentes
            var photosToUpload = _newlySelectedPhotos.ToList();

            // Envoie uniquement les nouvelles photos
            foreach (var photoResult in photosToUpload)
            {
                await using var stream = await photoResult.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var imageData = memoryStream.ToArray();

                var fileName = $"{_annonceOriginale.UtilisateurId}_{Guid.NewGuid()}.jpg";
                var userId = _annonceOriginale.UtilisateurId; // Récupérer l'ID de l'utilisateur
                var imageUrl = await _firebaseService.UploadImageAsync(imageData, fileName, userId);
                if (!string.IsNullOrEmpty(imageUrl))
                    updatedPhotoUrls.Add(imageUrl);
            }

            // Crée un nouvel objet Annonce avec les données mises à jour
            var annonceMiseAJour = new Annonce
            {
                Id = _annonceOriginale.Id,
                Titre = Titre,
                Description = Description,
                Type = Type,
                Categorie = Categorie,
                UtilisateurId = _annonceOriginale.UtilisateurId, // Conserve l'ID de l'utilisateur original
                DateCreation = _annonceOriginale.DateCreation, // Conserve la date de création originale
                PhotosUrls = updatedPhotoUrls, // Utilise la liste combinée d'URLs
                Latitude = _annonceOriginale.Latitude, // Conserve la position
                Longitude = _annonceOriginale.Longitude
            };

            // Met à jour l'annonce dans Firebase
            await _firebaseService.UpdateAnnonceAsync(annonceMiseAJour.Id, annonceMiseAJour);

            // Vide la liste des nouvelles photos après upload
            _newlySelectedPhotos.Clear();

            await Shell.Current.DisplayAlert("Succès", "Annonce mise à jour avec succès !", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Erreur", $"Erreur lors de la mise à jour : {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void OnCancel() // Méthode pour annuler la modification et revenir en arrière
    {
        // Affiche une boîte de dialogue de confirmation
        var result = await Shell.Current.DisplayAlert("Annuler", "Êtes-vous sûr de vouloir annuler les modifications ?",
            "Oui", "Non");
        if (result)
        {
            // Retourne simplement à la page précédente
            await Shell.Current.GoToAsync("..");
        }
    }
}
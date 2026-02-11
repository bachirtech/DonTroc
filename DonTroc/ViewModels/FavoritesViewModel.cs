using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel pour gérer l'affichage et l'interaction avec les favoris
/// </summary>
public class FavoritesViewModel : BaseViewModel
{
    private readonly FavoritesService _favoritesService;
    private readonly AuthService _authService;
    
    // Collections observables pour l'interface
    public ObservableCollection<Favorite> Favorites { get; } = new();
    public ObservableCollection<FavoriteList> UserLists { get; } = new();
    public ObservableCollection<AnnonceAlert> UserAlerts { get; } = new();
    
    // Propriétés pour l'interface
    private FavoriteList? _selectedList;
    public FavoriteList? SelectedList
    {
        get => _selectedList;
        set
        {
            SetProperty(ref _selectedList, value);
            _ = LoadFavoritesForSelectedListAsync();
        }
    }
    
    private bool _showCreateListDialog;
    public bool ShowCreateListDialog
    {
        get => _showCreateListDialog;
        set => SetProperty(ref _showCreateListDialog, value);
    }
    
    private bool _showCreateAlertDialog;
    public bool ShowCreateAlertDialog
    {
        get => _showCreateAlertDialog;
        set => SetProperty(ref _showCreateAlertDialog, value);
    }
    
    // Propriétés pour la création de nouvelle liste
    private string _newListName = string.Empty;
    public string NewListName
    {
        get => _newListName;
        set => SetProperty(ref _newListName, value);
    }
    
    private string _newListDescription = string.Empty;
    public string NewListDescription
    {
        get => _newListDescription;
        set => SetProperty(ref _newListDescription, value);
    }
    
    private string _newListColor = "#FF6B6B";
    public string NewListColor
    {
        get => _newListColor;
        set => SetProperty(ref _newListColor, value);
    }
    
    // Propriétés pour la création d'alerte
    private string _newAlertName = string.Empty;
    public string NewAlertName
    {
        get => _newAlertName;
        set => SetProperty(ref _newAlertName, value);
    }
    
    private string _newAlertKeywords = string.Empty;
    public string NewAlertKeywords
    {
        get => _newAlertKeywords;
        set => SetProperty(ref _newAlertKeywords, value);
    }
    
    private string _newAlertLocation = string.Empty;
    public string NewAlertLocation
    {
        get => _newAlertLocation;
        set => SetProperty(ref _newAlertLocation, value);
    }
    
    // Commandes
    public ICommand LoadDataCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand ShowCreateListCommand { get; }
    public ICommand CreateListCommand { get; }
    public ICommand ShowCreateAlertCommand { get; }
    public ICommand CreateAlertCommand { get; }
    public ICommand DeleteFavoriteCommand { get; }
    public ICommand DeleteListCommand { get; }
    public ICommand DeleteAlertCommand { get; }
    public ICommand NavigateToAnnonceCommand { get; }

    public FavoritesViewModel(FavoritesService favoritesService, AuthService authService)
    {
        _favoritesService = favoritesService;
        _authService = authService;
        
        LoadDataCommand = new Command(async () => await LoadDataAsync());
        ToggleFavoriteCommand = new Command<Annonce>(async (annonce) => await ToggleFavoriteAsync(annonce));
        ShowCreateListCommand = new Command(() => ShowCreateListDialog = true);
        CreateListCommand = new Command(async () => await CreateListAsync());
        ShowCreateAlertCommand = new Command(() => ShowCreateAlertDialog = true);
        CreateAlertCommand = new Command(async () => await CreateAlertAsync());
        DeleteFavoriteCommand = new Command<Favorite>(async (favorite) => await DeleteFavoriteAsync(favorite));
        DeleteListCommand = new Command<FavoriteList>(async (list) => await DeleteListAsync(list));
        DeleteAlertCommand = new Command<AnnonceAlert>(async (alert) => await DeleteAlertAsync(alert));
        NavigateToAnnonceCommand = new Command<Favorite>(async (favorite) => await NavigateToAnnonceAsync(favorite));
    }
    
    /// <summary>
    /// Charge toutes les données (favoris, listes, alertes)
    /// </summary>
    public async Task LoadDataAsync()
    {
        if (IsBusy) return;
        var userId = _authService.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Shell.Current.DisplayAlert("Erreur", "Vous devez être connecté pour accéder à vos favoris.", "OK");
            return;
        }
        IsBusy = true;
        
        try
        {
            // Charger les listes
            var lists = await _favoritesService.GetUserListsAsync();
            UserLists.Clear();
            foreach (var list in lists)
            {
                UserLists.Add(list);
            }
            
            // Sélectionner la première liste par défaut
            if (UserLists.Any() && SelectedList == null)
            {
                SelectedList = UserLists.First();
            }
            
            // Charger les favoris pour la liste sélectionnée
            await LoadFavoritesForSelectedListAsync();
            
            // Charger les alertes
            var alerts = await _favoritesService.GetUserAlertsAsync();
            UserAlerts.Clear();
            foreach (var alert in alerts)
            {
                UserAlerts.Add(alert);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesViewModel] Erreur lors du chargement: {ex.Message}");
            await Shell.Current.DisplayAlert("Erreur", "Impossible de charger les favoris", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Charge les favoris pour la liste sélectionnée
    /// </summary>
    private async Task LoadFavoritesForSelectedListAsync()
    {
        var userId = _authService.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            await Shell.Current.DisplayAlert("Erreur", "Vous devez être connecté pour accéder à vos favoris.", "OK");
            return;
        }
        try
        {
            var listName = SelectedList?.Name;
            var favorites = await _favoritesService.GetUserFavoritesAsync(listName);
            
            Favorites.Clear();
            foreach (var favorite in favorites.OrderByDescending(f => f.DateAdded))
            {
                Favorites.Add(favorite);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesViewModel] Erreur lors du chargement des favoris: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Ajoute ou supprime une annonce des favoris
    /// </summary>
    public async Task ToggleFavoriteAsync(Annonce annonce)
    {
        try
        {
            var isFavorite = await _favoritesService.IsFavoriteAsync(annonce.Id);
            
            bool success;
            if (isFavorite)
            {
                success = await _favoritesService.RemoveFromFavoritesAsync(annonce.Id);
                if (success)
                {
                    var favoriteToRemove = Favorites.FirstOrDefault(f => f.AnnonceId == annonce.Id);
                    if (favoriteToRemove != null)
                    {
                        Favorites.Remove(favoriteToRemove);
                    }
                }
            }
            else
            {
                // Demander à l'utilisateur de choisir une liste
                var selectedListName = await PromptForListSelectionAsync();
                if (selectedListName != null)
                {
                    success = await _favoritesService.AddToFavoritesAsync(annonce, selectedListName);
                    if (success)
                    {
                        await LoadFavoritesForSelectedListAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesViewModel] Erreur lors du toggle favori: {ex.Message}");
            await Shell.Current.DisplayAlert("Erreur", "Impossible de modifier les favoris", "OK");
        }
    }
    
    /// <summary>
    /// Crée une nouvelle liste personnalisée
    /// </summary>
    private async Task CreateListAsync()
    {
        if (string.IsNullOrWhiteSpace(NewListName))
        {
            await Shell.Current.DisplayAlert("Erreur", "Veuillez saisir un nom pour la liste", "OK");
            return;
        }
        
        try
        {
            var success = await _favoritesService.CreateListAsync(
                NewListName, 
                NewListDescription, 
                NewListColor
            );
            
            if (success)
            {
                // Recharger les listes
                await LoadDataAsync();
                
                // Réinitialiser le formulaire
                NewListName = string.Empty;
                NewListDescription = string.Empty;
                NewListColor = "#FF6B6B";
                ShowCreateListDialog = false;
                
                await Shell.Current.DisplayAlert("Succès", "Liste créée avec succès", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Erreur", "Impossible de créer la liste", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesViewModel] Erreur lors de la création de liste: {ex.Message}");
            await Shell.Current.DisplayAlert("Erreur", "Une erreur est survenue", "OK");
        }
    }
    
    /// <summary>
    /// Crée une nouvelle alerte
    /// </summary>
    private async Task CreateAlertAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAlertName) || string.IsNullOrWhiteSpace(NewAlertKeywords))
        {
            await Shell.Current.DisplayAlert("Erreur", "Veuillez saisir un nom et des mots-clés", "OK");
            return;
        }
        
        try
        {
            var keywords = NewAlertKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .ToList();
            
            var success = await _favoritesService.CreateAlertAsync(
                NewAlertName,
                keywords,
                new List<string>(), // Categories - à implémenter plus tard
                NewAlertLocation.Trim()
            );
            
            if (success)
            {
                // Recharger les alertes
                await LoadDataAsync();
                
                // Réinitialiser le formulaire
                NewAlertName = string.Empty;
                NewAlertKeywords = string.Empty;
                NewAlertLocation = string.Empty;
                ShowCreateAlertDialog = false;
                
                await Shell.Current.DisplayAlert("Succès", "Alerte créée avec succès", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Erreur", "Impossible de créer l'alerte", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FavoritesViewModel] Erreur lors de la création d'alerte: {ex.Message}");
            await Shell.Current.DisplayAlert("Erreur", "Une erreur est survenue", "OK");
        }
    }
    
    /// <summary>
    /// Supprime un favori
    /// </summary>
    private async Task DeleteFavoriteAsync(Favorite favorite)
    {
        var result = await Shell.Current.DisplayAlert(
            "Confirmer", 
            $"Supprimer '{favorite.AnnonceTitle}' des favoris ?", 
            "Supprimer", 
            "Annuler"
        );
        
        if (result)
        {
            var success = await _favoritesService.RemoveFromFavoritesAsync(favorite.AnnonceId);
            if (success)
            {
                Favorites.Remove(favorite);
            }
        }
    }
    
    /// <summary>
    /// Supprime une liste (après confirmation)
    /// </summary>
    private async Task DeleteListAsync(FavoriteList list)
    {
        var result = await Shell.Current.DisplayAlert(
            "Confirmer", 
            $"Supprimer la liste '{list.Name}' et tous ses favoris ?", 
            "Supprimer", 
            "Annuler"
        );
        
        if (result)
        {
            // Implémentation à faire dans le service
            await Shell.Current.DisplayAlert("Info", "Fonctionnalité en cours de développement", "OK");
        }
    }
    
    /// <summary>
    /// Supprime une alerte
    /// </summary>
    private async Task DeleteAlertAsync(AnnonceAlert alert)
    {
        var result = await Shell.Current.DisplayAlert(
            "Confirmer", 
            $"Supprimer l'alerte '{alert.Name}' ?", 
            "Supprimer", 
            "Annuler"
        );
        
        if (result)
        {
            // Implémentation à faire dans le service
            await Shell.Current.DisplayAlert("Info", "Fonctionnalité en cours de développement", "OK");
        }
    }
    
    /// <summary>
    /// Navigue vers le détail d'une annonce favorite
    /// </summary>
    private async Task NavigateToAnnonceAsync(Favorite favorite)
    {
        System.Diagnostics.Debug.WriteLine($"🔵 [FavoritesViewModel] NavigateToAnnonceAsync appelé pour: {favorite?.AnnonceId ?? "NULL"}");
        
        if (favorite == null)
        {
            System.Diagnostics.Debug.WriteLine("❌ [FavoritesViewModel] Favorite est NULL");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"🔵 [FavoritesViewModel] AnnonceImageUrl: {favorite.AnnonceImageUrl ?? "NULL"}");
            System.Diagnostics.Debug.WriteLine($"🔵 [FavoritesViewModel] AnnonceTitle: {favorite.AnnonceTitle ?? "NULL"}");

            // Si on a une URL d'image, on ouvre le visualiseur d'images
            if (!string.IsNullOrEmpty(favorite.AnnonceImageUrl) && Uri.IsWellFormedUriString(favorite.AnnonceImageUrl, UriKind.Absolute))
            {
                System.Diagnostics.Debug.WriteLine($"✅ [FavoritesViewModel] Navigation vers ImageViewerView avec URL: {favorite.AnnonceImageUrl}");
                await Shell.Current.GoToAsync($"ImageViewerView?imageUrls={favorite.AnnonceImageUrl}");
            }
            else
            {
                // Sinon, on navigue vers la liste des annonces
                System.Diagnostics.Debug.WriteLine("⚠️ [FavoritesViewModel] Pas d'URL d'image valide, navigation vers AnnoncesView");
                await Shell.Current.DisplayAlert(
                    favorite.AnnonceTitle,
                    $"Type: {favorite.AnnonceType}\nCatégorie: {favorite.AnnonceCategory}\nLocalisation: {favorite.AnnonceLocation}\n\nPour voir les détails complets, recherchez cette annonce dans la liste des annonces.",
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FavoritesViewModel] Erreur lors de la navigation: {ex.Message}");
            await Shell.Current.DisplayAlert("Erreur", "Impossible d'ouvrir l'annonce", "OK");
        }
    }
    
    /// <summary>
    /// Demande à l'utilisateur de choisir une liste pour ajouter un favori
    /// </summary>
    private async Task<string?> PromptForListSelectionAsync()
    {
        if (!UserLists.Any())
        {
            // Initialiser les listes par défaut si aucune n'existe
            await _favoritesService.InitializeDefaultListsAsync();
            await LoadDataAsync();
        }
        
        if (UserLists.Count == 1)
        {
            return UserLists.First().Name;
        }
        
        // Afficher une liste de choix
        var listNames = UserLists.Select(l => l.Name).ToArray();
        var result = await Shell.Current.DisplayActionSheet(
            "Ajouter à quelle liste ?", 
            "Annuler", 
            null, 
            listNames
        );
        
        return result != "Annuler" ? result : null;
    }
    
    /// <summary>
    /// Vérifie si une annonce est en favoris (pour l'UI)
    /// </summary>
    public async Task<bool> IsAnnonceInFavoritesAsync(string annonceId)
    {
        return await _favoritesService.IsFavoriteAsync(annonceId);
    }
}

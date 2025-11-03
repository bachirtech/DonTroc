// Fichier: DonTroc/ViewModels/DashboardViewModel.cs

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using DonTroc.Views;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

// ViewModel pour la page d'accueil (Dashboard) avec aperçu des offres récentes
public class DashboardViewModel : BaseViewModel
{
    private readonly FirebaseService _firebaseService;
    private readonly AuthService _authService;
    private readonly AdMobService _adMobService; // Service de monétisation
    private readonly PremiumFeaturesViewModel _premiumFeatures; // ViewModel des fonctionnalités premium

    // Collections pour l'affichage des données
    public ObservableCollection<Annonce> AnnoncesRecentes { get; } = new();
    
    // Propriétés pour les statistiques utilisateur
    private int _nombreMesAnnonces;
    public int NombreMesAnnonces
    {
        get => _nombreMesAnnonces;
        set => SetProperty(ref _nombreMesAnnonces, value);
    }

    private int _nombreConversations;
    public int NombreConversations
    {
        get => _nombreConversations;
        set => SetProperty(ref _nombreConversations, value);
    }

    private string _messageAccueil;
    public string MessageAccueil
    {
        get => _messageAccueil;
        set => SetProperty(ref _messageAccueil, value);
    }

    // Propriétés et commandes pour les fonctionnalités premium
    public bool IsAdFreeActive => _premiumFeatures.IsAdFreeActive;
    public DateTime AdFreeUntil => _premiumFeatures.AdFreeUntil;
    public int BoostCredits => _premiumFeatures.BoostCredits;
    public ICommand WatchAdForAdFreeCommand => _premiumFeatures.WatchAdForAdFreeCommand;
    public ICommand WatchAdForBoostCreditsCommand => _premiumFeatures.WatchAdForBoostCreditsCommand;
    public ICommand WatchAdForStatsCommand => _premiumFeatures.WatchAdForStatsCommand;

    // Commandes
    public ICommand GoToCreationAnnonceCommand { get; }
    public ICommand GoToAnnoncesCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand VoirAnnonceCommand { get; }

    // Constructeur avec injection de dépendances
    public DashboardViewModel(FirebaseService firebaseService, AuthService authService, AdMobService adMobService, PremiumFeaturesViewModel premiumFeatures)
    {
        _firebaseService = firebaseService;
        _authService = authService;
        _adMobService = adMobService;
        _premiumFeatures = premiumFeatures;

        // S'abonner aux changements de propriétés du ViewModel premium
        _premiumFeatures.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null) OnPropertyChanged(e.PropertyName);
        };

        // Initialisation des propriétés
        _messageAccueil = "Bienvenue sur DonTroc !";
        _nombreMesAnnonces = 0;
        _nombreConversations = 0;

        // Initialisation des commandes
        GoToCreationAnnonceCommand = new Command(async void () => 
        {
            await Shell.Current.GoToAsync(nameof(CreationAnnonceView));
        });

        GoToAnnoncesCommand = new Command(async void () =>
        {
            await Shell.Current.GoToAsync("//AnnoncesView");
        });

        RefreshCommand = new Command(async void () => await LoadDashboardDataAsync());
        
        VoirAnnonceCommand = new Command<Annonce>(async void (annonce) => await OnVoirAnnonce(annonce));

        // Charger les données au démarrage
        _ = LoadDashboardDataAsync();
        
        // Précharger les publicités récompensées
        _adMobService.LoadAds();
    }

    /// <summary>
    /// Charge toutes les données nécessaires pour le dashboard
    /// </summary>
    public async Task LoadDashboardDataAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            // Charger les annonces récentes (toutes les annonces, limitées à 5)
            await LoadAnnoncesRecentesAsync();
            
            // Charger les statistiques de l'utilisateur connecté
            await LoadUserStatsAsync();
            
            // Mettre à jour le message d'accueil
            UpdateMessageAccueil();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des données du dashboard: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Charge les 5 annonces les plus récentes
    /// </summary>
    private async Task LoadAnnoncesRecentesAsync()
    {
        try
        {
            var toutesLesAnnonces = await _firebaseService.GetAllAnnoncesAsync();
            
            // Prendre les 5 annonces les plus récentes
            var annoncesRecentes = toutesLesAnnonces
                .OrderByDescending(a => a.DateCreation)
                .Take(5)
                .ToList();

            // Mettre à jour la collection
            AnnoncesRecentes.Clear();
            foreach (var annonce in annoncesRecentes)
            {
                AnnoncesRecentes.Add(annonce);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des annonces récentes: {ex.Message}");
        }
    }

    /// <summary>
    /// Charge les statistiques de l'utilisateur connecté
    /// </summary>
    private async Task LoadUserStatsAsync()
    {
        try
        {
            var userId = _authService.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                // Compter les annonces de l'utilisateur
                var mesAnnonces = await _firebaseService.GetAnnoncesForUserAsync(userId);
                NombreMesAnnonces = mesAnnonces.Count();

                // Compter les conversations (approximatif pour l'instant)
                // TODO: Ajouter une méthode pour compter les conversations réelles
                NombreConversations = 0; // Placeholder
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des statistiques: {ex.Message}");
        }
    }

    /// <summary>
    /// Met à jour le message d'accueil basé sur les statistiques
    /// </summary>
    private void UpdateMessageAccueil()
    {
        if (NombreMesAnnonces == 0)
        {
            MessageAccueil = "Bienvenue sur DonTroc ! Publiez votre première annonce 🎉";
        }
        else if (NombreMesAnnonces == 1)
        {
            MessageAccueil = "Vous avez publié votre première annonce ! 👍";
        }
        else
        {
            MessageAccueil = $"Vous avez {NombreMesAnnonces} annonces publiées 🌟";
        }
    }

    /// <summary>
    /// Navigation vers le détail d'une annonce
    /// </summary>
    private async Task OnVoirAnnonce(Annonce? annonce)
    {
        if (annonce == null) return;

        // Pour l'instant, aller vers la page des annonces
        // TODO: Créer une page de détail d'annonce
        await Shell.Current.GoToAsync("//AnnoncesView");
    }
}

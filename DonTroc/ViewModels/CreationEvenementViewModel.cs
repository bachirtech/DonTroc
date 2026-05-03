using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel de création d'un événement (vide-grenier, local géolocalisé).
/// Les événements saisonniers officiels sont créés depuis l'admin.
/// </summary>
public class CreationEvenementViewModel : BaseViewModel
{
    private readonly EventService _eventService;
    private readonly AuthService _authService;
    private readonly GeolocationService _geolocationService;

    public List<string> TypesDisponibles { get; } = new() { "Vide-grenier virtuel", "Troc de quartier (local)" };

    private string _titre = string.Empty;
    public string Titre { get => _titre; set => SetProperty(ref _titre, value); }

    private string _description = string.Empty;
    public string Description { get => _description; set => SetProperty(ref _description, value); }

    private string _selectedType = "Vide-grenier virtuel";
    public string SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
                OnPropertyChanged(nameof(IsLocalEvent));
        }
    }

    public bool IsLocalEvent => SelectedType == "Troc de quartier (local)";

    private DateTime _dateDebut = DateTime.Now.AddDays(7);
    public DateTime DateDebut { get => _dateDebut; set => SetProperty(ref _dateDebut, value); }

    private TimeSpan _heureDebut = new TimeSpan(14, 0, 0);
    public TimeSpan HeureDebut { get => _heureDebut; set => SetProperty(ref _heureDebut, value); }

    private TimeSpan _heureFin = new TimeSpan(18, 0, 0);
    public TimeSpan HeureFin { get => _heureFin; set => SetProperty(ref _heureFin, value); }

    private string _adresseRdv = string.Empty;
    public string AdresseRdv { get => _adresseRdv; set => SetProperty(ref _adresseRdv, value); }

    private string _ville = string.Empty;
    public string Ville { get => _ville; set => SetProperty(ref _ville, value); }

    private int? _maxParticipants;
    public int? MaxParticipants { get => _maxParticipants; set => SetProperty(ref _maxParticipants, value); }

    private string? _statusMessage;
    public string? StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public ICommand SaveCommand { get; }
    public ICommand UseCurrentLocationCommand { get; }

    public CreationEvenementViewModel(
        EventService eventService,
        AuthService authService,
        GeolocationService geolocationService,
        AdMobService? adMobService = null,
        ILogger<CreationEvenementViewModel>? logger = null) : base(logger)
    {
        _eventService = eventService;
        _authService = authService;
        _geolocationService = geolocationService;
        _adMobService = adMobService;

        SaveCommand = new Command(async () => await SaveAsync());
        UseCurrentLocationCommand = new Command(async () => await UseCurrentLocationAsync());
    }

    private readonly AdMobService? _adMobService;

    private double? _latitude;
    private double? _longitude;

    /// <summary>Latitude sélectionnée (lecture pour binding code-behind map).</summary>
    public double? SelectedLatitude
    {
        get => _latitude;
        private set { _latitude = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelectedLocation)); }
    }
    public double? SelectedLongitude
    {
        get => _longitude;
        private set { _longitude = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelectedLocation)); }
    }
    public bool HasSelectedLocation => _latitude.HasValue && _longitude.HasValue;

    /// <summary>Appelé par le code-behind quand l'utilisateur tape sur la carte.</summary>
    public void SetSelectedLocation(double latitude, double longitude)
    {
        SelectedLatitude = latitude;
        SelectedLongitude = longitude;
        StatusMessage = $"📍 Lieu sélectionné : {latitude:F4}, {longitude:F4}";
    }

    private async Task UseCurrentLocationAsync()
    {
        await ExecuteAsync(async () =>
        {
            var loc = _geolocationService.GetLastKnownLocation();
            if (loc == null)
            {
                StatusMessage = "📍 Position GPS indisponible. Activez la localisation.";
                return;
            }
            SelectedLatitude = loc.Latitude;
            SelectedLongitude = loc.Longitude;
            StatusMessage = $"📍 Position enregistrée : {loc.Latitude:F4}, {loc.Longitude:F4}";
            await Task.CompletedTask;
        }, operationName: "UseCurrentLocation");
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Titre) || Titre.Length < 3)
        {
            StatusMessage = "⚠️ Le titre doit contenir au moins 3 caractères.";
            return;
        }

        if (DateDebut.Date < DateTime.Today)
        {
            StatusMessage = "⚠️ La date doit être dans le futur.";
            return;
        }

        if (IsLocalEvent && (_latitude == null || _longitude == null))
        {
            StatusMessage = "⚠️ Pour un troc local, indiquez le lieu de RDV (bouton 'Ma position').";
            return;
        }

        // 🔒 Sécurité : la pub a normalement été regardée à l'ouverture de la page
        // (cf. EnsureAccessUnlockedAsync appelée par le code-behind dans OnAppearing).
        // On vérifie quand même le flag au cas où l'utilisateur aurait contourné le flow.
        if (!IsAccessUnlocked)
        {
            StatusMessage = "⏸️ Tu dois d'abord regarder la pub pour débloquer la création.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var user = await _authService.GetCurrentUserAsync();
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                StatusMessage = "Vous devez être connecté.";
                return;
            }

            var startUtc = DateTime.SpecifyKind(DateDebut.Date.Add(HeureDebut), DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(DateDebut.Date.Add(HeureFin), DateTimeKind.Local).ToUniversalTime();
            if (endUtc <= startUtc) endUtc = startUtc.AddHours(2);

            var ev = new Evenement
            {
                Titre = Titre.Trim(),
                Description = Description?.Trim() ?? string.Empty,
                CreateurId = userId,
                CreateurName = user?.DisplayName ?? "Utilisateur",
                Type = IsLocalEvent ? TypeEvenement.LocalGeolocalise : TypeEvenement.VideGrenier,
                Statut = StatutEvenement.AVenir,
                DateDebut = startUtc,
                DateFin = endUtc,
                Latitude = _latitude,
                Longitude = _longitude,
                AdresseRdv = string.IsNullOrWhiteSpace(AdresseRdv) ? null : AdresseRdv.Trim(),
                Ville = string.IsNullOrWhiteSpace(Ville) ? null : Ville.Trim(),
                NombreMaxParticipants = MaxParticipants
            };

            var newId = await _eventService.CreateEventAsync(ev);
            if (string.IsNullOrEmpty(newId))
            {
                var detail = string.IsNullOrEmpty(_eventService.LastError) ? "raison inconnue" : _eventService.LastError;
                StatusMessage = $"❌ Erreur lors de la création : {detail}";
                Logger?.LogWarning("Échec création événement : {Error}", detail);
                return;
            }

            StatusMessage = "✅ Événement créé !";

            // Pas d'interstitiel post-création : l'utilisateur vient déjà de regarder un rewarded
            // pour débloquer la création. Lui en mettre un second serait abusif.

            await Shell.Current.GoToAsync($"..?eventId={newId}");
        }, operationName: "SaveEvent");
    }

    // ════════════════════════════════════════════════════════════
    // REWARDED AD OBLIGATOIRE — "Regarde une pub pour créer un événement"
    // L'accès est gaté à l'OUVERTURE de la page (OnAppearing du code-behind).
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Indique si l'utilisateur a débloqué l'accès à cette session de création
    /// (via une pub rewarded regardée jusqu'au bout).
    /// </summary>
    public bool IsAccessUnlocked { get; private set; }

    /// <summary>
    /// À appeler depuis le code-behind dans <c>OnAppearing</c>.
    /// Si l'accès n'a pas encore été débloqué, propose la pub rewarded.
    /// Retourne <c>false</c> si l'utilisateur a refusé / la pub a échoué — dans ce cas
    /// le code-behind doit faire un <c>Shell.Current.GoToAsync("..")</c> pour fermer la page.
    /// </summary>
    public async Task<bool> EnsureAccessUnlockedAsync()
    {
        if (IsAccessUnlocked) return true;

        // Pas de service AdMob (mode dev / tests unitaires) → on laisse passer.
        if (_adMobService == null)
        {
            IsAccessUnlocked = true;
            return true;
        }

        // 🛡️ Si les pubs sont globalement désactivées (ADS_ENABLED=false dans AdMobConfiguration,
        // ex: mode debug, désactivation Google ou fallback) → ne pas bloquer la création.
        // Sinon, ShowRewardedAdAsync retournerait toujours false et personne ne pourrait créer.
        if (!AdMobConfiguration.ADS_ENABLED)
        {
            IsAccessUnlocked = true;
            return true;
        }

        // 🎁 Bypass si l'utilisateur a un mode Ad-Free actif (achat in-app — pas opérationnel
        // pour l'instant mais le check est gratuit et prêt pour le futur).
        if (_adMobService.IsAdFreeActive())
        {
            IsAccessUnlocked = true;
            return true;
        }

        // Annonce explicite à l'utilisateur (UX honnête + meilleur taux de complétion)
        var watch = await Shell.Current.DisplayAlert(
            "🎬 Débloque la création",
            "Pour créer un événement sur DonTroc, regarde une courte vidéo publicitaire.\n\n" +
            "Cela nous aide à garder l'application gratuite pour toute la communauté 💚",
            "▶️ Regarder la pub", "Annuler");

        if (!watch) return false;

        // Précharger si la pub n'est pas prête
        if (!_adMobService.IsRewardedAdReady())
        {
            _adMobService.LoadRewardedAd();
            // Petit délai pour laisser le SDK charger
            await Task.Delay(2000);
        }

        var rewarded = await _adMobService.ShowRewardedAdAsync();
        if (!rewarded)
        {
            await Shell.Current.DisplayAlert(
                "Pub indisponible",
                "Impossible de charger la vidéo. Vérifie ta connexion internet et réessaie 🙏",
                "OK");
            return false;
        }

        IsAccessUnlocked = true;
        return true;
    }
}


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;

namespace DonTroc.ViewModels;

/// <summary>
/// ViewModel pour la page d'administration des événements officiels saisonniers.
/// Permet de :
///  - Choisir un preset (Rentrée, Noël Solidaire, Ramadan du Partage, Printemps du Troc)
///  - Personnaliser titre / description / dates
///  - Publier l'événement avec IsOfficial = true
///  - Broadcaster une notification FCM à tous les utilisateurs (topic "all_users")
///  - Lister / supprimer les événements officiels existants
/// </summary>
public class AdminEventsViewModel : BaseViewModel
{
    private readonly EventService _eventService;
    private readonly AdminService _adminService;
    private readonly AuthService _authService;
    private readonly PushNotificationService _pushNotificationService;

    /// <summary>
    /// Représente un template prêt-à-l'emploi pour un événement saisonnier officiel.
    /// </summary>
    public class SeasonalPreset
    {
        public string Key { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Titre { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SaisonEvenement Saison { get; set; }
        public string Color { get; set; } = "#D98C6A";
        public string PushTitle { get; set; } = string.Empty;
        public string PushBody { get; set; } = string.Empty;
    }

    /// <summary>4 presets saisonniers officiels.</summary>
    public List<SeasonalPreset> Presets { get; } = new()
    {
        new SeasonalPreset
        {
            Key = "rentree",
            Emoji = "🎒",
            Titre = "Spécial Rentrée",
            Description = "C'est la rentrée ! Donne une seconde vie à tes affaires d'école et trouve les fournitures dont tu as besoin. Cartables, livres, vêtements... tout y passe !",
            Saison = SaisonEvenement.Rentree,
            Color = "#D98C6A",
            PushTitle = "🎒 Spécial Rentrée — Le grand troc commence !",
            PushBody = "Vide ton placard, équipe ta rentrée à 0€ ! Découvre les annonces de la communauté."
        },
        new SeasonalPreset
        {
            Key = "noel",
            Emoji = "🎁",
            Titre = "Noël Solidaire",
            Description = "Offre une magie de Noël à ceux qui en ont besoin. Donne ce qui ne te sert plus, reçois ce qui te ferait plaisir. Ensemble pour des fêtes plus solidaires !",
            Saison = SaisonEvenement.NoelSolidaire,
            Color = "#C0392B",
            PushTitle = "🎁 Noël Solidaire DonTroc",
            PushBody = "Faisons de cette fin d'année un moment de partage. Donne, reçois, fais des heureux !"
        },
        new SeasonalPreset
        {
            Key = "ramadan",
            Emoji = "🌙",
            Titre = "Ramadan du Partage",
            Description = "Le mois sacré du partage et de la générosité. Multiplie les bonnes actions en donnant ce qui ne te sert plus à ceux qui en ont besoin.",
            Saison = SaisonEvenement.RamadanPartage,
            Color = "#8E44AD",
            PushTitle = "🌙 Ramadan du Partage",
            PushBody = "Ramadan Moubarak ! Multiplie les dons, partage avec ta communauté."
        },
        new SeasonalPreset
        {
            Key = "printemps",
            Emoji = "🌸",
            Titre = "Printemps du Troc",
            Description = "Grand ménage de printemps ! Désencombre ton intérieur et fais le bonheur de quelqu'un d'autre. Ce qui ne te sert plus servira à un autre !",
            Saison = SaisonEvenement.PrintempsTroc,
            Color = "#A8C686",
            PushTitle = "🌸 Printemps du Troc — On désencombre !",
            PushBody = "Le grand ménage de printemps est lancé ! Donne ce qui ne te sert plus, fais des heureux."
        }
    };

    // === FORMULAIRE ===
    private SeasonalPreset? _selectedPreset;
    public SeasonalPreset? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetProperty(ref _selectedPreset, value) && value != null)
            {
                Titre = $"{value.Emoji} {value.Titre}";
                Description = value.Description;
                PushTitle = value.PushTitle;
                PushBody = value.PushBody;
                AccentColor = value.Color;
                OnPropertyChanged(nameof(HasPreset));
            }
        }
    }

    public bool HasPreset => SelectedPreset != null;

    private string _titre = string.Empty;
    public string Titre { get => _titre; set => SetProperty(ref _titre, value); }

    private string _description = string.Empty;
    public string Description { get => _description; set => SetProperty(ref _description, value); }

    private DateTime _dateDebut = DateTime.Now.Date.AddDays(7);
    public DateTime DateDebut { get => _dateDebut; set => SetProperty(ref _dateDebut, value); }

    private DateTime _dateFin = DateTime.Now.Date.AddDays(14);
    public DateTime DateFin { get => _dateFin; set => SetProperty(ref _dateFin, value); }

    private string _pushTitle = string.Empty;
    public string PushTitle { get => _pushTitle; set => SetProperty(ref _pushTitle, value); }

    private string _pushBody = string.Empty;
    public string PushBody { get => _pushBody; set => SetProperty(ref _pushBody, value); }

    private bool _sendBroadcast = true;
    public bool SendBroadcast { get => _sendBroadcast; set => SetProperty(ref _sendBroadcast, value); }

    private string _accentColor = "#D98C6A";
    public string AccentColor { get => _accentColor; set => SetProperty(ref _accentColor, value); }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    // === LISTE ÉVÉNEMENTS OFFICIELS ===
    public ObservableCollection<Evenement> ExistingOfficials { get; } = new();
    public bool HasOfficials => ExistingOfficials.Count > 0;

    // === COMMANDES ===
    public ICommand SelectPresetCommand { get; }
    public ICommand PublishCommand { get; }
    public ICommand DeleteOfficialCommand { get; }
    public ICommand RefreshCommand { get; }

    public AdminEventsViewModel(
        EventService eventService,
        AdminService adminService,
        AuthService authService,
        PushNotificationService pushNotificationService,
        ILogger<AdminEventsViewModel>? logger = null) : base(logger)
    {
        _eventService = eventService;
        _adminService = adminService;
        _authService = authService;
        _pushNotificationService = pushNotificationService;

        SelectPresetCommand = new Command<SeasonalPreset>(p => SelectedPreset = p);
        PublishCommand = new Command(async () => await PublishAsync());
        DeleteOfficialCommand = new Command<Evenement>(async ev => await DeleteOfficialAsync(ev));
        RefreshCommand = new Command(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Vérifier permission admin
            var isAdmin = await _adminService.IsCurrentUserAdminAsync();
            if (!isAdmin)
            {
                StatusMessage = "⚠️ Accès admin requis";
                return;
            }

            var officials = await _eventService.GetSeasonalEventsAsync(50);
            ExistingOfficials.Clear();
            foreach (var ev in officials.OrderByDescending(e => e.DateDebutTimestamp))
                ExistingOfficials.Add(ev);

            OnPropertyChanged(nameof(HasOfficials));
        }, operationName: "LoadAdminEvents");
    }

    private async Task PublishAsync()
    {
        StatusMessage = string.Empty;

        if (SelectedPreset == null)
        {
            StatusMessage = "❌ Choisis d'abord un thème saisonnier.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Titre) || string.IsNullOrWhiteSpace(Description))
        {
            StatusMessage = "❌ Titre et description sont obligatoires.";
            return;
        }

        if (DateFin <= DateDebut)
        {
            StatusMessage = "❌ La date de fin doit être après la date de début.";
            return;
        }

        // Confirmation
        var confirmMsg = SendBroadcast
            ? $"Publier « {Titre} » et envoyer une notification push à TOUS les utilisateurs ?"
            : $"Publier « {Titre} » sans notification push ?";

        var ok = await Shell.Current.DisplayAlert(
            "Publier événement officiel",
            confirmMsg,
            "Publier", "Annuler");

        if (!ok) return;

        await ExecuteAsync(async () =>
        {
            // Vérifier admin une dernière fois côté serveur
            var isAdmin = await _adminService.IsCurrentUserAdminAsync();
            if (!isAdmin)
            {
                StatusMessage = "❌ Accès admin requis.";
                return;
            }

            var admin = await _adminService.GetCurrentAdminAsync();
            var ev = new Evenement
            {
                Titre = Titre.Trim(),
                Description = Description.Trim(),
                Type = TypeEvenement.Saisonnier,
                Saison = SelectedPreset.Saison,
                IsOfficial = true,
                DateDebut = DateDebut,
                DateFin = DateFin,
                CreateurName = admin?.Name ?? "DonTroc",
                Statut = StatutEvenement.AVenir
            };
            ev.SyncTimestamps();

            var newId = await _eventService.CreateEventAsync(ev);
            if (string.IsNullOrEmpty(newId))
            {
                var detail = string.IsNullOrEmpty(_eventService.LastError) ? "raison inconnue" : _eventService.LastError;
                StatusMessage = $"❌ Création impossible : {detail}";
                return;
            }

            // Broadcast FCM (topic "all_users" auquel tous les clients sont abonnés)
            if (SendBroadcast)
            {
                try
                {
                    var pushOk = await _pushNotificationService.SendToTopicAsync(
                        "all_users",
                        PushTitle,
                        PushBody,
                        new { type = "official_event", eventId = newId });

                    StatusMessage = pushOk
                        ? $"✅ « {Titre} » publié + notification envoyée à tous !"
                        : $"✅ « {Titre} » publié, mais l'envoi push a échoué (vérifie la Cloud Function).";
                }
                catch (Exception ex)
                {
                    Logger?.LogWarning(ex, "Erreur broadcast FCM officiel");
                    StatusMessage = $"✅ « {Titre} » publié, mais erreur push : {ex.Message}";
                }
            }
            else
            {
                StatusMessage = $"✅ « {Titre} » publié (sans notification).";
            }

            // Reset du formulaire et recharge la liste
            SelectedPreset = null;
            Titre = string.Empty;
            Description = string.Empty;
            PushTitle = string.Empty;
            PushBody = string.Empty;

            await LoadAsync();
        }, operationName: "PublishOfficialEvent");
    }

    private async Task DeleteOfficialAsync(Evenement? ev)
    {
        if (ev == null || string.IsNullOrEmpty(ev.Id)) return;

        var confirm = await Shell.Current.DisplayAlert(
            "Supprimer événement officiel",
            $"Supprimer définitivement « {ev.Titre} » ? Tous les participants seront désinscrits.",
            "Supprimer", "Annuler");

        if (!confirm) return;

        await ExecuteAsync(async () =>
        {
            var ok = await _eventService.DeleteEventAsync(ev.Id);
            if (ok)
            {
                ExistingOfficials.Remove(ev);
                OnPropertyChanged(nameof(HasOfficials));
                StatusMessage = $"✅ « {ev.Titre} » supprimé.";
            }
            else
            {
                var detail = string.IsNullOrEmpty(_eventService.LastError) ? "raison inconnue" : _eventService.LastError;
                StatusMessage = $"❌ Suppression impossible : {detail}";
            }
        }, operationName: "DeleteOfficialEvent");
    }
}


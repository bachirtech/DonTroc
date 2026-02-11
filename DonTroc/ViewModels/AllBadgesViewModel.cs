using System.Collections.ObjectModel;
using System.Windows.Input;
using DonTroc.Configuration;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Extensions.Logging;

namespace DonTroc.ViewModels;

// Alias pour éviter les ambiguïtés
using Badge = DonTroc.Models.Badge;

/// <summary>
/// ViewModel pour la page affichant tous les badges disponibles
/// </summary>
public class AllBadgesViewModel : BaseViewModel
{
    private readonly IGamificationService _gamificationService;
    private readonly AuthService _authService;
    private readonly ILogger<AllBadgesViewModel> _logger;

    private BadgeCategory? _selectedCategory;
    private string _searchText = string.Empty;
    private int _totalBadges;
    private int _unlockedBadges;

    // Collections
    public ObservableCollection<BadgeCategoryGroup> BadgeGroups { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();
    
    // Liste de tous les badges de l'utilisateur (débloqués)
    private HashSet<string> _userBadgeIds = new();

    public AllBadgesViewModel(
        IGamificationService gamificationService,
        AuthService authService,
        ILogger<AllBadgesViewModel> logger)
    {
        _gamificationService = gamificationService;
        _authService = authService;
        _logger = logger;

        LoadDataCommand = new Command(async () => await LoadDataAsync());
        FilterByCategoryCommand = new Command<string>(FilterByCategory);
        SearchCommand = new Command<string>(Search);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

        // Charger les catégories
        Categories.Add("Tous");
        foreach (var category in Enum.GetValues<BadgeCategory>())
        {
            Categories.Add(GetCategoryDisplayName(category));
        }
    }

    #region Propriétés

    public BadgeCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            SetProperty(ref _selectedCategory, value);
            _ = LoadDataAsync();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            _ = LoadDataAsync();
        }
    }

    public int TotalBadges
    {
        get => _totalBadges;
        set
        {
            SetProperty(ref _totalBadges, value);
            OnPropertyChanged(nameof(BadgeCountText));
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(ProgressBarWidth));
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public int UnlockedBadges
    {
        get => _unlockedBadges;
        set
        {
            SetProperty(ref _unlockedBadges, value);
            OnPropertyChanged(nameof(BadgeCountText));
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(ProgressBarWidth));
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public double ProgressPercentage => TotalBadges > 0 ? (double)UnlockedBadges / TotalBadges * 100 : 0;
    
    // Propriétés calculées pour le binding simplifié
    public string BadgeCountText => $"{UnlockedBadges}/{TotalBadges}";
    public double ProgressBarWidth => Math.Min(300, ProgressPercentage * 3); // Max 300px
    public string ProgressText => $"{ProgressPercentage:F0}% de la collection";

    #endregion

    #region Commandes

    public ICommand LoadDataCommand { get; }
    public ICommand FilterByCategoryCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand BackCommand { get; }

    #endregion

    #region Méthodes

    public async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            
            // Obtenir tous les badges disponibles depuis la config (toujours disponible)
            var allBadges = GamificationConfig.AllBadges;
            TotalBadges = allBadges.Count;

            // Essayer de charger les badges débloqués de l'utilisateur
            try
            {
                var userId = _authService.GetUserId();
                if (!string.IsNullOrEmpty(userId))
                {
                    var userBadges = await _gamificationService.GetUserBadgesAsync(userId);
                    _userBadgeIds = new HashSet<string>(userBadges?.Select(b => b.Id) ?? Enumerable.Empty<string>());
                    UnlockedBadges = _userBadgeIds.Count;
                }
                else
                {
                    _userBadgeIds = new HashSet<string>();
                    UnlockedBadges = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de charger les badges de l'utilisateur, affichage sans progression");
                _userBadgeIds = new HashSet<string>();
                UnlockedBadges = 0;
            }

            // Filtrer par catégorie si nécessaire
            var filteredBadges = allBadges.AsEnumerable();
            
            if (_selectedCategory.HasValue)
            {
                filteredBadges = filteredBadges.Where(b => b.Category == _selectedCategory.Value);
            }

            // Filtrer par texte de recherche
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var searchLower = _searchText.ToLowerInvariant();
                filteredBadges = filteredBadges.Where(b => 
                    b.Name.ToLowerInvariant().Contains(searchLower) ||
                    b.Description.ToLowerInvariant().Contains(searchLower));
            }

            // Grouper par catégorie
            BadgeGroups.Clear();
            var grouped = filteredBadges
                .GroupBy(b => b.Category)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var categoryGroup = new BadgeCategoryGroup
                {
                    CategoryName = GetCategoryDisplayName(group.Key),
                    CategoryIcon = GetCategoryIcon(group.Key)
                };

                foreach (var badge in group.OrderBy(b => b.RequiredValue))
                {
                    var isUnlocked = _userBadgeIds.Contains(badge.Id);
                    categoryGroup.Badges.Add(new BadgeDisplayItem
                    {
                        Id = badge.Id,
                        Name = badge.Name,
                        Description = badge.Description,
                        Icon = badge.Icon,
                        Rarity = badge.Rarity,
                        RarityName = GetRarityDisplayName(badge.Rarity),
                        RarityColor = GetRarityColor(badge.Rarity),
                        RarityBackgroundColor = GetRarityBackgroundColor(badge.Rarity),
                        XpReward = badge.XpReward,
                        RequiredValue = badge.RequiredValue,
                        IsUnlocked = isUnlocked,
                        IsLocked = !isUnlocked,
                        IsSecret = badge.IsSecret && !isUnlocked
                    });
                }

                BadgeGroups.Add(categoryGroup);
            }

            OnPropertyChanged(nameof(ProgressPercentage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement des badges");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterByCategory(string categoryName)
    {
        if (categoryName == "Tous")
        {
            SelectedCategory = null;
        }
        else
        {
            foreach (var category in Enum.GetValues<BadgeCategory>())
            {
                if (GetCategoryDisplayName(category) == categoryName)
                {
                    SelectedCategory = category;
                    break;
                }
            }
        }
    }

    private void Search(string text)
    {
        SearchText = text;
    }

    private static string GetCategoryDisplayName(BadgeCategory category)
    {
        return category switch
        {
            BadgeCategory.Donateur => "🎁 Donateur",
            BadgeCategory.Receveur => "🎀 Receveur",
            BadgeCategory.Social => "💬 Social",
            BadgeCategory.Explorateur => "🗺️ Explorateur",
            BadgeCategory.Veteran => "🏆 Vétéran",
            BadgeCategory.Special => "✨ Spécial",
            _ => category.ToString()
        };
    }

    private static string GetCategoryIcon(BadgeCategory category)
    {
        return category switch
        {
            BadgeCategory.Donateur => "🎁",
            BadgeCategory.Receveur => "🎀",
            BadgeCategory.Social => "💬",
            BadgeCategory.Explorateur => "🗺️",
            BadgeCategory.Veteran => "🏆",
            BadgeCategory.Special => "✨",
            _ => "🏅"
        };
    }

    private static string GetRarityDisplayName(BadgeRarity rarity)
    {
        return rarity switch
        {
            BadgeRarity.Common => "Commun",
            BadgeRarity.Uncommon => "Peu commun",
            BadgeRarity.Rare => "Rare",
            BadgeRarity.Epic => "Épique",
            BadgeRarity.Legendary => "Légendaire",
            _ => rarity.ToString()
        };
    }

    private static Color GetRarityColor(BadgeRarity rarity)
    {
        return rarity switch
        {
            BadgeRarity.Common => Color.FromArgb("#78909C"),
            BadgeRarity.Uncommon => Color.FromArgb("#4CAF50"),
            BadgeRarity.Rare => Color.FromArgb("#2196F3"),
            BadgeRarity.Epic => Color.FromArgb("#9C27B0"),
            BadgeRarity.Legendary => Color.FromArgb("#FF9800"),
            _ => Color.FromArgb("#78909C")
        };
    }

    private static Color GetRarityBackgroundColor(BadgeRarity rarity)
    {
        return rarity switch
        {
            BadgeRarity.Common => Color.FromArgb("#ECEFF1"),
            BadgeRarity.Uncommon => Color.FromArgb("#E8F5E9"),
            BadgeRarity.Rare => Color.FromArgb("#E3F2FD"),
            BadgeRarity.Epic => Color.FromArgb("#F3E5F5"),
            BadgeRarity.Legendary => Color.FromArgb("#FFF3E0"),
            _ => Color.FromArgb("#ECEFF1")
        };
    }

    #endregion
}

/// <summary>
/// Groupe de badges par catégorie
/// </summary>
public class BadgeCategoryGroup
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public ObservableCollection<BadgeDisplayItem> Badges { get; } = new();
}

/// <summary>
/// Item d'affichage pour un badge
/// </summary>
public class BadgeDisplayItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public BadgeRarity Rarity { get; set; }
    public string RarityName { get; set; } = string.Empty;
    public Color RarityColor { get; set; } = Colors.Gray;
    public Color RarityBackgroundColor { get; set; } = Colors.LightGray;
    public int XpReward { get; set; }
    public int RequiredValue { get; set; }
    public bool IsUnlocked { get; set; }
    public bool IsLocked { get; set; }
    public bool IsSecret { get; set; }
    
    public string DisplayName => IsSecret ? "??? Badge Secret ???" : Name;
    public string DisplayDescription => IsSecret ? "Continuez à explorer pour découvrir ce badge !" : Description;
    public string DisplayIcon => IsSecret ? "🔒" : Icon;
    public double Opacity => IsUnlocked ? 1.0 : 0.5;
    
    // Propriétés calculées pour éviter les Converters dans les DataTemplates imbriqués
    public string UnlockedStatusText => IsUnlocked ? "✓ Débloqué" : "🔒 Verrouillé";
    public Color UnlockedStatusColor => IsUnlocked ? Color.FromArgb("#4CAF50") : Color.FromArgb("#9E9E9E");
}


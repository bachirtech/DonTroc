using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using DonTroc.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace DonTroc.Services
{
    /// <summary>
    /// Interface pour le service de conseils et infobulles
    /// </summary>
    public interface ITipsService
    {
        /// <summary>
        /// Obtient le prochain conseil à afficher pour une fonctionnalité
        /// </summary>
        Task<TipDisplayConfig?> GetNextTipAsync(string featureKey);

        /// <summary>
        /// Marque un conseil comme vu
        /// </summary>
        Task MarkTipAsSeenAsync(string tipId);

        /// <summary>
        /// Ignore définitivement un conseil
        /// </summary>
        Task DismissTipAsync(string tipId);

        /// <summary>
        /// Vérifie si une fonctionnalité a des conseils non vus
        /// </summary>
        Task<bool> HasUnseenTipsAsync(string featureKey);

        /// <summary>
        /// Réinitialise tous les conseils (pour les montrer à nouveau)
        /// </summary>
        Task ResetAllTipsAsync();

        /// <summary>
        /// Active ou désactive tous les conseils
        /// </summary>
        Task SetTipsEnabledAsync(bool enabled);

        /// <summary>
        /// Vérifie si les conseils sont activés
        /// </summary>
        Task<bool> AreTipsEnabledAsync();

        /// <summary>
        /// Obtient tous les conseils disponibles pour une fonctionnalité
        /// </summary>
        IEnumerable<Tip> GetTipsForFeature(string featureKey);
    }

    /// <summary>
    /// Service de gestion des conseils et infobulles pour les premières utilisations
    /// </summary>
    public class TipsService : ITipsService
    {
        private const string TipStateKey = "dontroc_tip_state";
        private readonly ILogger<TipsService>? _logger;
        private TipState _state = new();
        private bool _initialized = false;

        // Dictionnaire de tous les conseils disponibles
        private readonly Dictionary<string, List<Tip>> _tips = new()
        {
            // Conseils pour la création d'annonce
            ["creation_annonce"] = new List<Tip>
            {
                new Tip
                {
                    Id = "creation_title",
                    FeatureKey = "creation_annonce",
                    Title = "Un bon titre fait la différence",
                    Message = "Utilisez un titre clair et descriptif pour attirer plus de personnes intéressées par votre objet.",
                    Icon = "✏️",
                    Order = 1,
                    Position = TipPosition.Top
                },
                new Tip
                {
                    Id = "creation_photos",
                    FeatureKey = "creation_annonce",
                    Title = "Les photos sont essentielles",
                    Message = "Ajoutez plusieurs photos sous différents angles. Les annonces avec photos ont 5x plus de chances d'être consultées !",
                    Icon = "📸",
                    Order = 2,
                    Position = TipPosition.Center
                },
                new Tip
                {
                    Id = "creation_description",
                    FeatureKey = "creation_annonce",
                    Title = "Détaillez votre description",
                    Message = "Mentionnez l'état, les dimensions et toute info utile. Plus c'est précis, moins vous aurez de questions !",
                    Icon = "📝",
                    Order = 3,
                    Position = TipPosition.Bottom
                }
            },

            // Conseils pour les annonces (liste)
            ["annonces_list"] = new List<Tip>
            {
                new Tip
                {
                    Id = "annonces_search",
                    FeatureKey = "annonces_list",
                    Title = "Recherche intelligente",
                    Message = "Utilisez la barre de recherche pour trouver rapidement ce que vous cherchez. Vous pouvez aussi filtrer par catégorie et type.",
                    Icon = "🔍",
                    Order = 1,
                    Position = TipPosition.Top
                },
                new Tip
                {
                    Id = "annonces_distance",
                    FeatureKey = "annonces_list",
                    Title = "Trouvez proche de vous",
                    Message = "Activez la géolocalisation pour voir les annonces les plus proches et trier par distance.",
                    Icon = "📍",
                    Order = 2,
                    Position = TipPosition.Center
                }
            },

            // Conseils pour les favoris
            ["favoris"] = new List<Tip>
            {
                new Tip
                {
                    Id = "favoris_add",
                    FeatureKey = "favoris",
                    Title = "Sauvegardez vos coups de cœur",
                    Message = "Appuyez sur le cœur ❤️ pour ajouter une annonce à vos favoris et la retrouver facilement.",
                    Icon = "💖",
                    Order = 1,
                    Position = TipPosition.Center
                },
                new Tip
                {
                    Id = "favoris_organize",
                    FeatureKey = "favoris",
                    Title = "Organisez vos favoris",
                    Message = "Créez des listes personnalisées pour organiser vos favoris par thème ou par priorité.",
                    Icon = "📁",
                    Order = 2,
                    Position = TipPosition.Bottom
                }
            },

            // Conseils pour le chat
            ["chat"] = new List<Tip>
            {
                new Tip
                {
                    Id = "chat_intro",
                    FeatureKey = "chat",
                    Title = "Communiquez facilement",
                    Message = "Discutez directement avec le propriétaire de l'annonce pour organiser l'échange.",
                    Icon = "💬",
                    Order = 1,
                    Position = TipPosition.Top
                },
                new Tip
                {
                    Id = "chat_voice",
                    FeatureKey = "chat",
                    Title = "Messages vocaux disponibles",
                    Message = "Maintenez le bouton micro pour envoyer un message vocal si vous préférez parler !",
                    Icon = "🎤",
                    Order = 2,
                    Position = TipPosition.Bottom
                }
            },

            // Conseils pour le profil
            ["profil"] = new List<Tip>
            {
                new Tip
                {
                    Id = "profil_complete",
                    FeatureKey = "profil",
                    Title = "Complétez votre profil",
                    Message = "Un profil complet avec photo inspire confiance. Ajoutez une bio pour vous présenter à la communauté !",
                    Icon = "👤",
                    Order = 1,
                    Position = TipPosition.Top
                },
                new Tip
                {
                    Id = "profil_badges",
                    FeatureKey = "profil",
                    Title = "Gagnez des badges",
                    Message = "Participez activement pour débloquer des badges et améliorer votre réputation !",
                    Icon = "🏆",
                    Order = 2,
                    Position = TipPosition.Center
                }
            },

            // Conseils pour les transactions
            ["transactions"] = new List<Tip>
            {
                new Tip
                {
                    Id = "transactions_status",
                    FeatureKey = "transactions",
                    Title = "Suivez vos échanges",
                    Message = "Retrouvez ici toutes vos transactions en cours et passées. Vous pouvez voir leur statut en temps réel.",
                    Icon = "📦",
                    Order = 1,
                    Position = TipPosition.Top
                },
                new Tip
                {
                    Id = "transactions_confirm",
                    FeatureKey = "transactions",
                    Title = "Confirmez vos échanges",
                    Message = "N'oubliez pas de confirmer la transaction une fois l'échange effectué pour gagner des points !",
                    Icon = "✅",
                    Order = 2,
                    Position = TipPosition.Bottom
                }
            },

            // Conseils pour la gamification (récompenses)
            ["rewards"] = new List<Tip>
            {
                new Tip
                {
                    Id = "rewards_intro",
                    FeatureKey = "rewards",
                    Title = "Gagnez des récompenses",
                    Message = "Participez aux quiz et défis quotidiens pour gagner des points et des récompenses exclusives !",
                    Icon = "🎁",
                    Order = 1,
                    Position = TipPosition.Top
                },
                new Tip
                {
                    Id = "rewards_wheel",
                    FeatureKey = "rewards",
                    Title = "Tournez la roue de la fortune",
                    Message = "Chaque jour, vous pouvez tourner la roue pour tenter de gagner des bonus !",
                    Icon = "🎡",
                    Order = 2,
                    Position = TipPosition.Center
                }
            },

            // Conseils pour le quiz
            ["quiz"] = new List<Tip>
            {
                new Tip
                {
                    Id = "quiz_intro",
                    FeatureKey = "quiz",
                    Title = "Testez vos connaissances",
                    Message = "Répondez aux quiz thématiques pour gagner des points. Plus vous êtes rapide, plus vous gagnez !",
                    Icon = "❓",
                    Order = 1,
                    Position = TipPosition.Top
                },
                new Tip
                {
                    Id = "quiz_rewarded",
                    FeatureKey = "quiz",
                    Title = "Seconde chance disponible",
                    Message = "Regardez une publicité pour obtenir une seconde chance si vous vous trompez !",
                    Icon = "🎬",
                    Order = 2,
                    Position = TipPosition.Bottom
                }
            },

            // Conseils pour la carte
            ["map"] = new List<Tip>
            {
                new Tip
                {
                    Id = "map_explore",
                    FeatureKey = "map",
                    Title = "Explorez autour de vous",
                    Message = "La carte vous montre toutes les annonces à proximité. Touchez un marqueur pour voir les détails.",
                    Icon = "🗺️",
                    Order = 1,
                    Position = TipPosition.Top
                }
            },

            // Conseils pour la notation
            ["rating"] = new List<Tip>
            {
                new Tip
                {
                    Id = "rating_importance",
                    FeatureKey = "rating",
                    Title = "Les avis comptent",
                    Message = "Après un échange, notez votre expérience. Cela aide la communauté à faire de meilleurs choix !",
                    Icon = "⭐",
                    Order = 1,
                    Position = TipPosition.Center
                }
            },

            // Conseils pour la roue de la fortune
            ["wheel"] = new List<Tip>
            {
                new Tip
                {
                    Id = "wheel_intro",
                    FeatureKey = "wheel",
                    Title = "Tentez votre chance !",
                    Message = "Faites tourner la roue une fois par jour pour gagner des points bonus, des badges ou des avantages exclusifs !",
                    Icon = "🎰",
                    Order = 1,
                    Position = TipPosition.Top
                },
                new Tip
                {
                    Id = "wheel_second_chance",
                    FeatureKey = "wheel",
                    Title = "Une seconde chance ?",
                    Message = "Vous pouvez regarder une publicité pour tourner la roue une deuxième fois !",
                    Icon = "🔄",
                    Order = 2,
                    Position = TipPosition.Bottom
                }
            },

            // Conseils pour le dashboard
            ["dashboard"] = new List<Tip>
            {
                new Tip
                {
                    Id = "dashboard_overview",
                    FeatureKey = "dashboard",
                    Title = "Votre espace personnel",
                    Message = "Retrouvez ici un aperçu de votre activité : annonces, messages, transactions et statistiques.",
                    Icon = "📊",
                    Order = 1,
                    Position = TipPosition.Top
                }
            },

            // Conseils pour le social
            ["social"] = new List<Tip>
            {
                new Tip
                {
                    Id = "social_community",
                    FeatureKey = "social",
                    Title = "Rejoignez la communauté",
                    Message = "Suivez d'autres utilisateurs pour voir leurs nouvelles annonces et restez connecté !",
                    Icon = "👥",
                    Order = 1,
                    Position = TipPosition.Top
                }
            }
        };

        public TipsService(ILogger<TipsService>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initialise le service en chargeant l'état sauvegardé
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            try
            {
                var json = await SecureStorage.Default.GetAsync(TipStateKey);
                if (!string.IsNullOrEmpty(json))
                {
                    _state = JsonSerializer.Deserialize<TipState>(json) ?? new TipState();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Impossible de charger l'état des conseils, utilisation des valeurs par défaut");
                _state = new TipState();
            }

            _initialized = true;
        }

        /// <summary>
        /// Sauvegarde l'état actuel
        /// </summary>
        private async Task SaveStateAsync()
        {
            try
            {
                _state.LastUpdated = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(_state);
                await SecureStorage.Default.SetAsync(TipStateKey, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur lors de la sauvegarde de l'état des conseils");
            }
        }

        /// <inheritdoc />
        public async Task<TipDisplayConfig?> GetNextTipAsync(string featureKey)
        {
            await EnsureInitializedAsync();

            // Si tous les conseils sont désactivés, retourner null
            if (_state.AllTipsDisabled)
            {
                return null;
            }

            // Vérifier si des conseils existent pour cette fonctionnalité
            if (!_tips.TryGetValue(featureKey, out var tips) || tips.Count == 0)
            {
                return null;
            }

            // Trouver le prochain conseil non vu et non ignoré
            var unseenTips = tips
                .Where(t => !_state.SeenTips.ContainsKey(t.Id) && !_state.DismissedTips.Contains(t.Id))
                .OrderBy(t => t.Order)
                .ToList();

            if (unseenTips.Count == 0)
            {
                return null;
            }

            var nextTip = unseenTips.First();
            var totalTips = tips.Count;
            var currentIndex = tips.IndexOf(nextTip) + 1;

            return new TipDisplayConfig
            {
                Tip = nextTip,
                HasMoreTips = unseenTips.Count > 1,
                CurrentIndex = currentIndex,
                TotalTips = totalTips
            };
        }

        /// <inheritdoc />
        public async Task MarkTipAsSeenAsync(string tipId)
        {
            await EnsureInitializedAsync();

            if (!_state.SeenTips.ContainsKey(tipId))
            {
                _state.SeenTips[tipId] = DateTime.UtcNow;
                await SaveStateAsync();
                _logger?.LogDebug("Conseil {TipId} marqué comme vu", tipId);
            }
        }

        /// <inheritdoc />
        public async Task DismissTipAsync(string tipId)
        {
            await EnsureInitializedAsync();

            _state.DismissedTips.Add(tipId);
            if (!_state.SeenTips.ContainsKey(tipId))
            {
                _state.SeenTips[tipId] = DateTime.UtcNow;
            }
            await SaveStateAsync();
            _logger?.LogDebug("Conseil {TipId} ignoré définitivement", tipId);
        }

        /// <inheritdoc />
        public async Task<bool> HasUnseenTipsAsync(string featureKey)
        {
            await EnsureInitializedAsync();

            if (_state.AllTipsDisabled)
            {
                return false;
            }

            if (!_tips.TryGetValue(featureKey, out var tips))
            {
                return false;
            }

            return tips.Any(t => !_state.SeenTips.ContainsKey(t.Id) && !_state.DismissedTips.Contains(t.Id));
        }

        /// <inheritdoc />
        public async Task ResetAllTipsAsync()
        {
            await EnsureInitializedAsync();

            _state.SeenTips.Clear();
            _state.DismissedTips.Clear();
            _state.AllTipsDisabled = false;
            await SaveStateAsync();
            _logger?.LogInformation("Tous les conseils ont été réinitialisés");
        }

        /// <inheritdoc />
        public async Task SetTipsEnabledAsync(bool enabled)
        {
            await EnsureInitializedAsync();

            _state.AllTipsDisabled = !enabled;
            await SaveStateAsync();
            _logger?.LogInformation("Conseils {State}", enabled ? "activés" : "désactivés");
        }

        /// <inheritdoc />
        public async Task<bool> AreTipsEnabledAsync()
        {
            await EnsureInitializedAsync();
            return !_state.AllTipsDisabled;
        }

        /// <inheritdoc />
        public IEnumerable<Tip> GetTipsForFeature(string featureKey)
        {
            if (_tips.TryGetValue(featureKey, out var tips))
            {
                return tips.OrderBy(t => t.Order);
            }
            return Enumerable.Empty<Tip>();
        }
    }
}


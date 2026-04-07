using System;
using System.Threading.Tasks;

namespace DonTroc.Services
{
    /// <summary>
    /// Arguments pour les événements de publicité
    /// </summary>
    public class AdEventArgs : EventArgs
    {
        public string AdUnitId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public bool IsSuccess { get; set; }
    }

    /// <summary>
    /// Arguments pour les événements de récompense
    /// </summary>
    public class RewardEventArgs : EventArgs
    {
        public string RewardType { get; set; } = string.Empty;
        public double RewardAmount { get; set; }
        public string Provider { get; set; } = string.Empty;
    }

    /// <summary>
    /// Interface unifiée pour les services de publicité
    /// Permet d'abstraire le provider de publicité (AdMob, etc.)
    /// et de basculer facilement entre différents réseaux.
    /// </summary>
    public interface IAdsService
    {
        /// <summary>
        /// Nom du provider actif (AdMob, etc.)
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Initialise le service de publicité
        /// </summary>
        void Initialize();

        /// <summary>
        /// Indique si le service est initialisé
        /// </summary>
        bool IsInitialized { get; }

        #region Bannières

        /// <summary>
        /// Affiche une bannière à un emplacement donné
        /// </summary>
        /// <param name="placement">Identifiant du placement (ex: "bottom", "top")</param>
        /// <returns>True si la bannière est affichée avec succès</returns>
        Task<bool> ShowBannerAsync(string placement = "default");

        /// <summary>
        /// Cache la bannière actuellement affichée
        /// </summary>
        void HideBanner();

        /// <summary>
        /// Indique si une bannière est actuellement visible
        /// </summary>
        bool IsBannerVisible { get; }

        #endregion

        #region Interstitiels

        /// <summary>
        /// Indique si une publicité interstitielle est prête
        /// </summary>
        bool IsInterstitialReady { get; }

        /// <summary>
        /// Affiche une publicité interstitielle
        /// </summary>
        Task ShowInterstitialAsync();

        /// <summary>
        /// Précharge une publicité interstitielle
        /// </summary>
        void PreloadInterstitial();

        #endregion

        #region Vidéos récompensées

        /// <summary>
        /// Indique si une vidéo récompensée est prête
        /// </summary>
        bool IsRewardedReady { get; }

        /// <summary>
        /// Affiche une vidéo récompensée
        /// </summary>
        /// <returns>True si l'utilisateur a regardé la vidéo complète et gagné la récompense</returns>
        Task<bool> ShowRewardedAsync();

        /// <summary>
        /// Précharge une vidéo récompensée
        /// </summary>
        void PreloadRewarded();

        #endregion

        #region Événements

        /// <summary>
        /// Événement déclenché quand une publicité est chargée
        /// </summary>
        event EventHandler<AdEventArgs>? OnAdLoaded;

        /// <summary>
        /// Événement déclenché quand le chargement d'une publicité échoue
        /// </summary>
        event EventHandler<AdEventArgs>? OnAdFailed;

        /// <summary>
        /// Événement déclenché quand une publicité est fermée
        /// </summary>
        event EventHandler<AdEventArgs>? OnAdClosed;

        /// <summary>
        /// Événement déclenché quand l'utilisateur gagne une récompense
        /// </summary>
        event EventHandler<RewardEventArgs>? OnRewardEarned;

        #endregion
    }
}

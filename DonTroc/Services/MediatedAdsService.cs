using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DonTroc.Services
{
    /// <summary>
    /// Interface pour un provider de publicité individuel
    /// </summary>
    public interface IAdsProvider
    {
        string Name { get; }
        int Priority { get; }
        bool IsInitialized { get; }
        void Initialize();
        
        bool IsInterstitialReady { get; }
        bool IsRewardedReady { get; }
        
        Task ShowInterstitialAsync();
        Task<bool> ShowRewardedAsync();
        void PreloadInterstitial();
        void PreloadRewarded();
    }

    /// <summary>
    /// Service de médiation qui gère plusieurs providers de publicité
    /// avec fallback automatique si un provider échoue.
    /// 
    /// Prépare l'intégration future d'IronSource/LevelPlay ou d'autres réseaux.
    /// </summary>
    public class MediatedAdsService : IAdsService
    {
        private readonly List<IAdsProvider> _providers = new();
        private bool _isInitialized;
        private bool _isBannerVisible;

        public string ProviderName => "Mediated";
        public bool IsInitialized => _isInitialized;
        public bool IsBannerVisible => _isBannerVisible;

        public bool IsInterstitialReady => GetReadyProvider(p => p.IsInterstitialReady) != null;
        public bool IsRewardedReady => GetReadyProvider(p => p.IsRewardedReady) != null;

        public event EventHandler<AdEventArgs>? OnAdLoaded;
        public event EventHandler<AdEventArgs>? OnAdFailed;
        public event EventHandler<AdEventArgs>? OnAdClosed;
        public event EventHandler<RewardEventArgs>? OnRewardEarned;

        /// <summary>
        /// Enregistre un nouveau provider de publicité
        /// </summary>
        public void RegisterProvider(IAdsProvider provider)
        {
            _providers.Add(provider);
            // Trier par priorité (plus bas = plus prioritaire)
            _providers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            Debug.WriteLine($"📦 Provider enregistré: {provider.Name} (priorité: {provider.Priority})");
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            Debug.WriteLine("🔄 Initialisation du service de médiation publicitaire...");

            foreach (var provider in _providers)
            {
                try
                {
                    provider.Initialize();
                    Debug.WriteLine($"✅ Provider initialisé: {provider.Name}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Erreur initialisation {provider.Name}: {ex.Message}");
                }
            }

            _isInitialized = true;
            Debug.WriteLine($"✅ Service de médiation initialisé avec {_providers.Count} provider(s)");
        }

        public async Task<bool> ShowBannerAsync(string placement = "default")
        {
            // Les bannières sont gérées différemment (via handlers MAUI)
            // Cette méthode est un placeholder pour une future implémentation
            _isBannerVisible = true;
            Debug.WriteLine($"📢 Bannière affichée (placement: {placement})");
            await Task.CompletedTask;
            return true;
        }

        public void HideBanner()
        {
            _isBannerVisible = false;
            Debug.WriteLine("🚫 Bannière cachée");
        }

        public async Task ShowInterstitialAsync()
        {
            var provider = GetReadyProvider(p => p.IsInterstitialReady);

            if (provider == null)
            {
                Debug.WriteLine("⚠️ Aucun interstitiel disponible");
                OnAdFailed?.Invoke(this, new AdEventArgs
                {
                    Provider = "None",
                    IsSuccess = false,
                    ErrorMessage = "Aucun provider disponible"
                });
                return;
            }

            try
            {
                Debug.WriteLine($"🎬 Affichage interstitiel via {provider.Name}");
                await provider.ShowInterstitialAsync();
                
                OnAdClosed?.Invoke(this, new AdEventArgs
                {
                    Provider = provider.Name,
                    IsSuccess = true
                });

                // Précharger la prochaine pub
                PreloadInterstitial();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erreur interstitiel {provider.Name}: {ex.Message}");
                OnAdFailed?.Invoke(this, new AdEventArgs
                {
                    Provider = provider.Name,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public async Task<bool> ShowRewardedAsync()
        {
            var provider = GetReadyProvider(p => p.IsRewardedReady);

            if (provider == null)
            {
                Debug.WriteLine("⚠️ Aucune vidéo récompensée disponible");
                OnAdFailed?.Invoke(this, new AdEventArgs
                {
                    Provider = "None",
                    IsSuccess = false,
                    ErrorMessage = "Aucun provider disponible"
                });
                return false;
            }

            try
            {
                Debug.WriteLine($"🎬 Affichage vidéo récompensée via {provider.Name}");
                var result = await provider.ShowRewardedAsync();

                if (result)
                {
                    OnRewardEarned?.Invoke(this, new RewardEventArgs
                    {
                        Provider = provider.Name,
                        RewardType = "default",
                        RewardAmount = 1
                    });
                }

                // Précharger la prochaine pub
                PreloadRewarded();
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Erreur vidéo récompensée {provider.Name}: {ex.Message}");
                OnAdFailed?.Invoke(this, new AdEventArgs
                {
                    Provider = provider.Name,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
                return false;
            }
        }

        public void PreloadInterstitial()
        {
            foreach (var provider in _providers)
            {
                if (provider.IsInitialized && !provider.IsInterstitialReady)
                {
                    try
                    {
                        provider.PreloadInterstitial();
                        Debug.WriteLine($"🔄 Préchargement interstitiel: {provider.Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"⚠️ Erreur préchargement {provider.Name}: {ex.Message}");
                    }
                }
            }
        }

        public void PreloadRewarded()
        {
            foreach (var provider in _providers)
            {
                if (provider.IsInitialized && !provider.IsRewardedReady)
                {
                    try
                    {
                        provider.PreloadRewarded();
                        Debug.WriteLine($"🔄 Préchargement vidéo récompensée: {provider.Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"⚠️ Erreur préchargement {provider.Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Trouve le premier provider prêt selon un prédicat
        /// </summary>
        private IAdsProvider? GetReadyProvider(Func<IAdsProvider, bool> predicate)
        {
            foreach (var provider in _providers)
            {
                if (provider.IsInitialized && predicate(provider))
                {
                    return provider;
                }
            }
            return null;
        }
    }
}

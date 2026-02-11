using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DonTroc.Services.Providers
{
    /// <summary>
    /// Provider Unity LevelPlay (anciennement IronSource) pour la médiation publicitaire
    /// 
    /// ⚠️ EN ATTENTE DE VALIDATION DU COMPTE IRONSOURCE
    /// 
    /// Votre compte doit être approuvé par Unity avant que les pubs s'affichent.
    /// Envoyez un email à: ironsource-account-review@unity3d.com
    /// Sujet: "ironSource account review details"
    /// Incluez: lien Play Store, email du compte, ticket #00880519
    /// 
    /// Une fois approuvé, changez ACCOUNT_VALIDATED = true
    /// </summary>
    public class IronSourceProvider : IAdsProvider
    {
        // ══════════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ══════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Mettre à TRUE une fois que Unity a validé votre compte IronSource
        /// </summary>
        private const bool ACCOUNT_VALIDATED = false;
        
        private const string AppKey = "2525f980d";
        
        // État du provider
        private bool _isInitialized;
        private bool _isInterstitialReady;
        private bool _isRewardedReady;
        private TaskCompletionSource<bool>? _rewardedTcs;
        private TaskCompletionSource<bool>? _interstitialTcs;
        
        public string Name => "LevelPlay";
        public int Priority => 2; // Après AdMob (priorité 1)
        
        public bool IsInitialized => _isInitialized;
        public bool IsInterstitialReady => _isInterstitialReady;
        public bool IsRewardedReady => _isRewardedReady;

        public IronSourceProvider()
        {
            Debug.WriteLine("📦 LevelPlayProvider créé");
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            
            // ⚠️ Compte non validé par Unity - ne pas initialiser
            if (!ACCOUNT_VALIDATED)
            {
                Debug.WriteLine("⚠️ LevelPlay: Compte en attente de validation Unity");
                Debug.WriteLine("   → Envoyez un email à ironsource-account-review@unity3d.com");
                Debug.WriteLine("   → Sujet: 'ironSource account review details'");
                Debug.WriteLine("   → Incluez: lien Play Store, email, ticket #00880519");
                Debug.WriteLine("   → Une fois approuvé, mettez ACCOUNT_VALIDATED = true dans IronSourceProvider.cs");
                return;
            }
            
            if (AppKey == "VOTRE_APP_KEY_IRONSOURCE" || string.IsNullOrEmpty(AppKey))
            {
                Debug.WriteLine("⚠️ LevelPlay: App Key non configurée !");
                Debug.WriteLine("   → Modifiez IronSourceProvider.cs et ajoutez votre App Key");
                Debug.WriteLine("   → Obtenez-la depuis https://unity.com/products/mediation");
                return;
            }

            // TODO: Une fois le compte validé, implémenter l'initialisation IronSource SDK
            Debug.WriteLine("ℹ️ LevelPlay: En attente de l'implémentation SDK");
        }

        public void PreloadInterstitial()
        {
            if (!_isInitialized) return;
            
            // TODO: Une fois le compte validé, implémenter le préchargement
            Debug.WriteLine("ℹ️ LevelPlay: PreloadInterstitial - En attente");
        }

        public void PreloadRewarded()
        {
            if (!_isInitialized) return;
            
            // TODO: Une fois le compte validé, implémenter le préchargement
            Debug.WriteLine("ℹ️ LevelPlay: PreloadRewarded - En attente");
        }

        public async Task ShowInterstitialAsync()
        {
            if (!_isInitialized || !_isInterstitialReady)
            {
                Debug.WriteLine("⚠️ LevelPlay: Interstitiel non prêt");
                return;
            }

            // TODO: Une fois le compte validé, implémenter l'affichage
            await Task.CompletedTask;
        }

        public async Task<bool> ShowRewardedAsync()
        {
            if (!_isInitialized || !_isRewardedReady)
            {
                Debug.WriteLine("⚠️ LevelPlay: Vidéo récompensée non prête");
                return false;
            }

            // TODO: Une fois le compte validé, implémenter l'affichage
            return await Task.FromResult(false);
        }

        // ══════════════════════════════════════════════════════════════════
        // Callbacks internes (seront utilisés quand le SDK sera activé)
        // ══════════════════════════════════════════════════════════════════
        
        internal void OnInitSuccess()
        {
            _isInitialized = true;
            Debug.WriteLine("✅ LevelPlay: Initialisé avec succès");
            
            PreloadInterstitial();
            PreloadRewarded();
        }
        
        internal void OnInitFailed(string error)
        {
            Debug.WriteLine($"❌ LevelPlay: Erreur init - {error}");
        }

        internal void OnInterstitialReady()
        {
            _isInterstitialReady = true;
            Debug.WriteLine("✅ LevelPlay: Interstitiel prêt");
        }

        internal void OnInterstitialFailed(string error)
        {
            _isInterstitialReady = false;
            Debug.WriteLine($"❌ LevelPlay: Erreur interstitiel - {error}");
        }

        internal void OnInterstitialClosed()
        {
            _isInterstitialReady = false;
            _interstitialTcs?.TrySetResult(true);
            PreloadInterstitial();
            Debug.WriteLine("📺 LevelPlay: Interstitiel fermé");
        }

        internal void OnRewardedReady()
        {
            _isRewardedReady = true;
            Debug.WriteLine("✅ LevelPlay: Vidéo récompensée prête");
        }
        
        internal void OnRewardedFailed(string error)
        {
            _isRewardedReady = false;
            Debug.WriteLine($"❌ LevelPlay: Erreur rewarded - {error}");
        }

        internal void OnRewardedRewarded()
        {
            Debug.WriteLine("🎁 LevelPlay: Récompense gagnée !");
            _rewardedTcs?.TrySetResult(true);
        }

        internal void OnRewardedClosed()
        {
            _isRewardedReady = false;
            _rewardedTcs?.TrySetResult(false);
            PreloadRewarded();
            Debug.WriteLine("📺 LevelPlay: Vidéo récompensée fermée");
        }
    }
}

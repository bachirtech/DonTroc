using System;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Google.MobileAds;
using DonTroc.Services;

namespace DonTroc.Platforms.iOS;

/// <summary>
/// Implémentation native d'AdMob pour iOS
/// </summary>
public class AdMobNativeService : NSObject, IAdMobService
{
    private RewardedAd? _rewardedAd;
    private InterstitialAd? _interstitialAd;
    private bool _isInitialized;
    private TaskCompletionSource<bool>? _rewardedAdCompletionSource;
    private TaskCompletionSource<bool>? _interstitialAdCompletionSource;
    
    // IDs de production AdMob pour iOS
    // TODO: Remplacer par vos vrais IDs iOS depuis la console AdMob
    private const string RewardedAdUnitId = "ca-app-pub-5085236088670848/7248212501"; //  l'ID iOS
    private const string InterstitialAdUnitId = "ca-app-pub-5085236088670848/5659069719"; // iOS
    private const string BannerAdUnitId = "ca-app-pub-5085236088670848/7822198620"; //  l'ID iOS
    
    // IDs de test AdMob pour iOS (pour le développement)
    private const string TestRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
    private const string TestInterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
    private const string TestBannerAdUnitId = "ca-app-pub-3940256099942544/2934735716";
    
    public void Initialize()
    {
        if (_isInitialized) return;
        
        try
        {
            // AdMob est déjà initialisé dans AppDelegate.FinishedLaunching
            _isInitialized = true;
            
            // Précharger les publicités
            LoadRewardedAd();
            LoadInterstitialAd();
            
            System.Diagnostics.Debug.WriteLine("[AdMob iOS] Initialized successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdMob iOS] Error initializing: {ex.Message}");
        }
    }
    
    public void LogApiLimitation()
    {
        System.Diagnostics.Debug.WriteLine("[AdMob iOS] API limitations logged");
    }
    
    #region Rewarded Ads
    
    public bool IsRewardedAdReady()
    {
        return _rewardedAd != null;
    }
    
    public void LoadRewardedAd()
    {
        try
        {
            var adUnitId = GetRewardedAdUnitId();
            var request = Request.GetDefaultRequest();
            
            RewardedAd.Load(adUnitId, request, (ad, error) =>
            {
                if (error != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdMob iOS] Failed to load rewarded ad: {error.LocalizedDescription}");
                    _rewardedAd = null;
                    return;
                }
                
                _rewardedAd = ad;
                System.Diagnostics.Debug.WriteLine("[AdMob iOS] Rewarded ad loaded successfully");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdMob iOS] Error loading rewarded ad: {ex.Message}");
        }
    }
    
    public Task<bool> ShowRewardedAdAsync()
    {
        _rewardedAdCompletionSource = new TaskCompletionSource<bool>();
        
        try
        {
            if (_rewardedAd == null)
            {
                System.Diagnostics.Debug.WriteLine("[AdMob iOS] Rewarded ad not ready");
                _rewardedAdCompletionSource.SetResult(false);
                LoadRewardedAd();
                return _rewardedAdCompletionSource.Task;
            }
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var viewController = GetRootViewController();
                    if (viewController != null && _rewardedAd != null)
                    {
                        _rewardedAd.Present(viewController, () =>
                        {
                            // L'utilisateur a regardé la publicité complètement
                            System.Diagnostics.Debug.WriteLine("[AdMob iOS] User earned reward");
                            _rewardedAdCompletionSource?.TrySetResult(true);
                            
                            // Recharger la publicité après affichage
                            _rewardedAd = null;
                            LoadRewardedAd();
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[AdMob iOS] No root view controller");
                        _rewardedAdCompletionSource?.TrySetResult(false);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdMob iOS] Error showing rewarded ad: {ex.Message}");
                    _rewardedAdCompletionSource?.TrySetResult(false);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdMob iOS] Error in ShowRewardedAdAsync: {ex.Message}");
            _rewardedAdCompletionSource.SetResult(false);
        }
        
        return _rewardedAdCompletionSource.Task;
    }
    
    #endregion
    
    #region Interstitial Ads
    
    public bool IsInterstitialAdReady()
    {
        return _interstitialAd != null;
    }
    
    public void LoadInterstitialAd()
    {
        try
        {
            var adUnitId = GetInterstitialAdUnitId();
            var request = Request.GetDefaultRequest();
            
            InterstitialAd.Load(adUnitId, request, (ad, error) =>
            {
                if (error != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdMob iOS] Failed to load interstitial ad: {error.LocalizedDescription}");
                    _interstitialAd = null;
                    return;
                }
                
                _interstitialAd = ad;
                System.Diagnostics.Debug.WriteLine("[AdMob iOS] Interstitial ad loaded successfully");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdMob iOS] Error loading interstitial ad: {ex.Message}");
        }
    }
    
    public async Task ShowInterstitialAdAsync()
    {
        _interstitialAdCompletionSource = new TaskCompletionSource<bool>();
        
        try
        {
            if (_interstitialAd == null)
            {
                System.Diagnostics.Debug.WriteLine("[AdMob iOS] Interstitial ad not ready");
                _interstitialAdCompletionSource.SetResult(false);
                LoadInterstitialAd();
                await _interstitialAdCompletionSource.Task;
                return;
            }
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var viewController = GetRootViewController();
                    if (viewController != null && _interstitialAd != null)
                    {
                        _interstitialAd.Present(viewController);
                        _interstitialAdCompletionSource?.TrySetResult(true);
                        
                        // Recharger la publicité après affichage
                        _interstitialAd = null;
                        LoadInterstitialAd();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[AdMob iOS] No root view controller");
                        _interstitialAdCompletionSource?.TrySetResult(false);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdMob iOS] Error showing interstitial ad: {ex.Message}");
                    _interstitialAdCompletionSource?.TrySetResult(false);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdMob iOS] Error in ShowInterstitialAdAsync: {ex.Message}");
            _interstitialAdCompletionSource.SetResult(false);
        }
        
        await _interstitialAdCompletionSource.Task;
    }
    
    #endregion
    
    #region Banner Ads
    
    public object? CreateBannerView()
    {
        try
        {
            var adUnitId = GetBannerAdUnitId();
            var bannerView = new BannerView(AdSizeCons.Banner, new CoreGraphics.CGPoint(0, 0))
            {
                AdUnitId = adUnitId,
                RootViewController = GetRootViewController()
            };
            
            bannerView.LoadRequest(Request.GetDefaultRequest());
            System.Diagnostics.Debug.WriteLine("[AdMob iOS] Banner view created");
            
            return bannerView;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdMob iOS] Error creating banner view: {ex.Message}");
            return null;
        }
    }
    
    #endregion
    
    #region Helpers
    
    private UIViewController? GetRootViewController()
    {
        try
        {
#pragma warning disable CA1422
            var scenes = UIApplication.SharedApplication.ConnectedScenes;
            foreach (var scene in scenes)
            {
                if (scene is UIWindowScene windowScene)
                {
                    foreach (var window in windowScene.Windows)
                    {
                        if (window.IsKeyWindow)
                        {
                            return window.RootViewController;
                        }
                    }
                }
            }
            
            // Fallback pour les anciennes versions
            return UIApplication.SharedApplication.KeyWindow?.RootViewController;
#pragma warning restore CA1422
        }
        catch
        {
            return null;
        }
    }
    
    private string GetRewardedAdUnitId()
    {
#if DEBUG
        return TestRewardedAdUnitId;
#else
        return RewardedAdUnitId;
#endif
    }
    
    private string GetInterstitialAdUnitId()
    {
#if DEBUG
        return TestInterstitialAdUnitId;
#else
        return InterstitialAdUnitId;
#endif
    }
    
    private string GetBannerAdUnitId()
    {
#if DEBUG
        return TestBannerAdUnitId;
#else
        return BannerAdUnitId;
#endif
    }
    
    #endregion
}


﻿﻿﻿using System;
   using DonTroc.Services;
using DonTroc.Views;
using System.ComponentModel;
using System.Linq;
using DonTroc.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace DonTroc;

public partial class AppShell : Shell
{
    private readonly UnreadMessageService _unreadMessageService;

    public int TotalUnreadCount => _unreadMessageService?.TotalUnreadCount ?? 0;

    public AppShell(UnreadMessageService unreadMessageService) // Constructeur
    {
        // ⚠️ Initialiser le service AVANT BindingContext pour éviter NullReferenceException
        _unreadMessageService = unreadMessageService;
        _unreadMessageService.PropertyChanged += OnUnreadMessageServicePropertyChanged;

        InitializeComponent();

#if ANDROID
        // Le template d'onglet personnalisé n'est appliqué que sur Android.
        // Sur iOS, Shell utilise une UITabBar native qui ne supporte pas
        // correctement Shell.ItemTemplate (bande grise + NRE ShellTableViewSource).
        ItemTemplate = (DataTemplate)Resources["AndroidShellItemTemplate"];
#endif

        // Le BindingContext est défini sur lui-même pour permettre les liaisons de données dans le XAML
        BindingContext = this;

        // Enregistre la route pour la page de création d'annonce
        Routing.RegisterRoute(nameof(CreationAnnonceView), typeof(CreationAnnonceView));
        // NOTE: AnnoncesView et DashboardView sont déjà définis comme Route dans AppShell.xaml (ShellContent)
        // Ne PAS les re-enregistrer ici, sinon Shell voit des routes dupliquées → "Ambiguous routes" au retour
        Routing.RegisterRoute(nameof(TransactionsView), typeof(TransactionsView)); // Enregistre la route pour la page des transactions
        Routing.RegisterRoute(nameof(TransactionDetailsView), typeof(TransactionDetailsView)); // Enregistre la route pour les détails de transaction
        Routing.RegisterRoute(nameof(ImageViewerView), typeof(ImageViewerView)); // Enregistre la route pour la visionneuse d'images
        // Enregistre la route pour la page de détail d'annonce
        Routing.RegisterRoute(nameof(AnnonceDetailView), typeof(AnnonceDetailView));

        // Enregistrement des autres routes importantes
        Routing.RegisterRoute(nameof(ChatView), typeof(ChatView));
        // Enregistre la route pour la page de modification du profil
        Routing.RegisterRoute(nameof(EditProfileView), typeof(EditProfileView));
        // Enregistre la route pour la page de modification d'annonce
        Routing.RegisterRoute(nameof(EditAnnonceView), typeof(EditAnnonceView));
        // Enregistre la route pour le système de notation et avis
        Routing.RegisterRoute(nameof(RatingView), typeof(RatingView));
        // Enregistre la route pour la vue carte interactive
        Routing.RegisterRoute(nameof(MapView), typeof(MapView));
        // Enregistre la route pour la page de récompenses et gamification
        Routing.RegisterRoute(nameof(RewardsPage), typeof(RewardsPage));
        // Enregistre la route pour la page de quiz
        Routing.RegisterRoute(nameof(QuizPage), typeof(QuizPage));
        // Enregistre la route pour la roue de la fortune
        Routing.RegisterRoute(nameof(WheelOfFortunePage), typeof(WheelOfFortunePage));
        // Enregistre la route pour la page de tous les badges
        Routing.RegisterRoute(nameof(AllBadgesPage), typeof(AllBadgesPage));
        // Route pour le classement
        Routing.RegisterRoute(nameof(LeaderboardView), typeof(LeaderboardView));
        // Route pour la page sociale (amis, parrainage)
        Routing.RegisterRoute(nameof(SocialView), typeof(SocialView));

        // Routes pour le système de propositions de troc
        Routing.RegisterRoute(nameof(TradeProposalPage), typeof(TradeProposalPage));
        Routing.RegisterRoute(nameof(TradeProposalsListPage), typeof(TradeProposalsListPage));

        // Routes pour les événements & trocs groupés
        Routing.RegisterRoute(nameof(EventsListView), typeof(EventsListView));
        Routing.RegisterRoute(nameof(EventDetailView), typeof(EventDetailView));
        Routing.RegisterRoute(nameof(CreationEvenementView), typeof(CreationEvenementView));
        Routing.RegisterRoute(nameof(MesEvenementsView), typeof(MesEvenementsView));
        Routing.RegisterRoute(nameof(EventsMapView), typeof(EventsMapView));
        
        // Routes pour le panneau d'administration
        Routing.RegisterRoute(nameof(AdminDashboardPage), typeof(AdminDashboardPage));
        Routing.RegisterRoute(nameof(UserManagementPage), typeof(UserManagementPage));
        Routing.RegisterRoute(nameof(AdminLogsPage), typeof(AdminLogsPage));
        Routing.RegisterRoute(nameof(ModerationPage), typeof(ModerationPage));
        Routing.RegisterRoute(nameof(AdminSetupPage), typeof(AdminSetupPage));
        Routing.RegisterRoute(nameof(AdminEventsPage), typeof(AdminEventsPage));

        UpdateMessagesBadge();
    }

#if IOS
    /// <summary>
    /// 🛠️ FIX iOS bande grise/Ardoise translucide sous la TabBar.
    /// Une fois le handler iOS attaché, on accède directement à l'UITabBar
    /// native pour forcer son apparence OPAQUE avec une couleur explicite
    /// (BeigeClair). Sans ça, MAUI Shell laisse la UITabBar en translucide
    /// → la couleur grise apparaît mélangée au contenu de la page.
    /// </summary>
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        // Reporter au prochain tick pour s'assurer que UITabBarController est créé
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(ApplyIosTabBarFix);
    }

    private static void ApplyIosTabBarFix()
    {
        try
        {
            UIKit.UITabBar? tabBar = null;
            // Trouver le UITabBarController dans la hiérarchie
            var rootVc = UIKit.UIApplication.SharedApplication.KeyWindow?.RootViewController;
            tabBar = FindTabBar(rootVc);
            if (tabBar == null) return;

            var bg = UIKit.UIColor.FromRGB(0xF6 / 255f, 0xF1 / 255f, 0xEB / 255f); // BeigeClair
            if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                var ap = new UIKit.UITabBarAppearance();
                ap.ConfigureWithOpaqueBackground();
                ap.BackgroundColor = bg;
                ap.ShadowColor = UIKit.UIColor.Clear;
                tabBar.StandardAppearance = ap;
                if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
                {
                    tabBar.ScrollEdgeAppearance = ap;
                }
            }
            tabBar.Translucent = false;
            tabBar.BarTintColor = bg;
            tabBar.BackgroundColor = bg;
            tabBar.TintColor = UIKit.UIColor.FromRGB(0xD9 / 255f, 0x8C / 255f, 0x6A / 255f);          // Terracotta (sélectionné)
            tabBar.UnselectedItemTintColor = UIKit.UIColor.FromRGB(0x6B / 255f, 0x7A / 255f, 0x8F / 255f); // Ardoise (icônes non sélectionnées)
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppShell] ApplyIosTabBarFix erreur: {ex.Message}");
        }
    }

    private static UIKit.UITabBar? FindTabBar(UIKit.UIViewController? vc)
    {
        if (vc == null) return null;
        if (vc is UIKit.UITabBarController tbc) return tbc.TabBar;
        foreach (var child in vc.ChildViewControllers)
        {
            var r = FindTabBar(child);
            if (r != null) return r;
        }
        if (vc.PresentedViewController != null)
        {
            var r = FindTabBar(vc.PresentedViewController);
            if (r != null) return r;
        }
        return null;
    }
#endif

    private void OnUnreadMessageServicePropertyChanged(object? sender, PropertyChangedEventArgs e) // Méthode pour gérer les changements de propriété dans le service de messages non lus
    {
        if (e.PropertyName == nameof(UnreadMessageService.TotalUnreadCount))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(TotalUnreadCount));
#if IOS
                UpdateIosMessagesBadge();
#endif
            });
        }
    }

    private void UpdateMessagesBadge() // Méthode pour mettre a jour le badge de messages 
    {
        OnPropertyChanged(nameof(TotalUnreadCount));
#if IOS
        UpdateIosMessagesBadge();
#endif
    }

#if IOS
    // Sur iOS la TabBar native ne lit pas le DataTemplate custom : on pose le badge
    // directement sur le Tab "Messages" via les propriétés natives Shell.
    private void UpdateIosMessagesBadge()
    {
        var messagesTab = this.Items
            .OfType<TabBar>()
            .SelectMany(tb => tb.Items)
            .FirstOrDefault(t => t.Title == "Messages");

        if (messagesTab == null) return;

        var count = TotalUnreadCount;
        // NB : pour utiliser TabBarBadgeText il faut que le package Shell le supporte
        // (depuis MAUI 8+ via les propriétés Shell). Sinon le badge reste invisible
        // côté iOS sans casser l'app.
        // On évite l'API non disponible : on met simplement à jour le titre avec un compteur.
        messagesTab.Title = count > 0 ? $"Messages ({count})" : "Messages";
    }
#endif
}
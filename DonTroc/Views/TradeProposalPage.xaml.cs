using DonTroc.Services;
using DonTroc.ViewModels;

namespace DonTroc.Views;

public partial class TradeProposalPage : ContentPage
{
    private bool _entranceAnimationDone;

    public TradeProposalPage(TradeProposalViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;

        if (vm != null)
            vm.PropertyChanged += OnVmPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_entranceAnimationDone) return;
        _entranceAnimationDone = true;

        try
        {
            // 🎬 Entrée scénarisée : carte cible glisse depuis la gauche, icône rotation pulse
            if (TargetCardBorder != null)
            {
                TargetCardBorder.Opacity = 0;
                TargetCardBorder.TranslationX = -60;
                await Task.WhenAll(
                    TargetCardBorder.FadeTo(1, 350, Easing.CubicOut),
                    TargetCardBorder.TranslateTo(0, 0, 450, Easing.SpringOut)
                );
            }

            if (SwapIconLabel != null)
            {
                SwapIconLabel.Opacity = 0;
                SwapIconLabel.Scale = 0.4;
                await Task.WhenAll(
                    SwapIconLabel.FadeTo(1, 250),
                    SwapIconLabel.ScaleTo(1, 350, Easing.SpringOut)
                );
                // 🔄 Rotation continue subtile pour signaler l'échange
                AnimationService.StartRotationAnimation(SwapIconLabel, 4000);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TradeProposalPage] Anim erreur: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (SwapIconLabel != null) AnimationService.StopRotationAnimation(SwapIconLabel);
        if (ExchangeArrow != null) ExchangeArrow.AbortAnimation("Pulse");
        if (BindingContext is TradeProposalViewModel vm)
            vm.PropertyChanged -= OnVmPropertyChanged;
    }

    private async void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TradeProposalViewModel.HasSelection)) return;
        if (BindingContext is not TradeProposalViewModel vm || !vm.HasSelection) return;

        try
        {
            // ✨ Quand l'utilisateur choisit son objet : animation grand zoom du preview
            if (PreviewBorder != null)
            {
                PreviewBorder.Opacity = 0;
                PreviewBorder.Scale = 0.85;
                await Task.WhenAll(
                    PreviewBorder.FadeTo(1, 280),
                    PreviewBorder.ScaleTo(1, 380, Easing.SpringOut)
                );

                // ↔ Flèche d'échange qui pulse en boucle pour célébrer le match
                if (ExchangeArrow != null)
                {
                    var pulse = new Animation();
                    pulse.Add(0, 0.5, new Animation(v => ExchangeArrow.Scale = v, 1.0, 1.3, Easing.CubicOut));
                    pulse.Add(0.5, 1, new Animation(v => ExchangeArrow.Scale = v, 1.3, 1.0, Easing.CubicIn));
                    pulse.Commit(ExchangeArrow, "Pulse", 16, 1200, repeat: () => true);
                }

                try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TradeProposalPage] Preview anim erreur: {ex.Message}");
        }
    }
}

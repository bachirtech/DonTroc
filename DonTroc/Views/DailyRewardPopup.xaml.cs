using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DonTroc.Models;
using DonTroc.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DonTroc.Views;

/// <summary>
/// Pop-up de récompense quotidienne affiché à l'ouverture de l'app.
/// Affiche la progression sur 7 jours, met en avant la récompense du jour,
/// et permet de la réclamer directement.
/// </summary>
public partial class DailyRewardPopup : ContentView
{
    private GamificationService? _gamificationService;
    private AuthService? _authService;
    private TaskCompletionSource<bool>? _tcs;
    private List<DailyReward>? _rewardsStatus;
    private int _currentDay;

    public DailyRewardPopup()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Vérifie si la récompense est disponible et affiche le pop-up.
    /// Retourne true si la récompense a été réclamée, false sinon.
    /// </summary>
    public async Task<bool> TryShowAsync(GamificationService gamificationService, AuthService authService)
    {
        _gamificationService = gamificationService;
        _authService = authService;

        try
        {
            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId)) return false;

            // Vérifier si la récompense peut être réclamée
            var canClaim = await _gamificationService.CanClaimDailyRewardAsync(userId);
            if (!canClaim) return false;

            // Charger le statut des 7 jours
            _rewardsStatus = await _gamificationService.GetDailyRewardsStatusAsync(userId);
            var profile = await _gamificationService.GetUserProfileAsync(userId);

            // Calculer le jour actuel du streak
            _currentDay = Math.Max(1, Math.Min((profile.DailyStreak % 7) + 1, 7));

            // Mettre à jour l'UI
            UpdateDaysDisplay();
            UpdateTodayReward();
            UpdateStreakLabel(profile.DailyStreak);

            // Afficher avec animation
            _tcs = new TaskCompletionSource<bool>();
            await ShowWithAnimationAsync();

            // Attendre que l'utilisateur interagisse
            return await _tcs.Task;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyRewardPopup] Erreur TryShowAsync: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Met à jour l'affichage des 7 jours (claimed, today, locked).
    /// </summary>
    private void UpdateDaysDisplay()
    {
        if (_rewardsStatus == null) return;

        var dayBorders = new[] { Day1, Day2, Day3, Day4, Day5, Day6, Day7 };

        for (var i = 0; i < Math.Min(_rewardsStatus.Count, dayBorders.Length); i++)
        {
            var reward = _rewardsStatus[i];
            var border = dayBorders[i];

            if (reward.IsClaimed)
            {
                // Jour déjà réclamé → vert
                border.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#2A4D22")
                    : Color.FromArgb("#E0F5D0");
                border.Opacity = 0.7;
            }
            else if (reward.IsToday)
            {
                // Jour actuel → mis en avant avec bordure terracotta
                border.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#4D3320")
                    : Color.FromArgb("#FFF0E0");
                border.Stroke = new SolidColorBrush(
                    Application.Current?.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#E5A07E")
                        : Color.FromArgb("#D98C6A"));
                border.StrokeThickness = 2;
                border.Scale = 1.08;
            }
            else
            {
                // Jour verrouillé → grisé
                border.Opacity = 0.4;
            }
        }
    }

    /// <summary>
    /// Met à jour la carte de la récompense du jour.
    /// </summary>
    private void UpdateTodayReward()
    {
        if (_rewardsStatus == null) return;

        var todayReward = _rewardsStatus.Find(r => r.IsToday);
        if (todayReward == null) return;

        TodayRewardIcon.Text = todayReward.Icon;
        TodayRewardTitle.Text = $"Récompense Jour {todayReward.Day}";

        TodayRewardDescription.Text = todayReward.Type switch
        {
            WheelRewardType.Xp => $"+{todayReward.Value} XP",
            WheelRewardType.BoostCredits => $"+{todayReward.Value} crédit(s) boost 🚀",
            _ => $"+{todayReward.Value}"
        };
    }

    /// <summary>
    /// Met à jour le label du streak.
    /// </summary>
    private void UpdateStreakLabel(int streak)
    {
        if (streak <= 0)
        {
            StreakLabel.Text = "Votre première récompense !";
            SubtitleLabel.Text = "Revenez chaque jour pour de meilleures récompenses";
        }
        else if (streak == 1)
        {
            StreakLabel.Text = "Série de 1 jour 🔥";
            SubtitleLabel.Text = "Continuez demain pour augmenter votre série !";
        }
        else
        {
            StreakLabel.Text = $"Série de {streak} jours 🔥";
            SubtitleLabel.Text = streak >= 7
                ? "Incroyable ! Vous êtes en feu !"
                : $"Encore {7 - (streak % 7)} jour(s) pour le bonus Jour 7 !";
        }
    }

    // ═══════════════════════════════════════
    // ANIMATIONS
    // ═══════════════════════════════════════

    private async Task ShowWithAnimationAsync()
    {
        IsVisible = true;
        Opacity = 1;

        // Phase 1 : Carte apparaît (scale + fade)
        await Task.WhenAll(
            PopupCard.ScaleTo(1.0, 450, Easing.SpringOut),
            PopupCard.FadeTo(1, 300, Easing.CubicOut)
        );

        // Phase 2 : Fire icon bounce
        await Task.WhenAll(
            FireIcon.FadeTo(1, 250),
            FireIcon.ScaleTo(1.2, 350, Easing.BounceOut)
        );
        await FireIcon.ScaleTo(1.0, 150, Easing.CubicOut);

        // Phase 3 : Titre + sous-titre
        await Task.WhenAll(
            StreakLabel.FadeTo(1, 300),
            SubtitleLabel.FadeTo(1, 300)
        );

        // Phase 4 : Grille des jours
        await DaysGrid.FadeTo(1, 350, Easing.CubicOut);

        // Phase 5 : Carte récompense du jour (pop)
        await Task.WhenAll(
            TodayRewardCard.FadeTo(1, 300),
            TodayRewardCard.ScaleTo(1.0, 400, Easing.SpringOut)
        );

        // Phase 6 : Boutons
        await Task.WhenAll(
            ClaimButton.FadeTo(1, 300, Easing.CubicOut),
            LaterLabel.FadeTo(1, 300, Easing.CubicOut)
        );
    }

    private async Task HideWithAnimationAsync()
    {
        await Task.WhenAll(
            PopupCard.ScaleTo(0.8, 250, Easing.CubicIn),
            PopupCard.FadeTo(0, 200, Easing.CubicIn)
        );

        IsVisible = false;
        ResetAnimationValues();
    }

    private async Task PlayClaimAnimationAsync()
    {
        // Pulse le bouton
        await ClaimButton.ScaleTo(0.9, 100);
        await ClaimButton.ScaleTo(1.05, 150, Easing.BounceOut);
        await ClaimButton.ScaleTo(1.0, 100);

        // Confettis
        ConfettiGrid.IsVisible = true;
        var confettis = new[] { Confetti1, Confetti2, Confetti3, Confetti4, Confetti5, Confetti6 };
        var tasks = new List<Task>();

        var random = new Random();
        foreach (var confetti in confettis)
        {
            confetti.Opacity = 1;
            var endX = random.Next(-150, 150);
            var endY = random.Next(-200, -50);
            var duration = (uint)random.Next(600, 1200);

            tasks.Add(Task.WhenAll(
                confetti.TranslateTo(endX, endY, duration, Easing.CubicOut),
                confetti.RotateTo(random.Next(180, 720), duration),
                confetti.FadeTo(0, duration, Easing.CubicIn)
            ));
        }

        await Task.WhenAll(tasks);
        ConfettiGrid.IsVisible = false;

        // Reset confettis
        foreach (var confetti in confettis)
        {
            confetti.TranslationX = 0;
            confetti.TranslationY = 0;
            confetti.Rotation = 0;
            confetti.Opacity = 0;
        }
    }

    private void ResetAnimationValues()
    {
        PopupCard.Scale = 0.5;
        PopupCard.Opacity = 0;
        FireIcon.Scale = 0.3;
        FireIcon.Opacity = 0;
        StreakLabel.Opacity = 0;
        SubtitleLabel.Opacity = 0;
        DaysGrid.Opacity = 0;
        TodayRewardCard.Opacity = 0;
        TodayRewardCard.Scale = 0.8;
        ClaimButton.Opacity = 0;
        LaterLabel.Opacity = 0;
    }

    // ═══════════════════════════════════════
    // ÉVÉNEMENTS
    // ═══════════════════════════════════════

    private async void OnClaimButtonClicked(object? sender, EventArgs e)
    {
        if (_gamificationService == null || _authService == null) return;

        try
        {
            ClaimButton.IsEnabled = false;
            ClaimButton.Text = "⏳ Chargement...";

            var userId = _authService.GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            var notification = await _gamificationService.ClaimDailyRewardAsync(userId);

            if (notification != null)
            {
                // Mettre à jour le bouton avec le résultat
                ClaimButton.Text = $"✅ {notification.Message}";
                ClaimButton.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#5A7A3E")
                    : Color.FromArgb("#A8C686");

                // Jouer l'animation de claim
                await PlayClaimAnimationAsync();

                // Pause pour voir le résultat
                await Task.Delay(1500);
            }

            await HideWithAnimationAsync();
            _tcs?.TrySetResult(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DailyRewardPopup] Erreur claim: {ex.Message}");
            ClaimButton.Text = "❌ Erreur, réessayez";
            ClaimButton.IsEnabled = true;
        }
    }

    private async void OnLaterTapped(object? sender, EventArgs e)
    {
        await HideWithAnimationAsync();
        _tcs?.TrySetResult(false);
    }

    private async void OnOverlayTapped(object? sender, EventArgs e)
    {
        await HideWithAnimationAsync();
        _tcs?.TrySetResult(false);
    }
}


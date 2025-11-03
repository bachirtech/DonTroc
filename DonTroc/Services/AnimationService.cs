using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using DonTroc.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace DonTroc.Services;

/// <summary>
/// Service pour gérer les animations et transitions dans l'application
/// </summary>
public class AnimationService
{
    private readonly ILogger<AnimationService>? _logger;
    private GamificationAnimationView? _currentGamificationAnimation;

    public AnimationService(ILogger<AnimationService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Affiche une animation de récompense de gamification
    /// </summary>
    public async Task ShowGamificationRewardAsync(string title, string description, int points, string icon = "🏆")
    {
        try
        {
            _logger?.LogInformation($"Démarrage animation gamification: {title}");
            
            // S'assurer qu'on est sur le thread principal
            if (!MainThread.IsMainThread)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ShowGamificationRewardInternalAsync(title, description, points, icon);
                });
            }
            else
            {
                await ShowGamificationRewardInternalAsync(title, description, points, icon);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'affichage de l'animation de gamification");
            System.Diagnostics.Debug.WriteLine($"Erreur animation gamification: {ex.Message}");
        }
    }

    private async Task ShowGamificationRewardInternalAsync(string title, string description, int points, string icon)
    {
        // S'assurer qu'aucune autre animation n'est en cours
        if (_currentGamificationAnimation?.IsVisible == true)
        {
            await _currentGamificationAnimation.HideAnimation();
        }

        // Créer une nouvelle instance d'animation
        _currentGamificationAnimation = new GamificationAnimationView();
        
        // Obtenir la page actuelle
        var currentPage = Application.Current?.MainPage;
        
        // Si on utilise Shell, obtenir la page courante via Shell
        if (currentPage is AppShell && Shell.Current?.CurrentPage != null)
        {
            currentPage = Shell.Current.CurrentPage;
        }
        
        if (currentPage == null)
        {
            _logger?.LogWarning("Impossible d'obtenir la page courante pour l'animation");
            return;
        }

        // Vérifier le type de page et ajouter l'animation
        await AddAnimationToPage(currentPage, _currentGamificationAnimation);

        // Configurer l'événement de fermeture
        _currentGamificationAnimation.AnimationCompleted += OnGamificationAnimationCompleted;

        // Démarrer l'animation
        await _currentGamificationAnimation.ShowAnimationAsync(title, description, points, icon);
    }

    private Task AddAnimationToPage(Page page, GamificationAnimationView animation) // méthode qui ajoute une animation au contenu de la page
    {
        try
        {
            switch (page)
            {
                case ContentPage contentPage when contentPage.Content is Grid grid:
                    // Ajouter l'animation par-dessus dans un Grid existant
                    Grid.SetRowSpan(animation, Math.Max(1, grid.RowDefinitions.Count));
                    Grid.SetColumnSpan(animation, Math.Max(1, grid.ColumnDefinitions.Count));
                    grid.Children.Add(animation);
                    break;

                case ContentPage contentPage when contentPage.Content is StackLayout stack:
                    // Créer un Grid wrapper pour StackLayout
                    var wrapperGrid = new Grid();
                    contentPage.Content = wrapperGrid;
                    wrapperGrid.Children.Add(stack);
                    wrapperGrid.Children.Add(animation);
                    break;

                case ContentPage contentPage:
                    // Créer un Grid wrapper pour tout autre contenu
                    var originalContent = contentPage.Content;
                    var newWrapperGrid = new Grid();
                    newWrapperGrid.Children.Add(originalContent);
                    newWrapperGrid.Children.Add(animation);
                    contentPage.Content = newWrapperGrid;
                    break;

                default:
                    _logger?.LogWarning($"Type de page non supporté pour l'animation: {page.GetType().Name}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors de l'ajout de l'animation à la page");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Animation spécialisée pour l'envoi de message
    /// </summary>
    public async Task ShowMessageSentRewardAsync()
    {
        await ShowGamificationRewardAsync(
            "Message envoyé ! 💬",
            "Vous gagnez des points pour chaque interaction !",
            5,
            "💬"
        );
    }

    /// <summary>
    /// Animation pour nouveau badge
    /// </summary>
    public async Task ShowNewBadgeAsync(string badgeName, int points)
    {
        await ShowGamificationRewardAsync(
            $"Nouveau Badge : {badgeName}",
            "Continuez comme ça pour débloquer plus de récompenses !",
            points,
            "🏆"
        );
    }

    /// <summary>
    /// Animation pour montée de niveau
    /// </summary>
    public async Task ShowLevelUpAsync(int newLevel, int points)
    {
        await ShowGamificationRewardAsync(
            $"Niveau {newLevel} atteint ! 🎉",
            "Votre engagement dans la communauté est remarquable !",
            points,
            "⭐"
        );
    }

    /// <summary>
    /// Animation pour première annonce
    /// </summary>
    public async Task ShowFirstAnnonceRewardAsync()
    {
        await ShowGamificationRewardAsync(
            "Première annonce créée ! 🎊",
            "Félicitations ! Vous venez de publier votre première annonce sur DonTroc !",
            50,
            "🎁"
        );
    }

    private void OnGamificationAnimationCompleted(object? sender, EventArgs e)
    {
        try
        {
            if (_currentGamificationAnimation != null)
            {
                _currentGamificationAnimation.AnimationCompleted -= OnGamificationAnimationCompleted;
                
                // Retirer l'animation de la page sur le thread principal
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        var parent = _currentGamificationAnimation.Parent;
                        if (parent is Layout layout)
                        {
                            layout.Children.Remove(_currentGamificationAnimation);
                        }
                        
                        _currentGamificationAnimation = null;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Erreur lors du nettoyage de l'animation");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Erreur lors du nettoyage de l'animation");
        }
    }

    /// <summary>
    /// Animation de fade in pour l'apparition d'éléments
    /// </summary>
    private static async Task FadeInAsync(VisualElement element, uint duration = 300)
    {
        element.Opacity = 0;
        element.IsVisible = true;
        await element.FadeTo(1, duration, Easing.CubicOut);
    }

    /// <summary>
    /// Animation de fade out pour la disparition d'éléments
    /// </summary>
    public static async Task FadeOutAsync(VisualElement element, uint duration = 300)
    {
        await element.FadeTo(0, duration, Easing.CubicIn);
        element.IsVisible = false;
    }

    /// <summary>
    /// Animation de slide in depuis la droite
    /// </summary>
    public static async Task SlideInFromRightAsync(VisualElement element, uint duration = 400)
    {
        element.TranslationX = element.Width;
        element.Opacity = 0;
        element.IsVisible = true;
        
        var slideTask = element.TranslateTo(0, 0, duration, Easing.CubicOut);
        var fadeTask = element.FadeTo(1, duration, Easing.CubicOut);
        
        await Task.WhenAll(slideTask, fadeTask);
    }

    /// <summary>
    /// Animation de slide out vers la droite
    /// </summary>
    public static async Task SlideOutToRightAsync(VisualElement element, uint duration = 400)
    {
        var slideTask = element.TranslateTo(element.Width, 0, duration, Easing.CubicIn);
        var fadeTask = element.FadeTo(0, duration, Easing.CubicIn);
        
        await Task.WhenAll(slideTask, fadeTask);
        element.IsVisible = false;
    }

    /// <summary>
    /// Animation de scale in (zoom in)
    /// </summary>
    public static async Task ScaleInAsync(VisualElement element, uint duration = 300)
    {
        element.Scale = 0.8;
        element.Opacity = 0;
        element.IsVisible = true;
        
        var scaleTask = element.ScaleTo(1, duration, Easing.SpringOut);
        var fadeTask = element.FadeTo(1, duration, Easing.CubicOut);
        
        await Task.WhenAll(scaleTask, fadeTask);
    }

    /// <summary>
    /// Animation de bounce pour les boutons
    /// </summary>
    public static async Task BounceAsync(VisualElement element, uint duration = 200)
    {
        await element.ScaleTo(0.95, duration / 2, Easing.CubicOut);
        await element.ScaleTo(1, duration / 2, Easing.SpringOut);
    }

    /// <summary>
    /// Animation de pulsation pour attirer l'attention
    /// </summary>
    public static async Task PulseAsync(VisualElement element, uint duration = 600)
    {
        var originalScale = element.Scale;
        await element.ScaleTo(originalScale * 1.1, duration / 2, Easing.CubicOut);
        await element.ScaleTo(originalScale, duration / 2, Easing.CubicIn);
    }

    /// <summary>
    /// Animation de shake pour les erreurs
    /// </summary>
    public static async Task ShakeAsync(VisualElement element, uint duration = 400)
    {
        var originalX = element.TranslationX;
        const int shakeDistance = 10;
        
        for (int i = 0; i < 4; i++)
        {
            await element.TranslateTo(originalX + shakeDistance, 0, duration / 8, Easing.Linear);
            await element.TranslateTo(originalX - shakeDistance, 0, duration / 8, Easing.Linear);
        }
        
        await element.TranslateTo(originalX, 0, duration / 8, Easing.Linear);
    }

    /// <summary>
    /// Animation de rotation pour les icônes de chargement
    /// </summary>
    public static void StartRotationAnimation(VisualElement element, uint duration = 1000)
    {
        var animation = new Animation(v => element.Rotation = v, 0, 360);
        animation.Commit(element, "Rotate", 16, duration, Easing.Linear, repeat: () => true);
    }

    /// <summary>
    /// Arrête l'animation de rotation
    /// </summary>
    public static void StopRotationAnimation(VisualElement element)
    {
        element.AbortAnimation("Rotate");
        element.Rotation = 0;
    }

    /// <summary>
    /// Animation pour l'apparition d'une liste d'éléments avec délai
    /// </summary>
    public static async Task StaggeredFadeInAsync(IEnumerable<VisualElement> elements, uint staggerDelay = 100, uint duration = 300)
    {
        var tasks = new List<Task>();
        int index = 0;
        
        foreach (var element in elements)
        {
            var delay = index * staggerDelay;
            var task = Task.Run(async () =>
            {
                await Task.Delay((int)delay);
                await FadeInAsync(element, duration);
            });
            tasks.Add(task);
            index++;
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Animation de swipe pour les gestes
    /// </summary>
    public static async Task SwipeAnimationAsync(VisualElement element, double distance, uint duration = 300)
    {
        await element.TranslateTo(distance, 0, duration / 2, Easing.CubicOut);
        await element.TranslateTo(0, 0, duration / 2, Easing.SpringOut);
    }
}

using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;

namespace DonTroc.Views
{
    public partial class GamificationAnimationView : ContentView
    {
        public GamificationAnimationView()
        {
            InitializeComponent();
            
            // Initialiser les valeurs par défaut pour les animations
            ResetAnimationValues();
        }

        // Propriétés pour configurer l'animation
        public static readonly BindableProperty TitleProperty = 
            BindableProperty.Create(nameof(Title), typeof(string), typeof(GamificationAnimationView), "Félicitations !");

        public static readonly BindableProperty DescriptionProperty = 
            BindableProperty.Create(nameof(Description), typeof(string), typeof(GamificationAnimationView), "Vous avez accompli quelque chose de génial !");

        public static readonly BindableProperty PointsProperty = 
            BindableProperty.Create(nameof(Points), typeof(int), typeof(GamificationAnimationView), 0);

        public static readonly BindableProperty IconProperty = 
            BindableProperty.Create(nameof(Icon), typeof(string), typeof(GamificationAnimationView), "🏆");

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public int Points
        {
            get => (int)GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        // Événement déclenché quand l'animation se ferme
        public event EventHandler? AnimationCompleted;

        // Méthode principale pour afficher l'animation
        public async Task ShowAnimationAsync(string title, string description, int points, string icon = "🏆")
        {
            try
            {
                Title = title;
                Description = description;
                Points = points;
                Icon = icon;

                // S'assurer qu'on est sur le thread principal
                if (!MainThread.IsMainThread)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ShowAnimationInternalAsync(title, description, points, icon);
                    });
                }
                else
                {
                    await ShowAnimationInternalAsync(title, description, points, icon);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur ShowAnimationAsync: {ex.Message}");
            }
        }

        private async Task ShowAnimationInternalAsync(string title, string description, int points, string icon)
        {
            // Réinitialiser les valeurs d'animation
            ResetAnimationValues();

            // Mettre à jour les textes
            AchievementTitle.Text = title;
            AchievementDescription.Text = description;
            PointsLabel.Text = points > 0 ? $"+{points} points" : "";
            AnimationIcon.Text = icon;

            // Rendre la vue visible
            IsVisible = true;

            // Démarrer l'animation
            await PlayAnimationSequence();
        }

        private void ResetAnimationValues()
        {
            try
            {
                // Réinitialiser les valeurs d'animation
                AnimationIcon.Scale = 0.1;
                AnimationIcon.Rotation = 0;
                AnimationFrame.Rotation = 0;
                
                AchievementTitle.Opacity = 0;
                AchievementTitle.Scale = 1.0;
                
                AchievementDescription.Opacity = 0;
                
                PointsLabel.Opacity = 0;
                PointsLabel.Scale = 1.0;
                
                CloseButton.Opacity = 0;
                
                ParticleGrid.IsVisible = false;
                
                // Réinitialiser les particules
                ResetParticles();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur ResetAnimationValues: {ex.Message}");
            }
        }

        private void ResetParticles()
        {
            try
            {
                var particles = new[] { Particle1, Particle2, Particle3, Particle4, Particle5, Particle6 };
                foreach (var particle in particles)
                {
                    particle.TranslationX = 0;
                    particle.TranslationY = 0;
                    particle.Scale = 1.0;
                    particle.Rotation = 0;
                    particle.Opacity = 1.0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur ResetParticles: {ex.Message}");
            }
        }

        private async Task PlayAnimationSequence()
        {
            try
            {
                // Phase 1: Apparition du cadre principal avec l'icône
                await Task.WhenAll(
                    AnimationIcon.ScaleTo(1.2, 600, Easing.BounceOut),
                    AnimationFrame.RotateTo(360, 800, Easing.CubicOut)
                );

                // Phase 2: Apparition du titre avec rebond
                AchievementTitle.Opacity = 1;
                await AchievementTitle.ScaleTo(1.1, 200, Easing.BounceOut);
                await AchievementTitle.ScaleTo(1.0, 200, Easing.BounceOut);

                // Phase 3: Apparition de la description
                AchievementDescription.Opacity = 1;
                await Task.Delay(200);

                // Phase 4: Animation des points avec effet "pop"
                PointsLabel.Opacity = 1;
                await PointsLabel.ScaleTo(1.2, 300, Easing.BounceOut);
                await PointsLabel.ScaleTo(1.0, 200, Easing.BounceOut);

                // Phase 5: Démarrer les confettis en parallèle
                var confettiTask = AnimateConfetti();

                // Phase 6: Apparition du bouton
                CloseButton.Opacity = 1;
                await Task.Delay(400);

                // Attendre la fin des confettis
                await confettiTask;

                // Auto-fermeture après 3 secondes
                await Task.Delay(3000);
                await HideAnimation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur PlayAnimationSequence: {ex.Message}");
                // En cas d'erreur, fermer l'animation
                await HideAnimation();
            }
        }

        private async Task AnimateConfetti()
        {
            try
            {
                ParticleGrid.IsVisible = true;

                // Animer chaque particule de confettis
                var particleAnimations = new List<Task>();

                // Particule 1 - En haut à gauche
                particleAnimations.Add(AnimateParticle(Particle1, -50, -100, 1500));
                
                // Particule 2 - En haut à droite  
                particleAnimations.Add(AnimateParticle(Particle2, 200, -80, 1200));
                
                // Particule 3 - À gauche
                particleAnimations.Add(AnimateParticle(Particle3, -80, 50, 1800));
                
                // Particule 4 - À droite
                particleAnimations.Add(AnimateParticle(Particle4, 150, 80, 1400));
                
                // Particule 5 - En bas à gauche
                particleAnimations.Add(AnimateParticle(Particle5, -30, 200, 1600));
                
                // Particule 6 - En bas à droite
                particleAnimations.Add(AnimateParticle(Particle6, 180, 180, 1300));

                // Attendre que toutes les particules terminent
                await Task.WhenAll(particleAnimations);

                ParticleGrid.IsVisible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur AnimateConfetti: {ex.Message}");
            }
        }

        private async Task AnimateParticle(Label particle, double endX, double endY, uint duration)
        {
            try
            {
                // Animation de mouvement et rotation simultanés
                var moveTask = particle.TranslateTo(endX, endY, duration, Easing.CubicOut);
                var rotateTask = particle.RotateTo(720, duration, Easing.Linear); // 2 tours complets
                var scaleTask = particle.ScaleTo(0.5, duration, Easing.CubicIn); // Rétrécissement
                
                await Task.WhenAll(moveTask, rotateTask, scaleTask);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur AnimateParticle: {ex.Message}");
            }
        }

        // Méthode pour fermer l'animation
        public async Task HideAnimation()
        {
            try
            {
                if (!IsVisible) return;

                // Animation de fermeture rapide
                await this.FadeTo(0, 300, Easing.CubicIn);
                IsVisible = false;
                
                // Déclencher l'événement de fermeture
                AnimationCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur HideAnimation: {ex.Message}");
                // S'assurer que l'animation est fermée même en cas d'erreur
                IsVisible = false;
                AnimationCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        // Gestionnaire de clic pour fermer l'animation
        private async void OnCloseButtonClicked(object sender, EventArgs e)
        {
            await HideAnimation();
        }

        // Gestionnaire de tap sur l'overlay pour fermer
        private async void OnOverlayTapped(object sender, EventArgs e)
        {
            await HideAnimation();
        }
    }
}

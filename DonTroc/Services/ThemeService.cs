using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using SixLabors.ImageSharp;
using Color = SixLabors.ImageSharp.Color;

namespace DonTroc.Services
{
    /// <summary>
    /// Service pour gérer le mode sombre/clair de l'application
    /// </summary>
    public class ThemeService
    {
        private const string THEME_KEY = "app_theme";
        private const string THEME_LIGHT = "light";
        private const string THEME_DARK = "dark";
        private const string THEME_SYSTEM = "system";

        /// <summary>
        /// Énumération des thèmes disponibles
        /// </summary>
        public enum AppTheme
        {
            Light,
            Dark,
            System
        }

        /// <summary>
        /// Événement déclenché lors du changement de thème
        /// </summary>
        public event EventHandler<AppTheme>? ThemeChanged;

        /// <summary>
        /// Obtient le thème actuellement sélectionné
        /// </summary>
        public AppTheme CurrentTheme { get; private set; }

        public ThemeService()
        {
            // Charger le thème sauvegardé ou utiliser le thème système par défaut
            LoadTheme();
        }

        /// <summary>
        /// Charge le thème depuis les préférences
        /// </summary>
        private void LoadTheme()
        {
            var savedTheme = Preferences.Get(THEME_KEY, THEME_SYSTEM);
            CurrentTheme = savedTheme switch
            {
                THEME_LIGHT => AppTheme.Light,
                THEME_DARK => AppTheme.Dark,
                _ => AppTheme.System
            };
            
            ApplyTheme(CurrentTheme);
        }

        /// <summary>
        /// Change le thème de l'application
        /// </summary>
        /// <param name="theme">Le nouveau thème à appliquer</param>
        public void SetTheme(AppTheme theme)
        {
            if (CurrentTheme == theme) return;

            CurrentTheme = theme;
            ApplyTheme(theme);
            SaveTheme(theme);
            ThemeChanged?.Invoke(this, theme);
        }

        /// <summary>
        /// Applique le thème sélectionné
        /// </summary>
        /// <param name="theme">Le thème à appliquer</param>
        private void ApplyTheme(AppTheme theme)
        {
            if (Application.Current == null) return;

            Application.Current.UserAppTheme = theme switch
            {
                AppTheme.Light => Microsoft.Maui.ApplicationModel.AppTheme.Light,
                AppTheme.Dark => Microsoft.Maui.ApplicationModel.AppTheme.Dark,
                AppTheme.System => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified,
                _ => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified
            };
        }

        /// <summary>
        /// Sauvegarde le thème dans les préférences
        /// </summary>
        /// <param name="theme">Le thème à sauvegarder</param>
        private void SaveTheme(AppTheme theme)
        {
            var themeString = theme switch
            {
                AppTheme.Light => THEME_LIGHT,
                AppTheme.Dark => THEME_DARK,
                AppTheme.System => THEME_SYSTEM,
                _ => THEME_SYSTEM
            };
            
            Preferences.Set(THEME_KEY, themeString);
        }

        /// <summary>
        /// Obtient la couleur actuelle selon le thème
        /// </summary>
        /// <param name="lightColor">Couleur en mode clair</param>
        /// <param name="darkColor">Couleur en mode sombre</param>
        /// <returns>La couleur appropriée selon le thème actuel</returns>
        public Color GetThemedColor(Color lightColor, Color darkColor)
        {
            var currentAppTheme = Application.Current?.RequestedTheme ?? Microsoft.Maui.ApplicationModel.AppTheme.Light;
            
            return (CurrentTheme == AppTheme.System ? currentAppTheme : ConvertToMauiAppTheme(CurrentTheme)) switch
            {
                Microsoft.Maui.ApplicationModel.AppTheme.Dark => darkColor,
                _ => lightColor
            };
        }

        /// <summary>
        /// Vérifie si le mode sombre est actuellement actif
        /// </summary>
        public bool IsDarkMode
        {
            get
            {
                if (CurrentTheme == AppTheme.System)
                {
                    return Application.Current?.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;
                }
                return CurrentTheme == AppTheme.Dark;
            }
        }

        /// <summary>
        /// Bascule entre le mode clair et sombre (ignore le mode système)
        /// </summary>
        public void ToggleTheme()
        {
            var newTheme = IsDarkMode ? AppTheme.Light : AppTheme.Dark;
            SetTheme(newTheme);
        }

        /// <summary>
        /// Obtient l'icône appropriée pour le thème actuel
        /// </summary>
        public string GetThemeIcon()
        {
            return CurrentTheme switch
            {
                AppTheme.Light => "☀️",
                AppTheme.Dark => "🌙",
                AppTheme.System => "🔄",
                _ => "🔄"
            };
        }

        /// <summary>
        /// Obtient l'emoji décoratif (grande taille) du thème actuel
        /// </summary>
        public string GetThemeEmoji()
        {
            if (CurrentTheme == AppTheme.System)
            {
                // En mode système, afficher l'icône qui correspond au thème réellement actif
                return IsDarkMode ? "🌜" : "🌤️";
            }
            return CurrentTheme switch
            {
                AppTheme.Light => "🌞",
                AppTheme.Dark => "🌜",
                _ => "🌤️"
            };
        }

        /// <summary>
        /// Obtient le nom d'affichage du thème actuel
        /// </summary>
        public string GetThemeDisplayName()
        {
            return CurrentTheme switch
            {
                AppTheme.Light => "Mode clair",
                AppTheme.Dark => "Mode sombre",
                AppTheme.System => "Automatique",
                _ => "Automatique"
            };
        }

        /// <summary>
        /// Convertit AppTheme local vers Microsoft.Maui.ApplicationModel.AppTheme
        /// </summary>
        private Microsoft.Maui.ApplicationModel.AppTheme ConvertToMauiAppTheme(AppTheme theme)
        {
            return theme switch
            {
                AppTheme.Light => Microsoft.Maui.ApplicationModel.AppTheme.Light,
                AppTheme.Dark => Microsoft.Maui.ApplicationModel.AppTheme.Dark,
                _ => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified
            };
        }
    }
}

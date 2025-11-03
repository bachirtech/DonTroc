using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace DonTroc.Views
{
    /// <summary>
    /// Contrôle personnalisé pour afficher un badge de comptage
    /// </summary>
    public class BadgeView : ContentView
    {
        public static readonly BindableProperty CountProperty = 
            BindableProperty.Create(nameof(Count), typeof(int), typeof(BadgeView), 0, propertyChanged: OnCountChanged);

        public static readonly BindableProperty BadgeColorProperty = 
            BindableProperty.Create(nameof(BadgeColor), typeof(Microsoft.Maui.Graphics.Color), typeof(BadgeView), Colors.Red);

        public static readonly BindableProperty TextColorProperty = 
            BindableProperty.Create(nameof(TextColor), typeof(Microsoft.Maui.Graphics.Color), typeof(BadgeView), Colors.White);

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        public Microsoft.Maui.Graphics.Color BadgeColor // Propriété pour la couleur du badge
        {
            get => (Microsoft.Maui.Graphics.Color)GetValue(BadgeColorProperty);
            set => SetValue(BadgeColorProperty, value);
        }

        public Microsoft.Maui.Graphics.Color TextColor // Propriété pour la couleur du texte
        {
            get => (Microsoft.Maui.Graphics.Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        private Label _countLabel = new();
        private Border _badgeBorder = new();

        public BadgeView()
        {
            CreateBadge();
        }

        private void CreateBadge()
        {
            _countLabel = new Label
            {
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                TextColor = TextColor
            };

            _badgeBorder = new Border
            {
                StrokeThickness = 0,
                BackgroundColor = BadgeColor,
                Padding = new Thickness(6, 2),
                Content = _countLabel
            };

            // Créer une forme circulaire/arrondie
            _badgeBorder.StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(10)
            };

            Content = _badgeBorder;
            UpdateVisibility();
        }

        private static void OnCountChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is BadgeView badge)
            {
                badge.UpdateBadge();
            }
        }

        private void UpdateBadge()
        {
            if (_countLabel != null)
            {
                // Afficher "99+" si le nombre dépasse 99
                _countLabel.Text = Count > 99 ? "99+" : Count.ToString();
                _countLabel.TextColor = TextColor;
            }

            if (_badgeBorder != null)
            {
                _badgeBorder.BackgroundColor = BadgeColor;
            }

            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            // Masquer le badge si le compteur est 0
            IsVisible = Count > 0;
        }

        protected override void OnPropertyChanged(string? propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(BadgeColor) || propertyName == nameof(TextColor))
            {
                UpdateBadge();
            }
        }
    }
}

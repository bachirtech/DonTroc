using System;
using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace DonTroc.Views
{
    /// <summary>
    /// Vue de sélection d'émojis pour le chat
    /// Affiche une grille d'émojis populaires organisés par catégories
    /// </summary>
    public class EmojiPickerView : ContentView
    {
        private readonly ScrollView _scrollView;
        private readonly Grid _mainGrid;
        private int _selectedCategoryIndex = 0;
        private HorizontalStackLayout? _categoryLayout;

        // Commande pour notifier quand un émoji est sélectionné
        public static readonly BindableProperty EmojiSelectedCommandProperty =
            BindableProperty.Create(
                nameof(EmojiSelectedCommand),
                typeof(ICommand),
                typeof(EmojiPickerView),
                null);

        public ICommand EmojiSelectedCommand
        {
            get => (ICommand)GetValue(EmojiSelectedCommandProperty);
            set => SetValue(EmojiSelectedCommandProperty, value);
        }

        // Catégories d'émojis avec leurs émojis associés
        private static readonly Dictionary<string, (string Icon, string[] Emojis)> EmojiCategories = new()
        {
            ["Smileys"] = ("😀", new[]
            {
                "😀", "😃", "😄", "😁", "😆", "😅", "🤣", "😂",
                "🙂", "🙃", "😉", "😊", "😇", "🥰", "😍", "🤩",
                "😘", "😗", "☺️", "😚", "😙", "🥲", "😋", "😛",
                "😜", "🤪", "😝", "🤑", "🤗", "🤭", "🤫", "🤔",
                "😐", "😑", "😶", "😏", "😒", "🙄", "😬", "😌",
                "😔", "😪", "🤤", "😴", "😷", "🤒", "🤕", "🤢",
                "🤮", "🤧", "🥵", "🥶", "🥴", "😵", "🤯", "🤠",
                "🥳", "🥸", "😎", "🤓", "😳", "🥺", "😢", "😭"
            }),
            
            ["Gestes"] = ("👋", new[]
            {
                "👋", "🤚", "🖐️", "✋", "🖖", "👌", "🤌", "🤏",
                "✌️", "🤞", "🤟", "🤘", "🤙", "👈", "👉", "👆",
                "👇", "☝️", "👍", "👎", "✊", "👊", "🤛", "🤜",
                "👏", "🙌", "👐", "🤲", "🤝", "🙏", "✍️", "💪"
            }),
            
            ["Cœurs"] = ("❤️", new[]
            {
                "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍",
                "🤎", "💔", "❣️", "💕", "💞", "💓", "💗", "💖",
                "💘", "💝", "💟", "❤️‍🔥", "❤️‍🩹", "💌", "😻", "😍"
            }),
            
            ["Animaux"] = ("🐶", new[]
            {
                "🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼",
                "🐨", "🐯", "🦁", "🐮", "🐷", "🐸", "🐵", "🙈",
                "🙉", "🙊", "🐔", "🐧", "🐦", "🐤", "🦆", "🦅",
                "🦉", "🦇", "🐺", "🐗", "🐴", "🦄", "🐝", "🦋"
            }),
            
            ["Objets"] = ("💡", new[]
            {
                "⌚", "📱", "💻", "⌨️", "🖥️", "📷", "📹", "🎥",
                "📞", "☎️", "📺", "📻", "🎙️", "⏰", "🔦", "💡",
                "🔌", "🔋", "💰", "💵", "💳", "🎁", "🎈", "🎉"
            }),
            
            ["Symboles"] = ("✨", new[]
            {
                "❗", "❓", "💯", "🔥", "✨", "⭐", "🌟", "💫",
                "💥", "💢", "💦", "💨", "💣", "💬", "💭", "💤",
                "✅", "❌", "⭕", "🚫", "🔴", "🟠", "🟡", "🟢",
                "🔵", "🟣", "⚫", "⚪", "🟤", "💎", "🔔", "🎵"
            })
        };

        public EmojiPickerView()
        {
            BackgroundColor = Color.FromArgb("#F5F5F5");
            MinimumHeightRequest = 250;

            _mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(50, GridUnitType.Absolute) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                },
                RowSpacing = 0,
                BackgroundColor = Color.FromArgb("#F5F5F5")
            };

            // Créer la barre de catégories
            var categoryBar = CreateCategoryBar();
            _mainGrid.SetRow(categoryBar, 0);
            _mainGrid.Children.Add(categoryBar);

            // Créer la grille d'émojis initiale
            _scrollView = new ScrollView
            {
                Orientation = ScrollOrientation.Vertical,
                Content = CreateEmojiGrid("Smileys"),
                BackgroundColor = Color.FromArgb("#F5F5F5")
            };
            _mainGrid.SetRow(_scrollView, 1);
            _mainGrid.Children.Add(_scrollView);

            Content = _mainGrid;
        }

        /// <summary>
        /// Crée la barre de sélection des catégories
        /// </summary>
        private View CreateCategoryBar()
        {
            var border = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                Padding = new Thickness(0),
                HeightRequest = 50
            };

            var categoryScroll = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
                VerticalOptions = LayoutOptions.Center
            };

            _categoryLayout = new HorizontalStackLayout
            {
                Spacing = 8,
                Padding = new Thickness(10, 8)
            };

            int index = 0;
            foreach (var category in EmojiCategories)
            {
                var categoryKey = category.Key;
                var categoryIndex = index;
                
                var categoryButton = new Button
                {
                    Text = category.Value.Icon,
                    FontSize = 22,
                    WidthRequest = 44,
                    HeightRequest = 36,
                    Padding = new Thickness(0),
                    CornerRadius = 10,
                    BackgroundColor = index == _selectedCategoryIndex 
                        ? Color.FromArgb("#D4A574") 
                        : Color.FromArgb("#EEEEEE"),
                    BorderWidth = 0
                };

                categoryButton.Clicked += (s, e) =>
                {
                    _selectedCategoryIndex = categoryIndex;
                    UpdateCategorySelection();
                    _scrollView.Content = CreateEmojiGrid(categoryKey);
                };

                _categoryLayout.Children.Add(categoryButton);
                index++;
            }

            categoryScroll.Content = _categoryLayout;
            border.Content = categoryScroll;
            return border;
        }

        /// <summary>
        /// Met à jour la sélection visuelle des catégories
        /// </summary>
        private void UpdateCategorySelection()
        {
            if (_categoryLayout == null) return;
            
            int index = 0;
            foreach (var child in _categoryLayout.Children)
            {
                if (child is Button btn)
                {
                    btn.BackgroundColor = index == _selectedCategoryIndex 
                        ? Color.FromArgb("#D4A574") 
                        : Color.FromArgb("#EEEEEE");
                }
                index++;
            }
        }

        /// <summary>
        /// Crée la grille d'émojis pour une catégorie donnée
        /// </summary>
        private View CreateEmojiGrid(string categoryName)
        {
            if (!EmojiCategories.TryGetValue(categoryName, out var category))
            {
                return new Label { Text = "Catégorie non trouvée", TextColor = Colors.Red };
            }

            var flexLayout = new FlexLayout
            {
                Direction = FlexDirection.Row,
                Wrap = FlexWrap.Wrap,
                JustifyContent = FlexJustify.Start,
                AlignContent = FlexAlignContent.Start,
                Padding = new Thickness(8),
                BackgroundColor = Color.FromArgb("#F5F5F5")
            };

            foreach (var emoji in category.Emojis)
            {
                var emojiLabel = new Label
                {
                    Text = emoji,
                    FontSize = 28,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center
                };

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) =>
                {
                    EmojiSelectedCommand?.Execute(emoji);
                };
                
                var emojiFrame = new Frame
                {
                    Content = emojiLabel,
                    WidthRequest = 48,
                    HeightRequest = 48,
                    Padding = new Thickness(0),
                    CornerRadius = 8,
                    BackgroundColor = Colors.Transparent,
                    BorderColor = Colors.Transparent,
                    HasShadow = false
                };
                emojiFrame.GestureRecognizers.Add(tapGesture);

                flexLayout.Children.Add(emojiFrame);
            }

            return flexLayout;
        }
    }
}

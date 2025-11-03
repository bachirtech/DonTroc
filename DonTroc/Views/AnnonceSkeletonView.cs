using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DonTroc.Views;

/// <summary>
/// Skeleton loading spécialisé pour les cartes d'annonces
/// </summary>
public class AnnonceSkeletonView : ContentView
{
    public AnnonceSkeletonView()
    {
        Content = new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 12,
            HasShadow = true,
            Margin = new Thickness(10, 5),
            Content = new StackLayout
            {
                Padding = new Thickness(15),
                Children =
                {
                    // Image skeleton
                    new Frame
                    {
                        CornerRadius = 8,
                        HeightRequest = 150,
                        HasShadow = false,
                        Padding = 0,
                        Content = new SkeletonView { IsActive = true }
                    },
                    
                    // Titre skeleton
                    new Frame
                    {
                        CornerRadius = 4,
                        HeightRequest = 20,
                        HasShadow = false,
                        Padding = 0,
                        Margin = new Thickness(0, 10, 50, 0),
                        Content = new SkeletonView { IsActive = true }
                    },
                    
                    // Description skeleton
                    new Frame
                    {
                        CornerRadius = 4,
                        HeightRequest = 16,
                        HasShadow = false,
                        Padding = 0,
                        Margin = new Thickness(0, 5, 20, 0),
                        Content = new SkeletonView { IsActive = true }
                    },
                    
                    // Ligne de détails skeleton
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        Margin = new Thickness(0, 10, 0, 0),
                        Children =
                        {
                            new Frame
                            {
                                CornerRadius = 12,
                                HeightRequest = 24,
                                WidthRequest = 60,
                                HasShadow = false,
                                Padding = 0,
                                Content = new SkeletonView { IsActive = true }
                            },
                            new Frame
                            {
                                CornerRadius = 12,
                                HeightRequest = 24,
                                WidthRequest = 80,
                                HasShadow = false,
                                Padding = 0,
                                Margin = new Thickness(10, 0, 0, 0),
                                Content = new SkeletonView { IsActive = true }
                            }
                        }
                    }
                }
            }
        };
    }
}

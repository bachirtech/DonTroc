using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DonTroc.Views;

/// <summary>
/// Vue skeleton pour affichage de chargement avec animation
/// </summary>
public class SkeletonView : ContentView
{
    private readonly BoxView _shimmerBox;
    private readonly Animation _shimmerAnimation;
    private bool _isAnimating;

    public static readonly BindableProperty IsActiveProperty =
        BindableProperty.Create(nameof(IsActive), typeof(bool), typeof(SkeletonView), false,
            propertyChanged: OnIsActiveChanged);

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(SkeletonView), 4.0);

    public static readonly BindableProperty ShimmerColorProperty =
        BindableProperty.Create(nameof(ShimmerColor), typeof(Color), typeof(SkeletonView), Colors.LightGray);

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public Color ShimmerColor
    {
        get => (Color)GetValue(ShimmerColorProperty);
        set => SetValue(ShimmerColorProperty, value);
    }

    public SkeletonView()
    {
        // Création du BoxView avec gradient pour l'effet shimmer
        _shimmerBox = new BoxView
        {
            Background = CreateShimmerGradient(),
            Opacity = 0.6
        };

        Content = _shimmerBox;

        // Animation de shimmer
        _shimmerAnimation = new Animation(v =>
        {
            _shimmerBox.Opacity = v;
        }, 0.3, 1.0);

        // Mise à jour des propriétés bindables
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CornerRadius))
        {
            UpdateCornerRadius();
        }
        else if (e.PropertyName == nameof(ShimmerColor))
        {
            UpdateShimmerColor();
        }
    }

    private static void OnIsActiveChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SkeletonView skeleton)
        {
            if ((bool)newValue)
                skeleton.StartAnimation();
            else
                skeleton.StopAnimation();
        }
    }

    private LinearGradientBrush CreateShimmerGradient()
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = ShimmerColor, Offset = 0.0f },
                new GradientStop { Color = ShimmerColor.WithAlpha(0.5f), Offset = 0.5f },
                new GradientStop { Color = ShimmerColor, Offset = 1.0f }
            }
        };
    }

    private void UpdateCornerRadius()
    {
        // Note: Dans .NET MAUI, le corner radius nécessite un Frame ou Border
        if (Parent is Frame frame)
        {
            frame.CornerRadius = (float)CornerRadius;
        }
    }

    private void UpdateShimmerColor()
    {
        _shimmerBox.Background = CreateShimmerGradient();
    }

    private void StartAnimation()
    {
        if (_isAnimating) return;

        _isAnimating = true;
        this.Animate("shimmer", _shimmerAnimation, length: 1500, repeat: () => IsActive);
    }

    private void StopAnimation()
    {
        _isAnimating = false;
        this.AbortAnimation("shimmer");
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (IsActive)
            StartAnimation();
    }
}

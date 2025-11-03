using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace DonTroc.Behaviors;

public class EmailValidationBehavior : Behavior<Entry>
{
    private static readonly Regex EmailRegex = new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");

    public static readonly BindableProperty ValidStyleProperty =
        BindableProperty.Create(nameof(ValidStyle), typeof(Style), typeof(EmailValidationBehavior));

    public static readonly BindableProperty InvalidStyleProperty =
        BindableProperty.Create(nameof(InvalidStyle), typeof(Style), typeof(EmailValidationBehavior));

    public Style ValidStyle
    {
        get => (Style)GetValue(ValidStyleProperty);
        set => SetValue(ValidStyleProperty, value);
    }

    public Style InvalidStyle
    {
        get => (Style)GetValue(InvalidStyleProperty);
        set => SetValue(InvalidStyleProperty, value);
    }

    protected override void OnAttachedTo(Entry entry)
    {
        entry.TextChanged += OnEntryTextChanged;
        base.OnAttachedTo(entry);
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnEntryTextChanged;
        base.OnDetachingFrom(entry);
    }

    private void OnEntryTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            bool isValid = !string.IsNullOrEmpty(e.NewTextValue) && EmailRegex.IsMatch(e.NewTextValue);
            entry.Style = isValid ? ValidStyle : InvalidStyle;
        }
    }
}

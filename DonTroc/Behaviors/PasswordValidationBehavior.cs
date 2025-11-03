using System.Collections.Generic;
using Microsoft.Maui.Controls;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Messaging;

namespace DonTroc.Behaviors;

public class PasswordValidationBehavior : Behavior<Entry>
{
    public static readonly BindableProperty MinimumLengthProperty =
        BindableProperty.Create(nameof(MinimumLength), typeof(int), typeof(PasswordValidationBehavior), 8);

    public static readonly BindableProperty RequireUppercaseProperty =
        BindableProperty.Create(nameof(RequireUppercase), typeof(bool), typeof(PasswordValidationBehavior), true);

    public static readonly BindableProperty RequireSpecialCharacterProperty =
        BindableProperty.Create(nameof(RequireSpecialCharacter), typeof(bool), typeof(PasswordValidationBehavior), true);

    public static readonly BindableProperty RequireNumberProperty =
        BindableProperty.Create(nameof(RequireNumber), typeof(bool), typeof(PasswordValidationBehavior), false);

    public static readonly BindableProperty ValidStyleProperty =
        BindableProperty.Create(nameof(ValidStyle), typeof(Style), typeof(PasswordValidationBehavior));

    public static readonly BindableProperty InvalidStyleProperty =
        BindableProperty.Create(nameof(InvalidStyle), typeof(Style), typeof(PasswordValidationBehavior));

    public static readonly BindableProperty ValidationMessageProperty =
        BindableProperty.Create(nameof(ValidationMessage), typeof(string), typeof(PasswordValidationBehavior), "");

    public int MinimumLength
    {
        get => (int)GetValue(MinimumLengthProperty);
        set => SetValue(MinimumLengthProperty, value);
    }

    public bool RequireUppercase
    {
        get => (bool)GetValue(RequireUppercaseProperty);
        set => SetValue(RequireUppercaseProperty, value);
    }

    public bool RequireSpecialCharacter
    {
        get => (bool)GetValue(RequireSpecialCharacterProperty);
        set => SetValue(RequireSpecialCharacterProperty, value);
    }

    public bool RequireNumber
    {
        get => (bool)GetValue(RequireNumberProperty);
        set => SetValue(RequireNumberProperty, value);
    }

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

    public string ValidationMessage
    {
        get => (string)GetValue(ValidationMessageProperty);
        set => SetValue(ValidationMessageProperty, value);
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
            var validation = ValidatePassword(e.NewTextValue);
            entry.Style = validation.IsValid ? ValidStyle : InvalidStyle;
            
            // Mettre à jour le message de validation
            SetValue(ValidationMessageProperty, validation.Message);
            
            // Utiliser WeakReferenceMessenger au lieu de MessagingCenter obsolète
            WeakReferenceMessenger.Default.Send(new PasswordValidationChangedMessage(validation));
        }
    }

    private PasswordValidationResult ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return new PasswordValidationResult
            {
                IsValid = false,
                Message = "Le mot de passe est requis"
            };
        }

        var errors = new List<string>();

        // Vérifier la longueur minimale
        if (password.Length < MinimumLength)
        {
            errors.Add($"Au moins {MinimumLength} caractères");
        }

        // Vérifier la présence d'une majuscule
        if (RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
        {
            errors.Add("Au moins une lettre majuscule");
        }

        // Vérifier la présence d'un caractère spécial
        if (RequireSpecialCharacter && !Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
        {
            errors.Add("Au moins un caractère spécial (!@#$%^&*...)");
        }

        // Vérifier la présence d'un chiffre (optionnel)
        if (RequireNumber && !Regex.IsMatch(password, @"\d"))
        {
            errors.Add("Au moins un chiffre");
        }

        return new PasswordValidationResult
        {
            IsValid = errors.Count == 0,
            Message = errors.Count == 0 ? "Mot de passe valide ✓" : $"Requis: {string.Join(", ", errors)}"
        };
    }
}

public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
}

// Message pour la communication MVVM moderne
public record PasswordValidationChangedMessage(PasswordValidationResult ValidationResult);

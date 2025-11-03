using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DonTroc.Services;

public class PasswordValidationService
{
    private const int MinimumLength = 8;
    private const string UppercasePattern = @"[A-Z]";
    private const string SpecialCharacterPattern = @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]";
    private const string NumberPattern = @"\d";
    private const string LowercasePattern = @"[a-z]";

    /// <summary>
    /// Valide un mot de passe selon les règles de sécurité renforcées
    /// </summary>
    /// <param name="password">Le mot de passe à valider</param>
    /// <returns>Résultat de la validation avec les détails</returns>
    public PasswordValidationResult ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return new PasswordValidationResult
            {
                IsValid = false,
                Message = "Le mot de passe est requis",
                Errors = new List<string> { "Le mot de passe ne peut pas être vide" }
            };
        }

        var errors = new List<string>();
        var suggestions = new List<string>();

        // Vérifier la longueur minimale
        if (password.Length < MinimumLength)
        {
            errors.Add($"Au moins {MinimumLength} caractères requis");
            suggestions.Add($"Ajoutez {MinimumLength - password.Length} caractère(s) supplémentaire(s)");
        }

        // Vérifier la présence d'une majuscule (OBLIGATOIRE)
        if (!Regex.IsMatch(password, UppercasePattern))
        {
            errors.Add("Au moins une lettre majuscule requise");
            suggestions.Add("Ajoutez une lettre majuscule (A-Z)");
        }

        // Vérifier la présence d'un caractère spécial (OBLIGATOIRE)
        if (!Regex.IsMatch(password, SpecialCharacterPattern))
        {
            errors.Add("Au moins un caractère spécial requis");
            suggestions.Add("Ajoutez un caractère spécial (!@#$%^&*...)");
        }

        // Vérifications recommandées (non obligatoires mais suggérées)
        if (!Regex.IsMatch(password, LowercasePattern))
        {
            suggestions.Add("Recommandé : ajoutez une lettre minuscule");
        }

        if (!Regex.IsMatch(password, NumberPattern))
        {
            suggestions.Add("Recommandé : ajoutez un chiffre pour plus de sécurité");
        }

        // Vérifier les patterns courants faibles
        if (ContainsCommonWeakPatterns(password))
        {
            suggestions.Add("Évitez les séquences communes (123, abc, qwerty...)");
        }

        var isValid = errors.Count == 0;
        var message = isValid ? "Mot de passe sécurisé ✓" : $"Requis: {string.Join(", ", errors)}";

        return new PasswordValidationResult
        {
            IsValid = isValid,
            Message = message,
            Errors = errors,
            Suggestions = suggestions,
            StrengthLevel = CalculateStrengthLevel(password, isValid)
        };
    }

    /// <summary>
    /// Valide que le mot de passe respecte les critères minimum pour l'inscription
    /// </summary>
    /// <param name="password">Le mot de passe à valider</param>
    /// <returns>True si le mot de passe est valide pour l'inscription</returns>
    public bool IsValidForRegistration(string password)
    {
        var result = ValidatePassword(password);
        return result.IsValid;
    }

    /// <summary>
    /// Obtient les règles de mot de passe formatées pour l'affichage
    /// </summary>
    /// <returns>Liste des règles formatées</returns>
    public List<string> GetPasswordRules()
    {
        return new List<string>
        {
            $"• Au moins {MinimumLength} caractères",
            "• Au moins une lettre majuscule (A-Z)",
            "• Au moins un caractère spécial (!@#$%^&*...)",
            "• Recommandé : une lettre minuscule et un chiffre"
        };
    }

    /// <summary>
    /// Calcule le niveau de force du mot de passe
    /// </summary>
    private PasswordStrength CalculateStrengthLevel(string password, bool isValid)
    {
        if (!isValid || string.IsNullOrEmpty(password))
        {
            return PasswordStrength.Weak;
        }

        int score = 0;

        // Points pour la longueur
        if (password.Length >= 8) score += 1;
        if (password.Length >= 12) score += 1;
        if (password.Length >= 16) score += 1;

        // Points pour la diversité des caractères
        if (Regex.IsMatch(password, UppercasePattern)) score += 1;
        if (Regex.IsMatch(password, LowercasePattern)) score += 1;
        if (Regex.IsMatch(password, NumberPattern)) score += 1;
        if (Regex.IsMatch(password, SpecialCharacterPattern)) score += 1;

        // Points bonus pour la complexité
        if (password.Length > 12 && score >= 4) score += 1;

        return score switch
        {
            <= 3 => PasswordStrength.Weak,
            4 or 5 => PasswordStrength.Medium,
            6 or 7 => PasswordStrength.Strong,
            _ => PasswordStrength.VeryStrong
        };
    }

    /// <summary>
    /// Vérifie si le mot de passe contient des patterns faibles courants
    /// </summary>
    private bool ContainsCommonWeakPatterns(string password)
    {
        var weakPatterns = new[]
        {
            "123", "abc", "qwe", "asd", "zxc", "qwerty", "azerty",
            "password", "motdepasse", "admin", "user", "1234"
        };

        return weakPatterns.Any(pattern => 
            password.ToLower().Contains(pattern.ToLower()));
    }
}

public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = "";
    public List<string> Errors { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public PasswordStrength StrengthLevel { get; set; }
    
    /// <summary>
    /// Obtient une description textuelle du niveau de sécurité
    /// </summary>
    public string StrengthDescription => StrengthLevel switch
    {
        PasswordStrength.Weak => "Faible",
        PasswordStrength.Medium => "Moyen", 
        PasswordStrength.Strong => "Fort",
        PasswordStrength.VeryStrong => "Très fort",
        _ => "Inconnu"
    };
}

public enum PasswordStrength
{
    Weak,
    Medium,
    Strong,
    VeryStrong
}

using System.Text.RegularExpressions;

namespace ConnectHub.Authentication.Helpers;

/// <summary>
/// Provides regex-based validation logic for authentication inputs.
/// All methods return a list of human-readable error messages (empty = valid).
/// </summary>
public static class ValidationHelper
{
    // ── Regex Patterns ──────────────────────────────────────────────────────────

    /// <summary>
    /// 3–20 characters: letters, digits, and underscores only. No spaces or special chars.
    /// </summary>
    private static readonly Regex UsernameRegex =
        new(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);

    /// <summary>
    /// 2–50 characters: letters, digits, spaces, hyphens, and underscores.
    /// </summary>
    private static readonly Regex DisplayNameRegex =
        new(@"^[a-zA-Z0-9 _\-]{2,50}$", RegexOptions.Compiled);

    /// <summary>
    /// Standard email format: local@domain.tld (no whitespace).
    /// </summary>
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Password: 8–100 chars, must contain at least:
    ///   • one uppercase letter
    ///   • one lowercase letter
    ///   • one digit
    ///   • one special character (@$!%*?&_-#)
    /// </summary>
    private static readonly Regex PasswordRegex =
        new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&_\-#\.\,])[A-Za-z\d@$!%*?&_\-#\.\,]{8,100}$",
            RegexOptions.Compiled);

    // ── Public Validation Methods ────────────────────────────────────────────────

    /// <summary>
    /// Validates all fields required for registration.
    /// Returns a list of error messages; empty list means all fields are valid.
    /// </summary>
    public static List<string> ValidateRegistration(
        string userName, string displayName, string email, string password)
    {
        var errors = new List<string>();

        // Username
        if (string.IsNullOrWhiteSpace(userName))
            errors.Add("Username is required.");
        else if (!UsernameRegex.IsMatch(userName))
            errors.Add("Username must be 3–20 characters and contain only letters, digits, or underscores.");

        // Display name (optional — skip regex if empty, it gets defaulted to UserName)
        if (!string.IsNullOrWhiteSpace(displayName) && !DisplayNameRegex.IsMatch(displayName))
            errors.Add("Display name must be 2–50 characters and contain only letters, digits, spaces, hyphens, or underscores.");

        // Email
        if (string.IsNullOrWhiteSpace(email))
            errors.Add("Email address is required.");
        else if (!EmailRegex.IsMatch(email))
            errors.Add("Email address format is invalid.");

        // Password
        if (string.IsNullOrWhiteSpace(password))
            errors.Add("Password is required.");
        else if (!PasswordRegex.IsMatch(password))
            errors.Add("Password must be 8–100 characters and include at least one uppercase letter, one lowercase letter, one digit, and one special character (@$!%*?&_-#).");

        return errors;
    }

    /// <summary>
    /// Validates login inputs (email or username + password).
    /// Returns a list of error messages; empty list means inputs are valid.
    /// </summary>
    public static List<string> ValidateLogin(string emailOrUsername, string password)
    {
        var errors = new List<string>();

        // Identifier — accept either a valid email or a valid username
        if (string.IsNullOrWhiteSpace(emailOrUsername))
        {
            errors.Add("Email or username is required.");
        }
        else if (!EmailRegex.IsMatch(emailOrUsername) && !UsernameRegex.IsMatch(emailOrUsername))
        {
            errors.Add("Please enter a valid email address or a username (3–20 characters, letters/digits/underscores).");
        }

        // Password — only basic presence + length check on login (no complexity to avoid leaking policy hints)
        if (string.IsNullOrWhiteSpace(password))
            errors.Add("Password is required.");
        else if (password.Length < 8)
            errors.Add("Password must be at least 8 characters.");
        else if (password.Length > 100)
            errors.Add("Password must not exceed 100 characters.");

        return errors;
    }
}

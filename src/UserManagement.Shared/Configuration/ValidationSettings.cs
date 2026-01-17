namespace UserManagement.Shared.Configuration;

/// <summary>
/// Configuration for business validation rules.
/// </summary>
public class ValidationSettings
{
    /// <summary>
    /// List of blocked email domains (e.g., temporary email providers).
    /// </summary>
    public List<string> BlockedEmailDomains { get; set; } = new List<string>();

    /// <summary>
    /// Bangladesh phone number operator prefixes (e.g., 017, 018).
    /// </summary>
    public List<string> BangladeshOperatorPrefixes { get; set; } = new List<string>();

    /// <summary>
    /// Minimum password length.
    /// </summary>
    public int MinPasswordLength { get; set; } = 12;

    /// <summary>
    /// Last N passwords to remember for prevention of reuse.
    /// </summary>
    public int PasswordHistoryLimit { get; set; } = 5;

    /// <summary>
    /// Restricted keywords for names and usernames.
    /// </summary>
    public List<string> RestrictedKeywords { get; set; } = new List<string> { "admin", "support", "system", "root" };

    /// <summary>
    /// Current required version of terms and conditions.
    /// </summary>
    public string RequiredTermsVersion { get; set; } = "1.0";

    /// <summary>
    /// Current required version of privacy policy.
    /// </summary>
    public string RequiredPrivacyPolicyVersion { get; set; } = "1.0";
}

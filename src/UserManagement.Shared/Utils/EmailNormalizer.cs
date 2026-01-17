namespace UserManagement.Shared.Utils;

public static class EmailNormalizer
{
    public static string Normalize(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;
        
        email = email.Trim().ToLowerInvariant();
        var parts = email.Split('@');
        if (parts.Length != 2) return email;

        var localPart = parts[0];
        var domain = parts[1];

        // Specific handling for Gmail/common providers to remove tags
        if (domain == "gmail.com" || domain == "googlemail.com")
        {
            var plusIndex = localPart.IndexOf('+');
            if (plusIndex >= 0)
            {
                localPart = localPart[..plusIndex];
            }
        }

        return $"{localPart}@{domain}";
    }
}

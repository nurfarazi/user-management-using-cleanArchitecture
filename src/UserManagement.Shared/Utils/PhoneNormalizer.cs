using System.Linq;

namespace UserManagement.Shared.Utils;

public static class PhoneNormalizer
{
    public static string? Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        
        // Remove all non-digit characters except leading '+'
        var digitsOnly = new string(phone.Where(c => char.IsDigit(c)).ToArray());
        
        // Basic E.164 normalization for Bangladesh if applicable
        if (phone.StartsWith("01") && phone.Length == 11)
        {
            return "+88" + phone;
        }
        
        if (phone.StartsWith("+"))
        {
            return "+" + digitsOnly;
        }

        return digitsOnly;
    }
}

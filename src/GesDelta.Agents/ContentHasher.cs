using System.Security.Cryptography;
using System.Text;

namespace GesDelta.Agents;

public static class ContentHasher
{
    public static string Sha256(string cleanText)
    {
        ArgumentNullException.ThrowIfNull(cleanText);

        var textBytes = Encoding.UTF8.GetBytes(cleanText);
        
        var hashBytes = SHA256.HashData(textBytes);
        
        return Convert.ToHexStringLower(hashBytes);
    }
}